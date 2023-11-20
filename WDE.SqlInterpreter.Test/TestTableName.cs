using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlInterpreter.Test
{
    public class TestTableName
    {
        private BaseQueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new("world", "hotfix", DataDatabaseType.Hotfix);
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
            var result = evaluator.ExtractInserts("INSERT INTO abc ('id') VALUES (5);").ToList();
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void QuotesSingle()
        {
            var result = evaluator.ExtractInserts("INSERT INTO 'abc' ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("abc"), result[0].TableName);
        }

        [Test]
        public void QuotesDouble()
        {
            var result = evaluator.ExtractInserts("INSERT INTO \"abc\" ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("abc"), result[0].TableName);
        }

        [Test]
        public void QuotesBacktick()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("abc"), result[0].TableName);
        }

        [Test]
        public void QuotesEscape()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `ab'c` ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("ab'c"), result[0].TableName);
        }

        [Test]
        public void TableNameWithHotfixDatabaseName()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `hotfix`.`table` ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("table"), result[0].TableName);
        }

        [Test]
        public void TableNameWithWorldDatabaseName()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `world`.`table` ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.WorldTable("table"), result[0].TableName);
        }

        [Test]
        public void TableNameWithNoDatabaseName()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `table` ('id') VALUES (5);").ToList();
            Assert.AreEqual(DatabaseTable.HotfixTable("table"), result[0].TableName);
        }

        [Test]
        public void TableNameWithUnknownDatabaseName()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `unknown`.`table` ('id') VALUES (5);").ToList();
            Assert.AreEqual(0, result.Count);
        }
    }
}