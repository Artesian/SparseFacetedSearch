/*
 * Version of Lucene.Net SimpleFacetedSearch
 * Modified/optimized to use simple enumerables instead of OpenBitSet.
 * OpenBitSet is a bitmap that requires an entry for even document in the index.
 * FieldValueBitSets then requires an OpenBitSet per facet value.
 * For large indexes and high cardinality facet values this requires very large amounts of memory.
 * An example in our case: 3.5M documents = a bitmap of around 400KB; 38k facet values = around 15GB!
 * 
 * Using IEnumberable<int> simulates having a sparse map of the document IDs.
 * /


/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Lucene.Net.Index;
using Lucene.Net.Search;

/*
 Suppose, we want a faceted search on fields f1 f2 f3, 
 and their values in index are
 
          f1     f2     f3
          --     --     --
doc1      A      I      1  
doc2      A      I      2  
doc3      A      I      3  
doc4      A      J      1  
doc5      A      J      2  
doc6      A      J      3  
doc7      B      I      1  
 
 Algorithm:
 1- Find all possible values for f1 which are (A,B) , for f2 which are (I,J) and for f3 which are (1,2,3)
 2- Find Cartesian Product of (A,B)X(I,J)X(1,2,3). (12 possible groups)
 3- Eliminate the ones that surely result in 0 hits. (for ex, B J 2. since they have no doc. in common)
*/

/*
 TODO: Support for pre-built queries defining groups can be added 
*/

namespace Lucene.Net.Search
{
	/// <summary>
	/// Based on SimpleFacetedSearch. Uses DocID lists instead on bitmaps.
	/// Efficient memory usage for high cardinality sparsely populated facets.
	/// <para>
	/// Suitable for high cardinality, sparsely populated facets.
	/// i.e. There are a large number of facet values and each facet value is hit in a small percentage of documents. Especially if there are also a large number of documents.
	/// SimpleFacetedSearch holds a bitmap for each value representing whether that value is a hit is each document (approx 122KB per 1M documents per facet value).
	/// So this is an O(N*M) problem. The memory requirement can grow very quickly.
	/// </para>
	/// <para>
	/// SparseFacetedSearcher records the DocID (Int32) for each value hit (memory cost = values * hits * 4).
	/// SimpleFacetedSearch record a bit for evey document per value (memory cost = values * documents / 8).
	/// So if the average number of hits for each value is less than 1/32 or 3.125% then Sparse is more memory efficient.
	/// <para>
	/// There are also some enumerable methods than mean there is much less pressure on the GC.
	/// Plus some bug fixes.
	/// </para>
	/// </para>
	/// </summary>
	public partial class SparseFacetedSearcher
	{
		public const int DefaultMaxDocPerGroup = 25;
		public static int MAX_FACETS = int.MaxValue;

		IndexReader reader;
		List<Tuple<FacetName, IEnumerable<int>>> groups = new List<Tuple<FacetName, IEnumerable<int>>>();

		public SparseFacetedSearcher(IndexReader reader, params string[] groupByFields)
		{
			this.reader = reader;

			var fieldValuesBitSets = new List<FieldValuesDocIDs>();

			//STEP 1
			//f1 = A, B
			//f2 = I, J
			//f3 = 1, 2, 3
			int maxFacets = 1;
			var inputToCP = new List<List<string>>();
			foreach (string field in groupByFields)
			{
				var f = new FieldValuesDocIDs(reader, field);
				maxFacets *= f.FieldValueDocIDsPair.Count;
				if (maxFacets > MAX_FACETS) throw new Exception("Facet count exceeded " + MAX_FACETS);
				fieldValuesBitSets.Add(f);
				inputToCP.Add(f.FieldValueDocIDsPair.Keys.ToList());
			}

			//STEP 2
			// comb1: A I 1
			// comb2: A I 2 etc.
			var cp = inputToCP.CartesianProduct();

			//SETP 3
			//create a single BitSet for each combination
			//BitSet1: A AND I AND 1
			//BitSet2: A AND I AND 2 etc.
			//and remove impossible comb's (for ex, B J 3) from list.
			Parallel.ForEach(cp, combinations =>
			{
				var comb = combinations.ToList();

				var bitSet = fieldValuesBitSets[0].FieldValueDocIDsPair[comb[0]];
				for (int j = 1; j < comb.Count; j++)
					bitSet = bitSet.WalkingIntersect(fieldValuesBitSets[j].FieldValueDocIDsPair[comb[j]]).ToList();

				//STEP 3
				if (bitSet.Any())
				{
					lock (groups)
						groups.Add(Tuple.Create(new FacetName(comb), bitSet));
				}
			});

			//Now groups has 7 rows (as <List<string>, BitSet> pairs) 
		}

		public FacetSearchResult Search(Query query = null, int maxDocPerGroup = DefaultMaxDocPerGroup)
		{
			var hitsPerFacet = SearchInternal(query, maxDocPerGroup);
			return new FacetSearchResult(hitsPerFacet);
		}
		public FacetCountResult Count(Query query = null, int maxDocPerGroup = DefaultMaxDocPerGroup)
		{
			var hitsPerFacet = SearchInternal(query, maxDocPerGroup);
			return new FacetCountResult(hitsPerFacet);
		}

		private IEnumerable<FacetHits> SearchInternal(Query query = null, int maxDocPerGroup = DefaultMaxDocPerGroup)
		{
			if (query == null)
				query = new MatchAllDocsQuery();

			var queryDocidSet =
				new CachingWrapperFilter(new QueryWrapperFilter(query))
				.GetDocIdSet(reader)
				.AsEnumerable()
				.ToList();

			var hitsPerFacet = groups.Select(g => new FacetHits(g.Item1, reader, queryDocidSet, g.Item2, maxDocPerGroup)).ToList();

			Parallel.ForEach(hitsPerFacet, h => h.Calculate());

			return hitsPerFacet;
		}
	}
}