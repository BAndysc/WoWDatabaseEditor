using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

internal class InstanceSpawnInfoSpawnIdParameter : ICustomPickerContextualParameter<long>
{
    private readonly IParameter<long> creatureParameter;
    private readonly IParameter<long> gameobjectParameter;
    private readonly IParameterPickerService parameterPickerService;

    public InstanceSpawnInfoSpawnIdParameter(IParameter<long> creature,
        IParameter<long> gameobject, IParameterPickerService parameterPickerService)
    {
        this.creatureParameter = creature;
        this.gameobjectParameter = gameobject;
        this.parameterPickerService = parameterPickerService;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var spawnType= entity.GetTypedValueOrThrow<long>("spawnType");
            return parameterPickerService.PickParameter(spawnType == 0 ? creatureParameter : gameobjectParameter, value);
        }
        return parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
}