using System;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackMangosUnitConditionService : IMangosUnitConditionService
{
    public Task<int?> EditUnitCondition(int? id, string? customTitle = null)
        => Task.FromResult<int?>(null);

    public Task<IMangosUnitConditionLine?> EditUnitConditionInMemory(IMangosUnitConditionLine? line,
        int suggestedNewId, string? customTitle = null)
        => Task.FromResult<IMangosUnitConditionLine?>(null);

    public string BuildReadable(IMangosUnitConditionLine line) => "";

    public Task<IMangosUnitConditionLine?> LoadUnitCondition(int id)
        => Task.FromResult<IMangosUnitConditionLine?>(null);

    public Task<int> GetFreeUnitConditionId(int localMin = 0)
        => Task.FromResult(Math.Min(localMin, -1) - 1);
}
