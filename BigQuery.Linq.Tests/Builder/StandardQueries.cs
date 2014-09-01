﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigQuery.Linq.Tests.Builder
{
    class Hoge
    {
        public double MyProperty { get; set; }
    }


    class Huga
    {
        public double MyProperty2 { get; set; }
    }

    class HogeMoge
    {
        public string Hoge { get; set; }
        public string Huga { get; set; }
        public int Tako { get; set; }
    }

    [TableName("[publicdata:samples.wikipedia]")]
    class Wikipedia
    {
        public string title { get; set; }

        public int wp_namespace { get; set; }
    }

    [TableName("[publicdata:samples.shakespeare]")]
    class Shakespeare
    {
        public string word { get; set; }
        public string corpus { get; set; }
    }

    class Repository
    {
        public bool has_downloads { get; set; }
    }

    [TestClass]
    public class StandardQueries
    {
        [TestMethod]
        public void DirectSelect()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var s = context.Select(() => new
            {
                A = "aaa",
                B = BqFunc.Abs(-5),
                FROM = 100,
            }).ToString().TrimEnd();

            s.Is(@"SELECT
  'aaa' AS [A],
  ABS(-5) AS [B],
  100 AS [FROM]");
        }

        [TestMethod]
        public void WhereSelect()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var s = context.From<Wikipedia>("tablewikipedia")
                .Where(x => x.wp_namespace == 100)
                .Select(x => new { x.title, x.wp_namespace })
                .ToString().TrimEnd();

            s.Is(@"SELECT
  [title],
  [wp_namespace]
FROM
  [tablewikipedia]
WHERE
  ([wp_namespace] = 100)");
        }

        [TestMethod]
        public void WhereWhere()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var s = context.From<Wikipedia>("tablewikipedia")
                .Where(x => x.wp_namespace == 100)
                .Where(x => x.title != null)
                .Where(x => x.title == "AiUeo")
                .Select(x => new { x.title, x.wp_namespace })
                .ToString().TrimEnd();

            s.Is(@"SELECT
  [title],
  [wp_namespace]
FROM
  [tablewikipedia]
WHERE
  ((([wp_namespace] = 100) AND ([title] IS NOT NULL)) AND ([title] = 'AiUeo'))");
        }

        [TestMethod]
        public void TodoJoin()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var query1 = context.From<Wikipedia>("[publicdata:samples.wikipedia]")
                .Join(context.From<Wikipedia>("[publicdata:samples.wikipedia]").Select(x => new { x.title, x.wp_namespace }).Limit(1000),
                    (kp, tp) => new { kp, tp },
                    x => x.tp.title == x.kp.title)
                .Select(x => new { x.kp.title, x.tp.wp_namespace })
                .Limit(100)
                .ToString();

            query1.Is(@"
SELECT
  [kp.title] AS [title],
  [tp.wp_namespace] AS [wp_namespace]
FROM
  [publicdata:samples.wikipedia] AS kp
INNER JOIN (
  SELECT
    [title],
    [wp_namespace]
  FROM
    [publicdata:samples.wikipedia]
  LIMIT 1000
) AS tp ON ([tp.title] = [kp.title])
LIMIT 100".TrimSmart());
        }

        [TestMethod]
        public void Sample1()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var query1 = context.From<Wikipedia>()
                .Where(x => x.wp_namespace == 0)
                .Select(x => new
                {
                    x.title,
                    hash_value = BqFunc.Hash(x.title),
                    included_in_sample = (BqFunc.Abs(BqFunc.Hash(x.title)) % 2 == 1)
                        ? "True"
                        : "False"
                })
                .Limit(5)
                .ToString();

            query1.Is(@"
SELECT
  [title],
  HASH([title]) AS [hash_value],
  IF(((ABS(HASH([title])) % 2) = 1), 'True', 'False') AS [included_in_sample]
FROM
  [publicdata:samples.wikipedia]
WHERE
  ([wp_namespace] = 0)
LIMIT 5".TrimStart());
        }

        [TestMethod]
        public void SampleFirstCase()
        {
            var context = new BigQuery.Linq.BigQueryContext();

            var query1 = context.From<Shakespeare>()
                .Where(x => x.word.Contains("th"))
                .Select(x => new
                {
                    x.word,
                    x.corpus,
                    count = BqFunc.Count(x.word)
                })
                .GroupBy(x => new { x.word, x.corpus })
                .ToString()
                .TrimSmart();

            query1.Is(@"
SELECT
  [word],
  [corpus],
  COUNT([word]) AS [count]
FROM
  [publicdata:samples.shakespeare]
WHERE
  [word] CONTAINS 'th'
GROUP BY
  [word],
  [corpus]".TrimSmart());
        }
    }
}
