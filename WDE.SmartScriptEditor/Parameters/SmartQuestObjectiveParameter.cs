using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Conditions.Shared;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class SmartQuestObjectiveParameter : IAsyncContextualParameter<long, SmartBaseElement>, 
    ICustomPickerContextualParameter<long>,
    IAsyncContextualParameter<long, IConditionViewModel>,
    IAffectedByOtherParametersParameter
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IParameterFactory parameterFactory;
    private readonly bool byIndex;
    public bool AllowUnknownItems => true;

    public SmartQuestObjectiveParameter(IDatabaseProvider databaseProvider, 
        IItemFromListProvider itemFromListProvider,
        IParameterPickerService parameterPickerService,
        IParameterFactory parameterFactory,
        bool byIndex)
    {
        this.databaseProvider = databaseProvider;
        this.itemFromListProvider = itemFromListProvider;
        this.parameterPickerService = parameterPickerService;
        this.parameterFactory = parameterFactory;
        this.byIndex = byIndex;
    }
    
    private long? GetQuestIdFromContext(IConditionViewModel? context)
    {
        if (context == null)
            return null;

        return context.GetParameter(0).Value;
    }
    
    private long? GetQuestIdFromContext(SmartBaseElement? context)
    {
        if (context == null)
            return null;

        return context.GetParameter(0).Value;
    }

    private string GetNameWithoutAmount(IQuestObjective questObjective)
    {
         if (!string.IsNullOrEmpty(questObjective.Description))
            return questObjective.Description;

         switch (questObjective.Type)
         {
             case QuestObjectiveType.Monster:
             case QuestObjectiveType.TalkTo:
                 return databaseProvider.GetCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "NPC " + questObjective.ObjectId;
             case QuestObjectiveType.Item:
                 var itemParameter = parameterFactory.Factory("ItemParameter");
                 return "Get " + itemParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.GamObject:
                 return databaseProvider.GetGameObjectTemplate((uint)questObjective.ObjectId)?.Name ?? "Object " + questObjective.ObjectId;
             case QuestObjectiveType.Currency:
             case QuestObjectiveType.HaveCurrency:
             case QuestObjectiveType.ObtainCurrency:
                 var currencyParameter = parameterFactory.Factory("CurrencyParameter");
                 return "Get " + currencyParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.LearnSpell:
                 var spellParameter = parameterFactory.Factory("SpellParameter");
                 return "Learn " + spellParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.MinReputation:
             case QuestObjectiveType.MaxReputation:
                 var factionParameter = parameterFactory.Factory("FactionParameter");
                 return "Earn reputation with " + factionParameter.ToString(questObjective.ObjectId);
             case QuestObjectiveType.Money:
                 return "Money";
             case QuestObjectiveType.PlayerKills:
                 return "Slain players";
             case QuestObjectiveType.AreaTrigger:
                 return "Reach areatrigger " + questObjective.ObjectId;
             case QuestObjectiveType.WinPetBattleAgainstNpc:
                 return "Defeat " + (databaseProvider.GetCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "npc " + questObjective.ObjectId);
             case QuestObjectiveType.DefeatBattlePet:
                 return  (databaseProvider.GetCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "npc " + questObjective.ObjectId) + " defeated";
             case QuestObjectiveType.WinPvPPetBattles:
                 return "Win PvP pet battles";
             case QuestObjectiveType.CriteriaTree:
                 var achievementParameter = parameterFactory.Factory("AchievementParameter");
                 return "Complete " + achievementParameter.ToString(questObjective.ObjectId);
             case QuestObjectiveType.ProgressBar:
                 return "Progressbar";
             default:
                 return "Objective " + questObjective.ObjectiveId;
         }
    }
    
    private string GenerateNameForObjective(IQuestObjective questObjective)
    {
        if (questObjective.Amount <= 1)
            return GetNameWithoutAmount(questObjective);
        return GetNameWithoutAmount(questObjective) + " (" + questObjective.Amount + ")";
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var questId = GetQuestIdFromContext(context as SmartBaseElement);

        if (!questId.HasValue)
            questId = GetQuestIdFromContext(context as IConditionViewModel);
        
        if (!questId.HasValue)
            return await parameterPickerService.PickParameter(Parameter.Instance, value);

        var objectives = await databaseProvider.GetQuestObjectives((uint)questId.Value);
        
        Dictionary<long, SelectOption> options = new();
        foreach (var objective in objectives)
        {
            options.Add(byIndex ? objective.StorageIndex : objective.ObjectiveId, new SelectOption(GenerateNameForObjective(objective)));
        }

        var result = await itemFromListProvider.GetItemFromList(options, false, value, "Pick quest objective");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, SmartBaseElement context)
    {
        var questId = GetQuestIdFromContext(context);

        return await ToString(value, questId);
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, IConditionViewModel context)
    {
        var questId = GetQuestIdFromContext(context);

        return await ToString(value, questId);
    }

    private async Task<string> ToString(long value, long? questId)
    {
        if (!questId.HasValue)
            return ToString(value);

        IQuestObjective? objective;
        if (byIndex)
            objective = await databaseProvider.GetQuestObjective((uint)questId, (int)value);
        else
            objective = await databaseProvider.GetQuestObjectiveById((uint)value);

        if (objective == null)
            return ToString(value);

        return GenerateNameForObjective(objective) + " (" + value + ")";
    }

    public string? Prefix => null;
    public bool HasItems => true;
    
    public string ToString(long value) => "Objective index " + value;
    
    public Dictionary<long, SelectOption>? Items => null;

    public string ToString(long value, SmartBaseElement context) => ToString(value);
    public string ToString(long value, IConditionViewModel context) => ToString(value);
    
    public IEnumerable<int> AffectedByParameters()
    {
        yield return 0;
    }
}