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
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
        }
    
        [Test]
        public void WhereIn()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE `b` IN (5, 6, 7, 8);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual(4, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            CollectionAssert.AreEqual(new string[]{"b", "b", "b", "b"}, result[0].Where.Conditions.Select(c => c.Columns[0]));
            CollectionAssert.AreEqual(new object?[]{5L, 6L, 7L, 8L}, result[0].Where.Conditions.Select(c => c.Values[0]));
        }
    
        [Test]
        public void MultiUpdate()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5, `b` = 6 WHERE `b` IN (5, 6, 7, 8);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a", "b"}, result[0].Updates.Select(s => s.ColumnName));
        }
    
        [Test]
        public void MultiKeyUpdate()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE `b` = 5 AND `c` = 4;").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual("c", result[0].Where.Conditions[0].Columns[1]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
            Assert.AreEqual(4L, result[0].Where.Conditions[0].Values[1]);
        }
    
        [Test]
        public void ManyMultiKeyUpdate()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE (`b` = 5 AND `c` = 4) OR (`b` = 3 AND `d` = 9);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
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
    
        [Test]
        public void ManyMultiKeyUpdateExtraBrackets()
        {
            var result = evaluator.ExtractUpdates("UPDATE `abc` SET `a` = 5 WHERE (((`b` = 5 AND `c` = 4)) OR (`b` = 3 AND `d` = 9) OR (`x` = 3));").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("abc", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"a"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual(3, result[0].Where.Conditions.Length);
            Assert.AreEqual("b", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual("c", result[0].Where.Conditions[0].Columns[1]);
            Assert.AreEqual(5L, result[0].Where.Conditions[0].Values[0]);
            Assert.AreEqual(4L, result[0].Where.Conditions[0].Values[1]);
            
            Assert.AreEqual("b", result[0].Where.Conditions[1].Columns[0]);
            Assert.AreEqual("d", result[0].Where.Conditions[1].Columns[1]);
            Assert.AreEqual(3L, result[0].Where.Conditions[1].Values[0]);
            Assert.AreEqual(9L, result[0].Where.Conditions[1].Values[1]);
            
            Assert.AreEqual("x", result[0].Where.Conditions[2].Columns[0]);
            Assert.AreEqual(3L, result[0].Where.Conditions[2].Values[0]);
        }
    }
}