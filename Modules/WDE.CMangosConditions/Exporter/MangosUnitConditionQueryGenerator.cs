using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Exporter
{
    [AutoRegister]
    public class MangosUnitConditionQueryGenerator : IMangosUnitConditionQueryGenerator
    {
        private static readonly DatabaseTable UnitConditionTable = DatabaseTable.WorldTable("unit_condition");

        public IQuery BuildDeleteQuery(IReadOnlyList<int> ids)
        {
            if (ids.Count == 0)
                return Queries.Empty(DataDatabaseType.World);
            return Queries.Table(UnitConditionTable)
                .WhereIn("Id", ids.Distinct().OrderBy(x => x).ToList())
                .Delete();
        }

        public IQuery BuildInsertQuery(IReadOnlyList<IMangosUnitConditionLine> lines)
        {
            if (lines.Count == 0)
                return Queries.Empty(DataDatabaseType.World);
            return Queries.Table(UnitConditionTable)
                .BulkInsert(lines.Select(l => new
                {
                    Id = l.Id,
                    Flags = l.Flags,
                    Variable_0 = l.GetClause(0).Variable,
                    Variable_1 = l.GetClause(1).Variable,
                    Variable_2 = l.GetClause(2).Variable,
                    Variable_3 = l.GetClause(3).Variable,
                    Variable_4 = l.GetClause(4).Variable,
                    Variable_5 = l.GetClause(5).Variable,
                    Variable_6 = l.GetClause(6).Variable,
                    Variable_7 = l.GetClause(7).Variable,
                    Op_0 = l.GetClause(0).Op,
                    Op_1 = l.GetClause(1).Op,
                    Op_2 = l.GetClause(2).Op,
                    Op_3 = l.GetClause(3).Op,
                    Op_4 = l.GetClause(4).Op,
                    Op_5 = l.GetClause(5).Op,
                    Op_6 = l.GetClause(6).Op,
                    Op_7 = l.GetClause(7).Op,
                    Value_0 = l.GetClause(0).Value,
                    Value_1 = l.GetClause(1).Value,
                    Value_2 = l.GetClause(2).Value,
                    Value_3 = l.GetClause(3).Value,
                    Value_4 = l.GetClause(4).Value,
                    Value_5 = l.GetClause(5).Value,
                    Value_6 = l.GetClause(6).Value,
                    Value_7 = l.GetClause(7).Value,
                }));
        }
    }
}
