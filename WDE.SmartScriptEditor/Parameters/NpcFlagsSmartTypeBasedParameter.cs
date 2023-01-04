using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Conditions.Shared;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class NpcFlagsSmartTypeBasedParameter : BaseContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>, IAffectsOtherParametersParameter
{
    private readonly IParameter<long> npcFlag1;
    private readonly IParameter<long> npcFlag2;
    private readonly IParameterPickerService parameterPickerService;

    public override string? Prefix => null;
    public override bool HasItems => true;
    public bool AllowUnknownItems => true;
    public override string ToString(long value) => value.ToString();
    public override Dictionary<long, SelectOption>? Items => null;
    
    public NpcFlagsSmartTypeBasedParameter(IParameter<long> npcFlag1, IParameter<long> npcFlag2,
        IParameterPickerService parameterPickerService)
    {
        this.npcFlag1 = npcFlag1;
        this.npcFlag2 = npcFlag2;
        this.parameterPickerService = parameterPickerService;
    }
    
    private int GetTypeForContext(object context)
    {
        var element = context as SmartBaseElement;
        if (element == null)
            return 0;

        var type = element.GetParameter(1); // 2nd parameter is type
        return (int)type.Value;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var type = GetTypeForContext(context);
        var parameter = type == 0 ? npcFlag1 : npcFlag2;
        return await parameterPickerService.PickParameter(parameter, value);
    }

    public override string ToString(long value, SmartBaseElement context)
    {
        var type = GetTypeForContext(context);
        var parameter = type == 0 ? npcFlag1 : npcFlag2;
        return parameter.ToString(value);
    }

    public IEnumerable<int> AffectedParameters()
    {
        yield return 0;
    }
}