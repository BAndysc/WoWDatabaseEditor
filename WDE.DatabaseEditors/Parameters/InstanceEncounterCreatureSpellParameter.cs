using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

internal class InstanceEncounterCreatureSpellParameter : ICustomPickerContextualParameter<long>, IContextualParameter<long, DatabaseEntity>
{
    private readonly IParameter<long> creatureParameter;
    private readonly IParameter<long> spellParameter;
    private readonly IParameterPickerService parameterPickerService;

    public InstanceEncounterCreatureSpellParameter(IParameter<long> creature,
        IParameter<long> spell, IParameterPickerService parameterPickerService)
    {
        this.creatureParameter = creature;
        this.spellParameter = spell;
        this.parameterPickerService = parameterPickerService;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var creditType = entity.GetTypedValueOrThrow<long>("creditType");
            return parameterPickerService.PickParameter(creditType == 0 ? creatureParameter : spellParameter, value);
        }
        return parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
    public string ToString(long value, DatabaseEntity context)
    {
        var creditType = context.GetTypedValueOrThrow<long>("creditType");
        if (creditType == 0)
            return creatureParameter.ToString(value);
        else if (creditType == 1)
            return spellParameter.ToString(value);
        return value.ToString();
    }
}