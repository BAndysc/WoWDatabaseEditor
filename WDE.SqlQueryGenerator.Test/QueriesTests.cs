using NUnit.Framework;

namespace WDE.SqlQueryGenerator.Test
{
    public class QueriesTests
    {
        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual("", Queries.Empty().QueryString);
        }
        
        [Test]
        public void TestTable()
        {
            Assert.AreEqual("abc", Queries.Table("abc").TableName);
        }
        
        [Test]
        public void TestTransaction()
        {
            var query = Queries.BeginTransaction();
            query.Table("abc").Where(r => true).Delete();
            query.Table("def").Where(r => true).Delete();
            Assert.AreEqual("DELETE FROM `abc`;\nDELETE FROM `def`;", query.Close().QueryString);
        }
    }
}