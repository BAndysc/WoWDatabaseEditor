using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace WDE.SqlQueryGenerator.Test
{
    public class InsertTests
    {
        [Test]
        public void TestInsertSingleSimpleObject()
        {
            var query = Queries
                .Table("a")
                .Insert(new { b = 1 });
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertTwoSimpleObjects()
        {
            var query = Queries
                .Table("a")
                .BulkInsert(new []{new { b = 1 }, new { b = 2 }});
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES {Environment.NewLine}(1),{Environment.NewLine}(2);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleDictionary()
        {
            var query = Queries
                .Table("a")
                .Insert(new Dictionary<string, object?>(){ {"b", 1} });
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertTwoSimpleDictionary()
        {
            var query = Queries
                .Table("a")
                .BulkInsert(new []{new Dictionary<string, object?>(){ {"b", 1} }, new Dictionary<string, object?>(){ {"b", 2} }});
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES{Environment.NewLine}(1),{Environment.NewLine}(2);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleObjectIgnore()
        {
            var query = Queries
                .Table("a")
                .InsertIgnore(new { b = 1 });
            Assert.AreEqual($"INSERT IGNORE INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleDictionaryIgnore()
        {
            var query = Queries
                .Table("a")
                .InsertIgnore(new Dictionary<string, object?>(){ {"b", 1} });
            Assert.AreEqual($"INSERT IGNORE INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertEmpty()
        {
            var query = Queries.Table("a").BulkInsert(Array.Empty<object>());
            Assert.AreEqual("", query.QueryString);
        }
    }
}