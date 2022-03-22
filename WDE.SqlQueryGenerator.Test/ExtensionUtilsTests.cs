using System;
using NUnit.Framework;

namespace WDE.SqlQueryGenerator.Test
{
    public class ExtensionUtilsTests
    {
        [Test]
        public void TestComment()
        {
            Assert.AreEqual(" -- abc", Queries.BeginTransaction().Comment("abc").QueryString);
        }
        
        [Test]
        public void TestDefineVariableInt()
        {
            Assert.AreEqual("SET @A := 1;", Queries.BeginTransaction().DefineVariable("A", 1).QueryString);
        }
        
        [Test]
        public void TestDefineVariableFloat()
        {
            Assert.AreEqual("SET @A := 1.5;", Queries.BeginTransaction().DefineVariable("A", 1.5f).QueryString);
        }
        
        [Test]
        public void TestDefineVariableDouble()
        {
            Assert.AreEqual("SET @A := 1.5;", Queries.BeginTransaction().DefineVariable("A", 1.5).QueryString);
        }
        
        [Test]
        public void TestDefineVariableNull()
        {
            Assert.AreEqual("SET @A := NULL;", Queries.BeginTransaction().DefineVariable("A", null).QueryString);
        }
        
        [Test]
        public void TestDefineVariableString()
        {
            Assert.AreEqual("SET @A := 'abc';", Queries.BeginTransaction().DefineVariable("A", "abc").QueryString);
        }
        
        [Test]
        public void TestDefineVariableVariable()
        {
            Assert.AreEqual("SET @A := @B;", Queries.BeginTransaction().DefineVariable("A", Queries.BeginTransaction().Variable("B")).QueryString);
        }
        
        [Test]
        public void TestBlankLine()
        {
            var trans = Queries.BeginTransaction();
            trans.BlankLine();
            trans.BlankLine();
            Assert.AreEqual("", trans.Close().QueryString);
        }
        
        [Test]
        public void TestQueryToString()
        {
            Assert.AreEqual("", Queries.BeginTransaction().BlankLine().ToString());
        }
    }
}