using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

 internal class DisablesEntryParameter : ICustomPickerContextualParameter<long>, IContextualParameter<long, DatabaseEntity>
{
    private readonly IParameter<long> spell;
    private readonly IParameter<long> quest;
    private readonly IParameter<long> map;
    private readonly IParameter<long> battleground;
    private readonly IParameter<long> achievement;
    private readonly IParameterPickerService parameterPickerService;

    public DisablesEntryParameter(IParameter<long> spell,
        IParameter<long> quest,
        IParameter<long> map,
        IParameter<long> battleground,
        IParameter<long> achievement,
        IParameterPickerService parameterPickerService)
    {
        this.spell = spell;
        this.quest = quest;
        this.map = map;
        this.battleground = battleground;
        this.achievement = achievement;
        this.parameterPickerService = parameterPickerService;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var type = entity.GetTypedValueOrThrow<long>("sourceType");
            IParameter<long> parameter = Parameter.Instance;
            if (type == 0)
                parameter = spell;
            else if (type == 1)
                parameter = quest;
            else if (type == 2)
                parameter = map;
            else if (type == 3)
                parameter = battleground;
            else if (type == 4)
                parameter = achievement;
            return parameterPickerService.PickParameter(parameter, value);
        }
        return parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
    
    public string ToString(long value, DatabaseEntity context)
    {
        var type = context.GetTypedValueOrThrow<long>("sourceType");
        IParameter<long> parameter = Parameter.Instance;
        if (type == 0)
            parameter = spell;
        else if (type == 1)
            parameter = quest;
        else if (type == 2)
            parameter = map;
        else if (type == 3)
            parameter = battleground;
        else if (type == 4)
            parameter = achievement;
        return parameter.ToString(value);
    }
}