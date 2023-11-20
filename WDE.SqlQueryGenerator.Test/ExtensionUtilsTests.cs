using System;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator.Test
{
    public class ExtensionUtilsTests
    {
        [Test]
        public void TestComment()
        {
            Assert.AreEqual(" -- abc", Queries.BeginTransaction(DataDatabaseType.World).Comment("abc").QueryString);
        }
        
        [Test]
        public void TestDefineVariableInt()
        {
            Assert.AreEqual("SET @A := 1;", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", 1).QueryString);
        }
        
        [Test]
        public void TestDefineVariableFloat()
        {
            Assert.AreEqual("SET @A := 1.5;", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", 1.5f).QueryString);
        }
        
        [Test]
        public void TestDefineVariableDouble()
        {
            Assert.AreEqual("SET @A := 1.5;", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", 1.5).QueryString);
        }
        
        [Test]
        public void TestDefineVariableNull()
        {
            Assert.AreEqual("SET @A := NULL;", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", null).QueryString);
        }
        
        [Test]
        public void TestDefineVariableString()
        {
            Assert.AreEqual("SET @A := 'abc';", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", "abc").QueryString);
        }
        
        [Test]
        public void TestDefineVariableVariable()
        {
            Assert.AreEqual("SET @A := @B;", Queries.BeginTransaction(DataDatabaseType.World).DefineVariable("A", Queries.BeginTransaction(DataDatabaseType.World).Variable("B")).QueryString);
        }
        
        [Test]
        public void TestBlankLine()
        {
            var trans = Queries.BeginTransaction(DataDatabaseType.World);
            trans.BlankLine();
            trans.BlankLine();
            Assert.AreEqual(Environment.NewLine + Environment.NewLine + Environment.NewLine, trans.Close().QueryString);
        }
        
        [Test]
        public void TestQueryToString()
        {
            Assert.AreEqual(Environment.NewLine, Queries.BeginTransaction(DataDatabaseType.World).BlankLine().ToString());
        }
    }
}