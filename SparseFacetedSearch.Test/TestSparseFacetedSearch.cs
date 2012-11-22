﻿/* 
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
using System.Threading;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Util;

using NUnit.Framework;

namespace Lucene.Net.Search
{
	[TestFixture]
	public class TestSparseFacetedSearch
	{
		Directory _Dir = new RAMDirectory();
		IndexReader _Reader;

		[SetUp]
		public void SetUp()
		{

			IndexWriter writer = new IndexWriter(_Dir, new StandardAnalyzer(), true);

			AddDoc(writer, "us", "CCN", "politics", "The White House doubles down on social media");
			AddDoc(writer, "us", "CCN", "politics", "Senate Dems fail to block filibuster over judicial nominee");
			AddDoc(writer, "us", "BCC", "politics", "A frozen pig's foot and a note laced with anti-Semitic rants were sent to Rep. Peter King's Capitol Hill office, a congressional source familiar with the situation confirmed to CNN Monday");
			AddDoc(writer, "us", "CCN", "sport", "But when all was said and done, Haslem's 13 points, five rebounds, two assists, one block and one steal in the course of 23 minutes");
			AddDoc(writer, "en", "CCN", "tech", "blockingQueue<T> contains two private fields and exposes two public methods.");
			AddDoc(writer, "en", "BCC", "tech", "An Argentine court this week granted an injunction that blocks the Internet giant from 'suggesting' searches that lead to certain sites that have been deemed anti-Semitic, and removes the sites from the search engine's index");
			AddDoc(writer, "en", "CCN", "dummy", "oooooooooooooooooooooo");
			writer.Close();

			_Reader = IndexReader.Open(_Dir);
		}

		void AddDoc(IndexWriter writer, string lang, string source, string group, string text)
		{
			Field f0 = new Field("lang", lang, Field.Store.YES, Field.Index.NOT_ANALYZED);
			Field f1 = new Field("source", source, Field.Store.YES, Field.Index.NOT_ANALYZED);
			Field f2 = new Field("category", group, Field.Store.YES, Field.Index.NOT_ANALYZED);
			Field f3 = new Field("text", text, Field.Store.YES, Field.Index.ANALYZED);
			Document doc = new Document();
			doc.Add(f0);
			doc.Add(f1);
			doc.Add(f2);
			doc.Add(f3);
			writer.AddDocument(doc);
		}

		[Test]
		public void Test1()
		{
			//See, Is there an exception
			HowToUse("block*");
			HowToUse("qwertyuiop");
			//OK. No exception
		}

		[Test]
		public void Test2()
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("block*");

			var sfs = new SparseFacetedSearcher(_Reader, "category");
			var hits = sfs.Search(query);

			Assert.AreEqual(4, hits.Facets.Count());

			foreach (var hpg in hits.Facets)
			{
				if (hpg.Name[0] == "politics")
				{
					Assert.AreEqual(1, hpg.Count);
				}
				else
					if (hpg.Name[0] == "tech")
					{
						Assert.AreEqual(2, hpg.Count);
					}
					else
						if (hpg.Name[0] == "sport")
						{
							Assert.AreEqual(1, hpg.Count);
						}
						else
						{
							Assert.AreEqual(0, hpg.Count);
						}
			}

			Assert.AreEqual(4, hits.TotalHitCount);

			foreach (var hpg in hits.Facets)
			{
				foreach (Document doc in hpg.Documents)
				{
					string text = doc.GetField("text").StringValue();
					Assert.IsTrue(text.Contains("block"));
				}
			}
		}

		[Test]
		public void Test3()
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("block*");

			var sfs = new SparseFacetedSearcher(_Reader, "lang", "source", "category");
			var hits = sfs.Search(query);

			Assert.AreEqual(6, hits.Facets.Count());
			int nohit = 0;

			foreach (var hpg in hits.Facets)
			{
				//Test for [System.Collections.Generic.KeyNotFoundException : The given key was not present in the dictionary.]
				var x = hits[hpg.Name];
				var y = hits[hpg.Name.ToString()];

				if (hpg.Name[0] == "us" && hpg.Name[1] == "CCN" && hpg.Name[2] == "politics")
				{
					Assert.AreEqual(1, hpg.Count);
				}
				else
					if (hpg.Name[0] == "en" && hpg.Name[1] == "BCC" && hpg.Name[2] == "tech")
					{
						Assert.AreEqual(1, hpg.Count);
					}
					else
						if (hpg.Name[0] == "us" && hpg.Name[1] == "CCN" && hpg.Name[2] == "sport")
						{
							Assert.AreEqual(1, hpg.Count);
						}
						else
							if (hpg.Name[0] == "en" && hpg.Name[1] == "CCN" && hpg.Name[2] == "tech")
							{
								Assert.AreEqual(1, hpg.Count);
							}
							else
							{
								nohit++;
								Assert.AreEqual(0, hpg.Count);
							}
			}
			Assert.AreEqual(2, nohit);
			Assert.AreEqual(4, hits.TotalHitCount);

			foreach (var hpg in hits.Facets)
			{
				foreach (Document doc in hpg.Documents)
				{
					string text = doc.GetField("text").StringValue();
					Assert.IsTrue(text.Contains("block"));
				}
			}
		}

		[Test]
		public void Test4()
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("xxxxxxxxxxxxx");

			var sfs = new SparseFacetedSearcher(_Reader, "category");
			var hits = sfs.Search(query);

			var facets = hits.Facets.ToArray();

			Assert.AreEqual(4, facets.Length);
			Assert.AreEqual(0, facets[0].Count);
			Assert.AreEqual(0, facets[1].Count);
			Assert.AreEqual(0, facets[2].Count);
		}

		[Test]
		public void Test5()
		{
			Query query = new MatchAllDocsQuery();

			var sfs = new SparseFacetedSearcher(_Reader, "category");
			var hits = sfs.Search(query);

			Assert.AreEqual(7, hits.TotalHitCount);
		}

		[Test]
		public void Test6()
		{
			Query query = new MatchAllDocsQuery();

			var sfs = new SparseFacetedSearcher(_Reader, "nosuchfield");
			var hits = sfs.Search(query);

			Assert.AreEqual(0, hits.TotalHitCount);
			Assert.AreEqual(0, hits.Facets.Count());
		}

		[Test]
		public void Test7()
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("a");

			var sfs = new SparseFacetedSearcher(_Reader, "category");
			var hits = sfs.Search(query);

			Assert.AreEqual(0, hits.TotalHitCount, "Unexpected TotalHitCount");
			foreach (var x in hits.Facets.Where(h => h.Count > 0))
			{
				Assert.Fail("There must be no hit");
			}

		}

		int _errorCount = 0;
		void MultiThreadedAccessThread(object o)
		{
			var sfs = (SparseFacetedSearcher)o;

			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("block*");

			for (int i = 0; i < 2000; i++)
			{
				var hits = sfs.Search(query);

				if (6 != hits.Facets.Count()) _errorCount++;

				foreach (var hpg in hits.Facets)
				{
					if (hpg.Name[0] == "us" && hpg.Name[1] == "CCN" && hpg.Name[2] == "politics")
					{
						if (1 != hpg.Count) _errorCount++;
					}
					else
						if (hpg.Name[0] == "en" && hpg.Name[1] == "BCC" && hpg.Name[2] == "tech")
						{
							if (1 != hpg.Count) _errorCount++;
						}
						else
							if (hpg.Name[0] == "us" && hpg.Name[1] == "CCN" && hpg.Name[2] == "sport")
							{
								if (1 != hpg.Count) _errorCount++;
							}
							else
								if (hpg.Name[0] == "en" && hpg.Name[1] == "CCN" && hpg.Name[2] == "tech")
								{
									if (1 != hpg.Count) _errorCount++;
								}
								else
								{
									if (0 != hpg.Count) _errorCount++;
								}

					if (4 != hits.TotalHitCount) _errorCount++;
				}
			}
		}

		[Test]
		public void TestMultiThreadedAccess()
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse("block*");

			var sfs = new SparseFacetedSearcher(_Reader, "lang", "source", "category");
			_errorCount = 0;

			Thread[] t = new Thread[20];
			for (int i = 0; i < t.Length; i++)
			{
				t[i] = new Thread(MultiThreadedAccessThread);
				t[i].Start(sfs);
			}
			for (int i = 0; i < t.Length; i++)
			{
				t[i].Join();
			}

			Assert.AreEqual(0, _errorCount);
		}

		/// <summary>
		/// *****************************************************
		/// * SAMPLE USAGE                                      *
		/// *****************************************************
		/// </summary>
		void HowToUse(string searchString)
		{
			Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "text", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29)).Parse(searchString);

			var sfs = new SparseFacetedSearcher(_Reader, "lang", "source", "category");
			var hits = sfs.Search(query);

			long totalHits = hits.TotalHitCount;
			foreach (var hpg in hits.Facets)
			{
				long hitCountPerGroup = hpg.Count;
				var facetName = hpg.Name;
				for (int i = 0; i < facetName.Length; i++)
				{
					string part = facetName[i];
				}
				foreach (Document doc in hpg.Documents)
				{
					string text = doc.GetField("text").StringValue();
					System.Diagnostics.Debug.WriteLine(">>" + facetName + ": " + text);
				}
			}
		}

	}
}