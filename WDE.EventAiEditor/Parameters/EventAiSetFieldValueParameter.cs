using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Parameters;

public class EventAiSetFieldValueParameter : BaseContextualParameter<long, EventAiBaseElement>, ICustomPickerContextualParameter<long>
{
    private readonly IParameterPickerService parameterPickerService;
    private readonly IParameter<long> itemParameter;
    private readonly IParameter<long> dynamicFlags;
    private readonly IParameter<long> npcFlags;

    public EventAiSetFieldValueParameter(IParameterPickerService parameterPickerService,
        IParameter<long> itemParameter,
        IParameter<long> dynamicFlags,
        IParameter<long> npcFlags)
    {
        this.parameterPickerService = parameterPickerService;
        this.itemParameter = itemParameter;
        this.dynamicFlags = dynamicFlags;
        this.npcFlags = npcFlags;
    }

    public Task<(long, bool)> PickValue(long value, object context)
    {
        var fieldType = ((EventAiBaseElement)context).GetParameter(0).Value;
        if (fieldType is 56 or 57 or 58)
            return parameterPickerService.PickParameter(itemParameter, value);
        if (fieldType is 79)
            return parameterPickerService.PickParameter(dynamicFlags, value);
        if (fieldType is 82)
            return parameterPickerService.PickParameter(npcFlags, value);
        return Task.FromResult<(long, bool)>((0, false));
    }

    public override string? Prefix => null;
    public override bool HasItems => true;
    public bool AllowUnknownItems => true;
    
    public override string ToString(long value)
    {
        return value.ToString();
    }

    public override Dictionary<long, SelectOption>? Items => null;
    
    public override string ToString(long value, EventAiBaseElement context)
    {
        var fieldType = context.GetParameter(0).Value;
        if (fieldType is 56 or 57 or 58)
            return itemParameter.ToString(value);
        if (fieldType is 79)
            return dynamicFlags.ToString(value);
        if (fieldType is 82)
            return npcFlags.ToString(value);
        return value.ToString();
    }

    public Dictionary<long, SelectOption>? ItemsForContext(EventAiBaseElement context)
    {
        var fieldType = context.GetParameter(0).Value;
        if (fieldType is 56 or 57 or 58)
            return itemParameter.Items;
        if (fieldType is 79)
            return dynamicFlags.Items;
        if (fieldType is 82)
            return npcFlags.Items;
        return null;
    }
}