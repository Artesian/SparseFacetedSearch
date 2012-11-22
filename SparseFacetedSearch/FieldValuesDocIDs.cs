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

using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Search
{
	/// <summary>
	/// "internal" class to extract the DocIDs of each value of a field
	/// </summary>
	public class FieldValuesDocIDs
	{
		public string FieldName { get; private set; }
		public IDictionary<string, IEnumerable<int>> FieldValueDocIDsPair { get; private set; }

		public FieldValuesDocIDs(IndexReader reader, string field)
		{
			FieldName = field;
			FieldValueDocIDsPair = new Dictionary<string, IEnumerable<int>>();

			foreach (string val in GetFieldValues(reader, field))
				FieldValueDocIDsPair.Add(val, GetDocIDs(reader, field, val));
		}

		private IEnumerable<string> GetFieldValues(IndexReader reader, string groupByField)
		{
			TermEnum te = reader.Terms(new Term(groupByField, string.Empty));
			if (te.Term() == null || te.Term().Field() != groupByField)
				return Enumerable.Empty<string>();

			var list = new List<string>();
			list.Add(te.Term().Text());

			while (te.Next())
			{
				if (te.Term().Field() != groupByField)
					break;

				list.Add(te.Term().Text());
			}
			return list;
		}

		private IEnumerable<int> GetDocIDs(IndexReader reader, string groupByField, string group)
		{
			var query = new TermQuery(new Term(groupByField, group));
			Filter filter = new CachingWrapperFilter(new QueryWrapperFilter(query));

			return filter.GetDocIdSet(reader).AsEnumerable().ToList();
		}
	}
}