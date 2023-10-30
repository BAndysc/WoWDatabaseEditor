using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.LootEditor.Parameters;

public class LootParameter : IParameter<long>, ICustomPickerParameter<long>
{
    private readonly ILootPickerService pickerService;
    private readonly LootSourceType sourceType;
    public string? Prefix => null;
    public bool HasItems => true;

    public LootParameter(ILootPickerService pickerService, LootSourceType sourceType)
    {
        this.pickerService = pickerService;
        this.sourceType = sourceType;
    }
    
    public string ToString(long value)
    {
        return value.ToString();
    }

    public Dictionary<long, SelectOption>? Items => null;
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await pickerService.PickLoot(sourceType);
        if (result.HasValue)
            return (result.Value, true);
        
        return (default, false);
    }
}