using System.Linq;
using NUnit.Framework;

namespace WDE.SqlInterpreter.Test
{
    public class TestTableName
    {
        private QueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new();
        }

        [Test]
        public void InvalidQuery()
        {
            var result = evaluator.ExtractInserts("INSERT INTO ('id', 'abc') VALUES (5, 6);").ToList();
            Assert.AreEqual(0, result.Count);
        }
    
        [Test]
        public void NoQuotes()
        {
            /* this should be supported, but it isn't now because of this antlr4 grammar */
            /* if grammar is fixed (to accept table name without quotes, this test can be fixed */
            var result = evaluator.ExtractInserts("INSERT INTO abc ('id') VALUES (5);").ToList();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void QuotesSingle()
        {
            var result = evaluator.ExtractInserts("INSERT INTO 'abc' ('id') VALUES (5);").ToList();
            Assert.AreEqual("abc", result[0].TableName);
        }

        [Test]
        public void QuotesDouble()
        {
            var result = evaluator.ExtractInserts("INSERT INTO \"abc\" ('id') VALUES (5);").ToList();
            Assert.AreEqual("abc", result[0].TableName);
        }

        [Test]
        public void QuotesBacktick()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` ('id') VALUES (5);").ToList();
            Assert.AreEqual("abc", result[0].TableName);
        }

        [Test]
        public void QuotesEscape()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `ab'c` ('id') VALUES (5);").ToList();
            Assert.AreEqual("ab'c", result[0].TableName);
        }

        [Test]
        public void TableNameWithDatabaseName()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `db`.`table` ('id') VALUES (5);").ToList();
            Assert.AreEqual("table", result[0].TableName);
        }
    }
}