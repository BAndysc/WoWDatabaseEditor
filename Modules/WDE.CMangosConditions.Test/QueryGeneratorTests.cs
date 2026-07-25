using NUnit.Framework;
using WDE.CMangosConditions.Exporter;
using WDE.Common.Database;

namespace WDE.CMangosConditions.Test
{
    public class QueryGeneratorTests
    {
        private readonly MangosConditionQueryGenerator generator = new();

        [Test]
        public void Delete_UsesInClause_DistinctOrdered()
        {
            var sql = generator.BuildDeleteQuery(new uint[] { 7, 3, 7 }).QueryString;
            StringAssert.Contains("`conditions`", sql);
            StringAssert.Contains("`condition_entry` IN (3, 7)", sql);
        }

        [Test]
        public void Delete_Empty_IsNoop()
        {
            var sql = generator.BuildDeleteQuery(new uint[0]).QueryString;
            StringAssert.DoesNotContain("DELETE", sql.ToUpperInvariant());
        }

        [Test]
        public void Insert_ContainsAllColumns()
        {
            var sql = generator.BuildInsertQuery(new IMangosConditionLine[]
            {
                new AbstractMangosConditionLine
                {
                    ConditionEntry = 11, ConditionType = -1, Value1 = 9, Value2 = 10,
                    Flags = 1, Comments = "a AND b",
                }
            }).QueryString;
            StringAssert.Contains("`conditions`", sql);
            StringAssert.Contains("condition_entry", sql);
            StringAssert.Contains("type", sql);
            StringAssert.Contains("value1", sql);
            StringAssert.Contains("value4", sql);
            StringAssert.Contains("flags", sql);
            StringAssert.Contains("comments", sql);
            StringAssert.Contains("-1", sql);
            StringAssert.Contains("a AND b", sql);
        }
    }
}
