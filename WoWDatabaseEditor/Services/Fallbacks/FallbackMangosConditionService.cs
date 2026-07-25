using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
public class FallbackMangosConditionService : IMangosConditionService
{
    public Task<IReadOnlyList<IMangosConditionLine>?> EditConditions(IReadOnlyList<IMangosConditionLine> conditions, string? customTitle = null,
        MangosConditionSourceTarget? sourceTarget = null)
        => Task.FromResult<IReadOnlyList<IMangosConditionLine>?>(null);

    public Task<uint?> EditConditions(uint rootEntry, string? customTitle = null,
        MangosConditionSourceTarget? sourceTarget = null)
        => Task.FromResult<uint?>(null);

    public Task<MangosConditionTreeEditResult?> EditConditionTree(uint rootEntry, IReadOnlyList<IMangosConditionLine> knownConditions,
        uint firstFreeEntry, string? customTitle = null, MangosConditionSourceTarget? sourceTarget = null)
        => Task.FromResult<MangosConditionTreeEditResult?>(null);

    public string BuildReadable(uint rootEntry, IReadOnlyList<IMangosConditionLine> knownConditions,
        MangosConditionSourceTarget? sourceTarget = null) => "";

    public Task<IReadOnlyList<IMangosConditionLine>> LoadConditionsClosure(uint rootEntry)
        => Task.FromResult<IReadOnlyList<IMangosConditionLine>>(Array.Empty<IMangosConditionLine>());

    public Task<uint> GetFirstFreeConditionEntry(uint localMax)
        => Task.FromResult(localMax + 1);
}
