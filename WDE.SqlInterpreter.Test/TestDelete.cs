using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlInterpreter.Test
{
    public class TestDelete
    {
        private BaseQueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new("world", "hotfix", DataDatabaseType.World);
        }
    
        [Test]
        public void Simple()
        {
            var result = evaluator.ExtractDeletes("DELETE FROM `abc` WHERE `b` = 5;").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), result[0].TableName);
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
        }
    
        [Test]
        public void WhereIn()
        {
            var result = evaluator.ExtractDeletes("DELETE FROM `abc` WHERE `b` IN (5, 6, 7, 8);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), result[0].TableName);
            Assert.AreEqual(4, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            CollectionAssert.AreEqual(new string[]{"b", "b", "b", "b"}, result[0].Where.Conditions.Select(c => c.Columns[0]));
            CollectionAssert.AreEqual(new object?[]{5L, 6L, 7L, 8L}, result[0].Where.Conditions.Select(c => c.Values[0]));
        }
    
        [Test]
        public void MultiKeyDelete()
        {
            var result = evaluator.ExtractDeletes("DELETE FROM `abc` WHERE `b` = 5 AND `c` = 4;").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), result[0].TableName);
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual("c", result[0].Where.Conditions[0].Columns[1]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
            Assert.AreEqual(4L, result[0].Where.Conditions[0].Values[1]);
        }
    
        [Test]
        public void ManyMultiKeyDelete()
        {
            var result = evaluator.ExtractDeletes("DELETE FROM `abc` WHERE (`b` = 5 AND `c` = 4) OR (`b` = 3 AND `d` = 9);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), result[0].TableName);
            Assert.AreEqual(2, result[0].Where.Conditions.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual("c", result[0].Where.Conditions[0].Columns[1]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
            Assert.AreEqual(4L, result[0].Where.Conditions[0].Values[1]);
            
            Assert.AreEqual("b", result[0].Where.Conditions[1].Columns[0]);
            Assert.AreEqual("d", result[0].Where.Conditions[1].Columns[1]);
            Assert.AreEqual(3L, result[0].Where.Conditions[1].Values[0]);
            Assert.AreEqual(9L, result[0].Where.Conditions[1].Values[1]);
        }
    }
}