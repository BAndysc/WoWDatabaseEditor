using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class SmartScenarioStepParameter : IContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
{
    private readonly IDbcStore dbcStore;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IParameterPickerService parameterPickerService;
    public bool AllowUnknownItems => true;

    public SmartScenarioStepParameter(IDbcStore dbcStore, 
        IItemFromListProvider itemFromListProvider,
        IParameterPickerService parameterPickerService)
    {
        this.dbcStore = dbcStore;
        this.itemFromListProvider = itemFromListProvider;
        this.parameterPickerService = parameterPickerService;
    }

    private long? GetScenarioIdFromContext(SmartBaseElement? context)
    {
        if (context == null)
            return null;

        return context.GetParameter(0).Value;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var scenarioId = GetScenarioIdFromContext(context as SmartBaseElement);
        
        if (!scenarioId.HasValue || !dbcStore.ScenarioToStepStore.TryGetValue(scenarioId.Value, out var steps))
            return await parameterPickerService.PickParameter(Parameter.Instance, value);

        Dictionary<long, SelectOption> options = new();
        foreach (var (index, stepId) in steps)
        {
            if (dbcStore.ScenarioStepStore.TryGetValue(stepId, out var step))
            {
                options.Add(index, new SelectOption(step));
            }
            else
            {
                options.Add(index, new SelectOption("Unknown step " + index));
            }
        }

        var result = await itemFromListProvider.GetItemFromList(options, false, value, "Pick scenario step");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;
    
    public string ToString(long value) => "Step " + value;
    
    public Dictionary<long, SelectOption>? Items => null;

    public string ToString(long value, SmartBaseElement context)
    {
        var scenarioId = GetScenarioIdFromContext(context);
        if (!scenarioId.HasValue || 
            !dbcStore.ScenarioToStepStore.TryGetValue(scenarioId.Value, out var steps) ||
            !steps.TryGetValue(value, out var stepId) ||
            !dbcStore.ScenarioStepStore.TryGetValue(stepId, out var stepName))
            return ToString(value);
        return stepName + " (" + value + ")";
    }
}