using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Exporter
{
    // cmangos has no TC-style conditions table and WDE.Conditions (the real generator)
    // doesn't load on cmangos cores, but always-loaded consumers (e.g. WDE.DatabaseEditors'
    // QueryGenerator) still inject IConditionQueryGenerator, so they get an empty-query stub
    [AutoRegister]
    [RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
    public class StubConditionQueryGenerator : IConditionQueryGenerator
    {
        public IQuery BuildDeleteQuery(IDatabaseProvider.ConditionKey conditionKey) => Queries.Empty(DataDatabaseType.World);

        public IQuery BuildInsertQuery(IReadOnlyList<IConditionLine> conditions) => Queries.Empty(DataDatabaseType.World);
    }
}
