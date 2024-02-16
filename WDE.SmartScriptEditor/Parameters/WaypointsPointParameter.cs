using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class WaypointsPointParameter : IParameter<long>, ICustomPickerContextualParameter<long>
{
    private readonly ITableEditorPickerService pickerService;
    private readonly IParameterPickerService parameterPickerService;

    public WaypointsPointParameter(ITableEditorPickerService pickerService,
        IParameterPickerService parameterPickerService)
    {
        this.pickerService = pickerService;
        this.parameterPickerService = parameterPickerService;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is SmartBaseElement element)
        {
            var pathId = element.GetParameter(1).Value;
            var newValue = await pickerService.PickByColumn(DatabaseTable.WorldTable("waypoints"), new DatabaseKey(pathId), "pointid", value, null, null);
            return (newValue ?? 0, newValue.HasValue);
        }
        return await parameterPickerService.PickParameter(Parameter.Instance, value);
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