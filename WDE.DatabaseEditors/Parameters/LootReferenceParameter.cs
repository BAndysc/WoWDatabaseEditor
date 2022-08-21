using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Services;

namespace WDE.DatabaseEditors.Parameters;

public class LootReferenceParameter : ICustomPickerParameter<long>
{
    private readonly TableEditorPickerService pickerService;

    public LootReferenceParameter(TableEditorPickerService pickerService)
    {
        this.pickerService = pickerService;
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var pick = await pickerService.PickByColumn("reference_loot_template", new DatabaseKey(value), "Entry", value);
        return (pick ?? 0, pick.HasValue);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
}