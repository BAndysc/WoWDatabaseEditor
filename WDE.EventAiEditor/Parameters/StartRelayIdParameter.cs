using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Parameters;

public class StartRelayIdParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
{
    private readonly IMangosDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;

    public StartRelayIdParameter(IMangosDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val >= 0)
            return ToString(val);

        var result = await databaseProvider.GetScriptRandomTemplates((uint)-val, IMangosDatabaseProvider.RandomTemplateType.RelayScript);

        if (result == null || result.Count == 0)
            return $"random relay script from template [p=2]{val}[/p] (missing template!)";

        if (result.Count == 1)
            return $"relay script {result[0].Value} from template [p=0]{val}[/p]";
        
        return $"random relay script: {string.Join(", ", result.Select(r => r.Value))} from template [p=0]{val}[/p]";
    }

    public string ToString(long value)
    {
        return $"relay script [p=0]{value}[/p]";
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable("dbscript_random_templates"), default, "id", value, null, "type = 1");
        if (result.HasValue)
            return (-result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;

    public Dictionary<long, SelectOption>? Items => null;
}