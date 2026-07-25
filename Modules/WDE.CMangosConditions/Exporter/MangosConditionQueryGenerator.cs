using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Exporter
{
    [AutoRegister]
    public class MangosConditionQueryGenerator : IMangosConditionQueryGenerator
    {
        private static readonly DatabaseTable ConditionsTable = DatabaseTable.WorldTable("conditions");

        public IQuery BuildDeleteQuery(IReadOnlyList<uint> entries)
        {
            if (entries.Count == 0)
                return Queries.Empty(DataDatabaseType.World);
            return Queries.Table(ConditionsTable)
                .WhereIn("condition_entry", entries.Distinct().OrderBy(x => x).ToList())
                .Delete();
        }

        public IQuery BuildInsertQuery(IReadOnlyList<IMangosConditionLine> conditions)
        {
            if (conditions.Count == 0)
                return Queries.Empty(DataDatabaseType.World);
            return Queries.Table(ConditionsTable)
                .BulkInsert(conditions.Select(c => new
                {
                    condition_entry = c.ConditionEntry,
                    type = c.ConditionType,
                    value1 = c.Value1,
                    value2 = c.Value2,
                    value3 = c.Value3,
                    value4 = c.Value4,
                    flags = c.Flags,
                    comments = c.Comments ?? "",
                }));
        }
    }
}
