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

namespace Lucene.Net.Search
{
	/// <summary>
	/// Simplified version of FacetHits containing the count only.
	/// <para>
	/// This means that IndexReader does not need to be held open.
	/// Useful for the majority of casses where we're only interested in the count and not the individual Documents.
	/// </para>
	/// </summary>
	public class FacetCount
	{
		public FacetCount(FacetHits facet)
		{
			Name = facet.Name;
			Count = facet.Count;
		}

		public SparseFacetedSearcher.FacetName Name { get; private set; }
		public long Count { get; private set; }
	}
}