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
        
        [Test]
        public void SimpleNoBackticks()
        {
            var result = evaluator.ExtractUpdates("UPDATE creature_template SET faction = 35 WHERE entry = 31779").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("creature_template", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"faction"}, result[0].Updates.Select(s => s.ColumnName));
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            Assert.AreEqual("entry", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual(31779L, result[0].Where.Conditions[0].Values[0]);
        }
        
        [Test]
        public void SimpleFloatNoBackticks()
        {
            var result = evaluator.ExtractUpdates("UPDATE creature SET position_x = 6443.63, position_y = 2039.9923, position_z = 551.1352, orientation = 2.6383197 WHERE guid = 121393").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("creature", result[0].TableName);
            CollectionAssert.AreEqual(new string[]{"position_x", "position_y", "position_z", "orientation"}, result[0].Updates.Select(s => s.ColumnName));
            CollectionAssert.AreEqual(new string[]{"6443.63", "2039.9923", "551.1352", "2.6383197"}, result[0].Updates.Select(s => s.Value));
            Assert.AreEqual(1, result[0].Where.Conditions.Length);
            Assert.AreEqual(1, result[0].Where.Conditions[0].Columns.Length);
            Assert.AreEqual("guid", result[0].Where.Conditions[0].Columns[0]);
            Assert.AreEqual(121393L, result[0].Where.Conditions[0].Values[0]);
        }
    }
}