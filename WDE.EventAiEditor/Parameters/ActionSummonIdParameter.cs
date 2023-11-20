using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.EventAiEditor.Parameters;

public class ActionSummonIdParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
{
    private readonly IMangosDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;

    public ActionSummonIdParameter(IMangosDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val < 0)
            return ToString(val);

        var result = await databaseProvider.GetCreatureAiSummon((uint)val);

        if (result == null)
            return $"[p=2]{val}[/p] (missing position in creature_ai_summons!)";

        var spawnTimer = result.SpawnTimeSeconds == 0 ? "" : $" with duration [p=2]{result.SpawnTimeSeconds}[/p] seconds";
        return $"[p=2]({result.X:0.00}, {result.Y:0.00}, {result.Z:0.00}, {result.O:0.00})[/p]{spawnTimer}";
    }

    public string ToString(long value)
    {
        return $"[p=2]{value}[/p]";
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable("creature_ai_summons"), default, "id", value);
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;

    public Dictionary<long, SelectOption>? Items => null;
}