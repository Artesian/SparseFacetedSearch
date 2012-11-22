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
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Represents the Documents containing a facet value
	/// </summary>
	[DebuggerDisplay("{Name}")]
	public class FacetHits : IEnumerable<Document>
	{
		IndexReader reader;
		int maxDocPerFacet;
		IEnumerable<int> result;
		IEnumerable<int> queryDocidSet;
		IEnumerable<int> groupDocidSet;
		int itemsReturned;

		internal FacetHits(SparseFacetedSearcher.FacetName facetName, IndexReader reader, IEnumerable<int> queryDocidSet, IEnumerable<int> groupDocidSet, int maxDocPerFacet)
		{
			this.Name = facetName;
			this.reader = reader;
			this.maxDocPerFacet = maxDocPerFacet;
			this.queryDocidSet = queryDocidSet;
			this.groupDocidSet = groupDocidSet;
		}

		internal void Calculate()
		{
			result = queryDocidSet.WalkingIntersect(groupDocidSet).ToList();

			Count = result.Count();

			queryDocidSet = null;
			groupDocidSet = null;
		}

		public SparseFacetedSearcher.FacetName Name { get; private set; }

		public long Count { get; private set; }

		public IEnumerator<Document> GetEnumerator()
		{
			return Documents.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerable<Document> Documents
		{
			get
			{
				return result
					.Where(id => ++itemsReturned <= maxDocPerFacet)
					.Select(id => reader.Document(id));
			}
		}
	}
}
