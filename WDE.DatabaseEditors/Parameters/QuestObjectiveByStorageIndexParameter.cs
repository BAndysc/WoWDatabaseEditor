using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

public class QuestObjectiveByStorageIndexParameter : IAsyncContextualParameter<long, DatabaseEntity>, 
    ICustomPickerContextualParameter<long>
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IParameterFactory parameterFactory;
    private readonly string questIdColumnName;
    public bool AllowUnknownItems => true;

    public QuestObjectiveByStorageIndexParameter(ICachedDatabaseProvider databaseProvider, 
        IItemFromListProvider itemFromListProvider,
        IParameterPickerService parameterPickerService,
        IParameterFactory parameterFactory,
        string questIdColumnName)
    {
        this.databaseProvider = databaseProvider;
        this.itemFromListProvider = itemFromListProvider;
        this.parameterPickerService = parameterPickerService;
        this.parameterFactory = parameterFactory;
        this.questIdColumnName = questIdColumnName;
    }
    
    private long? GetQuestIdFromContext(DatabaseEntity? context)
    {
        if (context == null)
            return null;

        return context.GetTypedValueOrThrow<long>(questIdColumnName);
    }

    private string GetNameWithoutAmount(IQuestObjective questObjective)
    {
         if (!string.IsNullOrEmpty(questObjective.Description))
            return questObjective.Description;

         switch (questObjective.Type)
         {
             case QuestObjectiveType.Monster:
             case QuestObjectiveType.TalkTo:
                 return databaseProvider.GetCachedCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "NPC " + questObjective.ObjectId;
             case QuestObjectiveType.Item:
                 var itemParameter = parameterFactory.Factory("ItemParameter");
                 return "Get " + itemParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.GamObject:
                 return databaseProvider.GetCachedGameObjectTemplate((uint)questObjective.ObjectId)?.Name ?? "Object " + questObjective.ObjectId;
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
                 var FactionTemplateParameter = parameterFactory.Factory("FactionTemplateParameter");
                 return "Earn reputation with " + FactionTemplateParameter.ToString(questObjective.ObjectId);
             case QuestObjectiveType.Money:
                 return "Money";
             case QuestObjectiveType.PlayerKills:
                 return "Slain players";
             case QuestObjectiveType.AreaTrigger:
                 return "Reach areatrigger " + questObjective.ObjectId;
             case QuestObjectiveType.WinPetBattleAgainstNpc:
                 return "Defeat " + (databaseProvider.GetCachedCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "npc " + questObjective.ObjectId);
             case QuestObjectiveType.DefeatBattlePet:
                 return  (databaseProvider.GetCachedCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "npc " + questObjective.ObjectId) + " defeated";
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
        var questId = GetQuestIdFromContext(context as DatabaseEntity);

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

    public async Task<string> ToStringAsync(long value, CancellationToken token, DatabaseEntity context)
    {
        var questId = GetQuestIdFromContext(context);

        return await ToString(value, questId);
    }

    private async Task<string> ToString(long value, long? questId)
    {
        if (!questId.HasValue)
            return ToString(value);

        IQuestObjective? objective;
        objective = await databaseProvider.GetQuestObjective((uint)questId, (int)value);

        if (objective == null)
            return ToString(value);

        return GenerateNameForObjective(objective) + " (" + value + ")";
    }

    public string? Prefix => null;
    public bool HasItems => true;
    
    public string ToString(long value) => "Objective index " + value;
    
    public Dictionary<long, SelectOption>? Items => null;

    public string ToString(long value, DatabaseEntity context) => ToString(value);
}