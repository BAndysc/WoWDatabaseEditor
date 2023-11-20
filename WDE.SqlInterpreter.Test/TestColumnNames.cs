using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlInterpreter.Test
{
    public class TestColumnNames
    {
        private BaseQueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new("world", "hotfix", DataDatabaseType.World);
        }

        [Test]
        public void NoQuotes()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (guid, name, surname) VALUES (5, 6, 7);").ToList();
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void QuotesSingle()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` ('guid', 'name', 'surname') VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }

        [Test]
        public void QuotesDouble()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (\"guid\", \"name\", \"surname\") VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }

        [Test]
        public void QuotesBacktick()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`guid`, `name`, `surname`) VALUES (5, 6, 7);").ToList();
            CollectionAssert.AreEqual(new []{"guid", "name", "surname"}, result[0].Columns);
        }
    }
}