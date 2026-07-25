using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackMangosConditionQueryGenerator : IMangosConditionQueryGenerator
{
    public IQuery BuildDeleteQuery(IReadOnlyList<uint> entries) => Queries.Empty(DataDatabaseType.World);

    public IQuery BuildInsertQuery(IReadOnlyList<IMangosConditionLine> conditions) => Queries.Empty(DataDatabaseType.World);
}
