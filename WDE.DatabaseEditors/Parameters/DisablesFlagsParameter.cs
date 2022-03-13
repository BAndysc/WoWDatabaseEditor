using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

internal class DisablesFlagsParameter : ICustomPickerContextualParameter<long>, IContextualParameter<long, DatabaseEntity>
{
    private readonly IParameter<long> spellFlags;
    private readonly IParameter<long> mapFlags;
    private readonly IParameter<long> vmapFlags;
    private readonly IParameterPickerService parameterPickerService;

    public DisablesFlagsParameter( 
        IParameter<long> spellFlags, 
        IParameter<long> mapFlags, 
        IParameter<long> vmapFlags,
        IParameterPickerService parameterPickerService)
    {
        this.parameterPickerService = parameterPickerService;
        this.spellFlags = spellFlags;
        this.mapFlags = mapFlags;
        this.vmapFlags = vmapFlags;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is DatabaseEntity entity)
        {
            var type = entity.GetTypedValueOrThrow<long>("sourceType");
            IParameter<long> parameter = Parameter.Instance;
            if (type == 0)
                parameter = spellFlags;
            else if (type == 2)
                parameter = mapFlags;
            else if (type == 6)
                parameter = vmapFlags;
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
            parameter = spellFlags;
        else if (type == 2)
            parameter = mapFlags;
        else if (type == 6)
            parameter = vmapFlags;
        return parameter.ToString(value);
    }
}