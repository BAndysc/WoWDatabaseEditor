using NUnit.Framework;
using WDE.CMangosConditions.Exporter;
using WDE.Common.Database;

namespace WDE.CMangosConditions.Test
{
    public class UnitConditionQueryGeneratorTests
    {
        private readonly MangosUnitConditionQueryGenerator generator = new();

        [Test]
        public void Delete_UsesInClause_DistinctOrdered_WithNegativeIds()
        {
            var sql = generator.BuildDeleteQuery(new[] { 7, -5530001, 7 }).QueryString;
            StringAssert.Contains("`unit_condition`", sql);
            StringAssert.Contains("`Id` IN (-5530001, 7)", sql);
        }

        [Test]
        public void Delete_Empty_IsNoop()
        {
            var sql = generator.BuildDeleteQuery(new int[0]).QueryString;
            StringAssert.DoesNotContain("DELETE", sql.ToUpperInvariant());
        }

        [Test]
        public void Insert_ContainsAllColumns()
        {
            var line = new AbstractMangosUnitConditionLine { Id = -2, Flags = 1 };
            line.SetClause(0, new UnitConditionClause { Variable = 12, Op = 3, Value = 20 });
            line.SetClause(7, new UnitConditionClause { Variable = 76, Op = 1, Value = -5 });

            var sql = generator.BuildInsertQuery(new IMangosUnitConditionLine[] { line }).QueryString;
            StringAssert.Contains("`unit_condition`", sql);
            StringAssert.Contains("Id", sql);
            StringAssert.Contains("Flags", sql);
            for (int i = 0; i < 8; ++i)
            {
                StringAssert.Contains($"Variable_{i}", sql);
                StringAssert.Contains($"Op_{i}", sql);
                StringAssert.Contains($"Value_{i}", sql);
            }
            StringAssert.Contains("-2", sql);
            StringAssert.Contains("-5", sql);
        }
    }
}
