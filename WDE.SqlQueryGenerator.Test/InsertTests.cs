using System;
using System.Collections.Generic;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator.Test
{
    public class InsertTests
    {
        [Test]
        public void TestInsertSingleSimpleObject()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .Insert(new { b = 1 });
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleObjectTwoProps()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .Insert(new { b = 1, c = 2 });
            Assert.AreEqual($"INSERT INTO `a` (`b`, `c`) VALUES (1, 2);", query.QueryString);
        }

        [Test]
        public void TestInsertSingleObjectThreeProps()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .Insert(new { b = 1, c = 2, d = 3 });
            Assert.AreEqual($"INSERT INTO `a` (`b`, `c`, `d`) VALUES {Environment.NewLine}(1, 2, 3);", query.QueryString);
        }

        [Test]
        public void TestInsertTwoSimpleObjects()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .BulkInsert(new []{new { b = 1 }, new { b = 2 }});
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES {Environment.NewLine}(1),{Environment.NewLine}(2);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleDictionary()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .Insert(new Dictionary<string, object?>(){ {"b", 1} });
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertTwoSimpleDictionary()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .BulkInsert(new []{new Dictionary<string, object?>(){ {"b", 1} }, new Dictionary<string, object?>(){ {"b", 2} }});
            Assert.AreEqual($"INSERT INTO `a` (`b`) VALUES{Environment.NewLine}(1),{Environment.NewLine}(2);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleObjectIgnore()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .InsertIgnore(new { b = 1 });
            Assert.AreEqual($"INSERT IGNORE INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertSingleSimpleDictionaryIgnore()
        {
            var query = Queries
                .Table(DatabaseTable.WorldTable("a"))
                .InsertIgnore(new Dictionary<string, object?>(){ {"b", 1} });
            Assert.AreEqual($"INSERT IGNORE INTO `a` (`b`) VALUES (1);", query.QueryString);
        }
        
        [Test]
        public void TestInsertEmpty()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).BulkInsert(Array.Empty<object>());
            Assert.AreEqual("", query.QueryString);
        }
    }
}