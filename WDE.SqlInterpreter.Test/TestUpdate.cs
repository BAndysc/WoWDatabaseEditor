using System.Linq;
using NUnit.Framework;

namespace WDE.SqlInterpreter.Test
{
    public class TestUpdate
    {
        private QueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new();
        }
    
        [Test]
        public void Simple()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE `b` = 5;").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual("b", result[0].Where.ColumnName);
            CollectionAssert.AreEqual(new object[]{5L}, result[0].Where.Values);
        }
    
        [Test]
        public void WhereIn()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE `b` IN (5, 6, 7, 8);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual("b", result[0].Where.ColumnName);
            CollectionAssert.AreEqual(new object[]{5L, 6L, 7L, 8L}, result[0].Where.Values);
        }
    
        [Test]
        public void MultiUpdate()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5, `b` = 6 WHERE `b` IN (5, 6, 7, 8);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a", "b"}, result[0].Updates.Select(s => s.ColumnName));
        }
    }
}