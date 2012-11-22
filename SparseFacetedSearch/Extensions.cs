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
using System.Text;

using Lucene.Net.Search;

namespace Lucene.Net.Search
{
	public static class Extensions
	{
		//CartesianProduct - Lambda
		public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
		{
			IEnumerable<IEnumerable<T>> emptyProduct = new IEnumerable<T>[] { Enumerable.Empty<T>() };
			return sequences.Aggregate(
					emptyProduct,
					(accumulator, sequence) =>
					{
						return accumulator.SelectMany(
								accseq => sequence,
								(accseq, item) => accseq.Concat(item)
						);
					}
			);
		}

		//CartesianProduct - LINQ
		//http://blogs.msdn.com/b/ericlippert/archive/2010/06/28/computing-a-cartesian-product-with-linq.aspx
		static IEnumerable<IEnumerable<T>> CartesianProduct2<T>(this IEnumerable<IEnumerable<T>> sequences)
		{
			IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
			return sequences.Aggregate(
					emptyProduct,
					(accumulator, sequence) =>
							from accseq in accumulator
							from item in sequence
							select accseq.Concat(new[] { item }));
		}

		/// <summary>
		/// Version of Enumerable.Concat but adding only a single additional item.
		/// <para>
		/// This avoid the creation of a new array in the CartesianProduct functions
		/// </para>
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="first"></param>
		/// <param name="additionalItem"></param>
		/// <returns></returns>
		private static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, TSource additionalItem)
		{
			foreach (TSource source in first)
				yield return source;
			yield return additionalItem;
		}

		/// <summary>
		/// Presents an Ienumerable version of the DocIdSet Iterator
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		public static IEnumerable<int> AsEnumerable(this DocIdSet set)
		{
			var disi = set.Iterator();
			int doc;
			while ((doc = disi.NextDoc()) < Int32.MaxValue)
				yield return doc;
		}

		/// <summary>
		/// Assuming <paramref name="source"/> and <paramref name="other"/> are ordered. Returns another IEnumerable containing the common values.
		/// </summary>
		/// <param name="source">Ordered sequence of int</param>
		/// <param name="other">Ordered sequence of int</param>
		/// <returns>Returns another IEnumerable containing the common values</returns>
		public static IEnumerable<int> WalkingIntersect(this IEnumerable<int> source, IEnumerable<int> other)
		{
			var sourceEnum = source.GetEnumerator();
			var otherEnum = other.GetEnumerator();
			if (!sourceEnum.MoveNext())
				yield break;
			if (!otherEnum.MoveNext())
				yield break;

			while (true)
			{
				if (sourceEnum.Current == otherEnum.Current)
				{
					yield return sourceEnum.Current;
					if (!sourceEnum.MoveNext())
						break;
					if (!otherEnum.MoveNext())
						break;
				}

				if (sourceEnum.Current > otherEnum.Current)
					if (!otherEnum.MoveNext())
						break;

				if (sourceEnum.Current < otherEnum.Current)
					if (!sourceEnum.MoveNext())
						break;
			}
		}
	}
}