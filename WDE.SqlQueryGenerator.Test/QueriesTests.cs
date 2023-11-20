using System;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator.Test
{
    public class QueriesTests
    {
        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual("", Queries.Empty(DataDatabaseType.World).QueryString);
        }
        
        [Test]
        public void TestTable()
        {
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), Queries.Table(DatabaseTable.WorldTable("abc")).TableName);
        }
        
        [Test]
        public void TestTransaction()
        {
            var query = Queries.BeginTransaction(DataDatabaseType.World);
            query.Table(DatabaseTable.WorldTable("abc")).Where(r => true).Delete();
            query.Table(DatabaseTable.WorldTable("def")).Where(r => true).Delete();
            Assert.AreEqual($"DELETE FROM `abc`;{Environment.NewLine}DELETE FROM `def`;", query.Close().QueryString);
        }
    }
}