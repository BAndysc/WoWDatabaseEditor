using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackTrinityConditionManager : ITrinityConditionManager
{
    public IReadOnlyList<(int conditionType, int parameterIndex)> GetQuestConditionParameters() => Array.Empty<(int conditionType, int parameterIndex)>();
}

[FallbackAutoRegister]
public class FallbackConditionQueryGenerator : IConditionQueryGenerator
{
    public IQuery BuildDeleteQuery(IDatabaseProvider.ConditionKey conditionKey)
    {
        return Queries.Raw(DataDatabaseType.World, "-- not supported");
    }

    public IQuery BuildInsertQuery(IReadOnlyList<IConditionLine> conditions)
    {
        return Queries.Raw(DataDatabaseType.World, "-- not supported");
    }
}