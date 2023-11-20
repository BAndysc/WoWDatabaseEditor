using System;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator.Test
{
    public class UpdateTests
    {
        [Test]
        public void SimpleSet()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => true).Set("b", 2).Update();
            Assert.AreEqual("UPDATE `a` SET `b` = 2;", query.QueryString);
        }
        
        [Test]
        public void DualSet()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => true).Set("b", 2).Set("c", 3.5).Update();
            Assert.AreEqual("UPDATE `a` SET `b` = 2, `c` = 3.5;", query.QueryString);
        }
        
        [Test]
        public void TripleSet()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => true).Set("b", 2).Set("c", 3.5).Set("d", "abc").Update();
            Assert.AreEqual("UPDATE `a` SET `b` = 2, `c` = 3.5, `d` = 'abc';", query.QueryString);
        }
        
        [Test]
        public void SimpleSetWithWhere()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => r.Column<int>("b") == 3).Set("b", 2).Update();
            Assert.AreEqual("UPDATE `a` SET `b` = 2 WHERE `b` = 3;", query.QueryString);
        }

        [Test]
        public void SetWithComment()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => true).Set("b", 2, "x").Update();
            Assert.AreEqual("UPDATE `a` SET `b` = 2; -- x", query.QueryString);
        }

        [Test]
        public void SetWithCommentAndWhere()
        {
            var query = Queries.Table(DatabaseTable.WorldTable("a")).Where(r => r.Column<int>("c") == 4).Set("b", 2, "x").Update();
            Assert.AreEqual($"UPDATE `a` SET `b` = 2 -- x{Environment.NewLine}                WHERE `c` = 4;", query.QueryString);
        }
    }
}