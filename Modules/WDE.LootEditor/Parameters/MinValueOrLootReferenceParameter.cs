using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.LootEditor.Parameters;

public class MinValueOrLootReferenceParameter : IParameter<long>, ICustomPickerParameter<long>
{
    private readonly ILootPickerService pickerService;
    public string? Prefix => null;
    public bool HasItems => true;

    public MinValueOrLootReferenceParameter(ILootPickerService pickerService)
    {
        this.pickerService = pickerService;
    }
    
    public string ToString(long value)
    {
        if (value >= 0)
            return value.ToString();
        return "ref " + (-value);
    }

    public Dictionary<long, SelectOption>? Items => null;
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await pickerService.PickLoot(LootSourceType.Reference);
        if (result.HasValue)
            return (-result.Value, true);
        
        return (default, false);
    }
}