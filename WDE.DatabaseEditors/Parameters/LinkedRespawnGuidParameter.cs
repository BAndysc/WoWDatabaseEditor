using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

internal class LinkedRespawnGuidParameter : ICustomPickerContextualParameter<long>
{
    private readonly bool isMaster;
    private readonly IParameter<long> creatureParameter;
    private readonly IParameter<long> gameobjectParameter;
    private readonly IParameterPickerService parameterPickerService;

    public LinkedRespawnGuidParameter(bool isMaster, IParameter<long> creature,
        IParameter<long> gameobject, IParameterPickerService parameterPickerService)
    {
        this.isMaster = isMaster;
        this.creatureParameter = creature;
        this.gameobjectParameter = gameobject;
        this.parameterPickerService = parameterPickerService;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var linkType = entity.GetTypedValueOrThrow<long>("linkType");
            bool creature = isMaster ? (linkType is 0 or 3) : (linkType is 0 or 1);
            return parameterPickerService.PickParameter(creature ? creatureParameter : gameobjectParameter, value);
        }
        return parameterPickerService.PickParameter(Parameter.Instance, value);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    public string ToString(long value) => value.ToString();
    public Dictionary<long, SelectOption>? Items => null;
}