using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;

namespace WDE.DatabaseEditors.Parameters;

public class CreatureTemplateSpellListIdParameter : ICustomPickerParameter<long>
{
    private readonly IParameter<long> creatureTemplates;
    private readonly IParameterPickerService parameterPickerService;

    public CreatureTemplateSpellListIdParameter(IParameter<long> creatureTemplates,
        IParameterPickerService parameterPickerService)
    {
        this.creatureTemplates = creatureTemplates;
        this.parameterPickerService = parameterPickerService;
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var phaseId = value % 100;
        var result = await parameterPickerService.PickParameter(creatureTemplates, value / 100);
        if (result.ok)
            return (result.value * 100 + phaseId, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;

    public string ToString(long value)
    {
        var entry = value / 100;
        var phaseId = value % 100;
        return $"{creatureTemplates.ToString(entry)} - Phase {phaseId}";
    }

    public Dictionary<long, SelectOption>? Items => null;
}