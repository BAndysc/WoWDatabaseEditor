using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class SmartQuestObjectiveParameter : IAsyncContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IParameterPickerService parameterPickerService;
    public bool AllowUnknownItems => true;

    public SmartQuestObjectiveParameter(IDatabaseProvider databaseProvider, 
        IItemFromListProvider itemFromListProvider,
        IParameterPickerService parameterPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.itemFromListProvider = itemFromListProvider;
        this.parameterPickerService = parameterPickerService;
    }

    private long? GetQuestIdFromContext(SmartBaseElement? context)
    {
        if (context == null)
            return null;

        return context.GetParameter(0).Value;
    }

    private string GenerateNameForObjective(IQuestObjective questObjective)
    {
        if (!string.IsNullOrEmpty(questObjective.Description))
            return questObjective.Description;

        var objectId = databaseProvider.GetCreatureTemplate((uint)questObjective.ObjectId);
        return objectId?.Name ?? "Objective " + questObjective.ObjectiveId;
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var questId = GetQuestIdFromContext(context as SmartBaseElement);
        
        if (!questId.HasValue)
            return await parameterPickerService.PickParameter(Parameter.Instance, value);

        var objectives = await databaseProvider.GetQuestObjectives((uint)questId.Value);
        
        Dictionary<long, SelectOption> options = new();
        foreach (var objective in objectives)
        {
            options.Add(objective.StorageIndex, new SelectOption(GenerateNameForObjective(objective)));
        }

        var result = await itemFromListProvider.GetItemFromList(options, false, value, "Pick quest objective");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, SmartBaseElement context)
    {
        var questId = GetQuestIdFromContext(context);

        if (!questId.HasValue)
            return ToString(value);

        var objective = await databaseProvider.GetQuestObjective((uint)questId, (int)value);

        if (objective == null)
            return ToString(value);
        
        return GenerateNameForObjective(objective) + " (" + value + ")";
    }

    public string? Prefix => null;
    public bool HasItems => true;
    
    public string ToString(long value) => "Objective index " + value;
    
    public Dictionary<long, SelectOption>? Items => null;

    public string ToString(long value, SmartBaseElement context) => ToString(value);
}