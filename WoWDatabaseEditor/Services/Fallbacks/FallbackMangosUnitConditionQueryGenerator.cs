using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackMangosUnitConditionQueryGenerator : IMangosUnitConditionQueryGenerator
{
    public IQuery BuildDeleteQuery(IReadOnlyList<int> ids) => Queries.Empty(DataDatabaseType.World);

    public IQuery BuildInsertQuery(IReadOnlyList<IMangosUnitConditionLine> lines) => Queries.Empty(DataDatabaseType.World);
}
