using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace WDE.DatabaseEditors.Parameters;

public class BroadcastTextParameter : IParameter<long>, IAsyncParameter<long>, ICustomPickerParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;

    public BroadcastTextParameter(IDatabaseProvider databaseProvider, ITableEditorPickerService tableEditorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        if (val >= uint.MaxValue || val < 0)
            return ToString(val);
        
        var result = await databaseProvider.GetBroadcastTextByIdAsync((uint)val);
        var text = (string.IsNullOrEmpty(result?.Text) ? result?.Text1 : result?.Text);
        if (text == null || result == null)
            return ToString(val);

        return $"{text.TrimToLength(60)} ({val})";
    }

    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn("broadcast_text", default, "Id", value);
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    
    public string ToString(long value) => "broadcast text " + value;

    public Dictionary<long, SelectOption>? Items => null;
}