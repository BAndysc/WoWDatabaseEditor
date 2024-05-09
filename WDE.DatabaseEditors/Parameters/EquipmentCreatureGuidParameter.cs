using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

public class EquipmentCreatureGuidParameter : ICustomPickerContextualParameter<long>
{
    private readonly ITableEditorPickerService pickerService;
    private readonly IParameterPickerService parameterPickerService;

    public EquipmentCreatureGuidParameter(ITableEditorPickerService pickerService,
        IParameterPickerService parameterPickerService)
    {
        this.pickerService = pickerService;
        this.parameterPickerService = parameterPickerService;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var entry = entity.GetTypedValueOrThrow<long>(new ColumnFullName(null, "id"));
            var result = await pickerService.PickByColumn(DatabaseTable.WorldTable("creature_equip_template"), new DatabaseKey(entry), "ID", value);
            return (result ?? 0, result.HasValue);
        }

        return await parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
}