using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

internal class TrainerRequirementParameter : IContextualParameter<long, DatabaseEntity>, ICustomPickerContextualParameter<long>
{
    private readonly IParameter<long> @class;
    private readonly IParameter<long> race;
    private readonly IParameter<long> spell;
    private readonly IParameterPickerService parameterPickerService;

    public TrainerRequirementParameter(IParameter<long> @class, 
        IParameter<long> race, 
        IParameter<long> spell,
        IParameterPickerService parameterPickerService)
    {
        this.@class = @class;
        this.race = race;
        this.spell = spell;
        this.parameterPickerService = parameterPickerService;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var type = entity.GetTypedValueOrThrow<long>("Type");
            if (type == 0) // class
                return parameterPickerService.PickParameter(@class, value);
            if (type == 1) // race
                return parameterPickerService.PickParameter(race, value);
            if (type == 2) // spell
                return parameterPickerService.PickParameter(spell, value);
        }
        return parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    
    public string ToString(long value)
    {
        return value.ToString();
    }

    public Dictionary<long, SelectOption>? Items => null;

    public Dictionary<long, SelectOption>? ItemsForContext(DatabaseEntity context)
    {
        var type = context.GetTypedValueOrThrow<long>("Type");
        if (type == 0) // class
            return @class.Items;
        if (type == 1) // race
            return race.Items;
        if (type == 2) // spell
            return spell.Items;
        return Items;
    }

    public string ToString(long value, DatabaseEntity context)
    {
        var type = context.GetTypedValueOrThrow<long>("Type");
        if (type == 0) // class
            return @class.ToString(value);
        if (type == 1) // race
            return race.ToString(value);
        if (type == 2) // spell
            return spell.ToString(value);
        return value.ToString();
    }
}