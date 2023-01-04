using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class SmartScenarioStepParameter : BaseContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
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

        if (context is SmartAction)
            return null;

        return context.GetParameter(0).Value;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var scenarioId = GetScenarioIdFromContext(context as SmartBaseElement);

        Dictionary<long, SelectOption> options = new();

        if (!scenarioId.HasValue || !dbcStore.ScenarioToStepStore.TryGetValue(scenarioId.Value, out var steps))
        {
            foreach (var step in dbcStore.ScenarioStepStore)
                options.Add(step.Key, new SelectOption(step.Value));
        }
        else
        {
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
        }


        var result = await itemFromListProvider.GetItemFromList(options.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value), false, value, "Pick scenario step");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public override string? Prefix => null;
    public override bool HasItems => true;
    
    public override string ToString(long value) => "Step " + value;
    
    public override Dictionary<long, SelectOption>? Items => null;

    public override string ToString(long value, SmartBaseElement context)
    {
        var scenarioId = GetScenarioIdFromContext(context);
        if (!scenarioId.HasValue || scenarioId.Value == 0)
        {
            if (!dbcStore.ScenarioStepStore.TryGetValue(value, out var stepName2))
                return ToString(value);
            return stepName2 + " (" + value + ")";
        }
        if (!dbcStore.ScenarioToStepStore.TryGetValue(scenarioId.Value, out var steps) ||
            !steps.TryGetValue(value, out var stepId) ||
            !dbcStore.ScenarioStepStore.TryGetValue(stepId, out var stepName))
            return ToString(value);
        return stepName + " (" + value + ")";
    }
}