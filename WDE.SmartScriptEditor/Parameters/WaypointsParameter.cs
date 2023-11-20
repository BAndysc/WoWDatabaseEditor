using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.SmartScriptEditor.Parameters;

public class WaypointsParameter : IParameter<long>, ICustomPickerParameter<long>
{
    private readonly ITableEditorPickerService pickerService;

    public WaypointsParameter(ITableEditorPickerService pickerService)
    {
        this.pickerService = pickerService;
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        await pickerService.ShowTable(DatabaseTable.WorldTable("waypoints"), null, new DatabaseKey(value));
        return (0, false);
    }

    public string? Prefix => null;
    
    public bool HasItems => true;

    public bool AllowUnknownItems => true;

    public string ToString(long value)
    {
        return value.ToString();
    }

    public Dictionary<long, SelectOption>? Items => null;
}