using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.TableData;
using WDE.Common.Utils;

namespace WDE.Parameters.Parameters;

public abstract class BaseQuestObjectiveParameter : ICustomPickerContextualParameter<long>
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IParameterPickerService parameterPickerService;
    private readonly IParameterFactory parameterFactory;
    private readonly bool byIndex;

    public bool AllowUnknownItems => true;

    public BaseQuestObjectiveParameter(ICachedDatabaseProvider databaseProvider,
        IItemFromListProvider itemFromListProvider,
        ITabularDataPicker tabularDataPicker,
        IParameterPickerService parameterPickerService,
        IParameterFactory parameterFactory,
        bool byIndex)
    {
        
        this.databaseProvider = databaseProvider;
        this.itemFromListProvider = itemFromListProvider;
        this.tabularDataPicker = tabularDataPicker;
        this.parameterPickerService = parameterPickerService;
        this.parameterFactory = parameterFactory;
        this.byIndex = byIndex;
    }

    protected abstract long? GetQuestIdFromContext(object? context);

    private string? GetObjectName(IQuestObjective questObjective)
    {
         switch (questObjective.Type)
         {
             case QuestObjectiveType.Monster:
             case QuestObjectiveType.TalkTo:
             case QuestObjectiveType.WinPetBattleAgainstNpc:
             case QuestObjectiveType.DefeatBattlePet:
                 return databaseProvider.GetCachedCreatureTemplate((uint)questObjective.ObjectId)?.Name ?? "NPC " + questObjective.ObjectId;
             case QuestObjectiveType.Item:
                 var itemParameter = parameterFactory.Factory("ItemParameter");
                 return itemParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.GameObject:
                 return databaseProvider.GetCachedGameObjectTemplate((uint)questObjective.ObjectId)?.Name ?? "Object " + questObjective.ObjectId;
             case QuestObjectiveType.Currency:
             case QuestObjectiveType.HaveCurrency:
             case QuestObjectiveType.ObtainCurrency:
                 var currencyParameter = parameterFactory.Factory("CurrencyTypeParameter");
                 return currencyParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.LearnSpell:
                 var spellParameter = parameterFactory.Factory("SpellParameter");
                 return spellParameter.ToString(questObjective.ObjectId, ToStringOptions.WithoutNumber);
             case QuestObjectiveType.MinReputation:
             case QuestObjectiveType.MaxReputation:
                 var factionTemplateParameter = parameterFactory.Factory("FactionTemplateParameter");
                 return factionTemplateParameter.ToString(questObjective.ObjectId);
             case QuestObjectiveType.CriteriaTree:
                 var achievementParameter = parameterFactory.Factory("AchievementParameter");
                 return achievementParameter.ToString(questObjective.ObjectId);
         }
         return null;
    }
    
    private string? GetNameWithoutAmount(IQuestObjective questObjective)
    {
         if (!string.IsNullOrEmpty(questObjective.Description))
            return questObjective.Description;

         var objectName = GetObjectName(questObjective);
         
         switch (questObjective.Type)
         {
             case QuestObjectiveType.Monster:
             case QuestObjectiveType.TalkTo:
                 return objectName;
             case QuestObjectiveType.Item:
                 return "Get " + objectName;
             case QuestObjectiveType.GameObject:
                 return objectName;
             case QuestObjectiveType.Currency:
             case QuestObjectiveType.HaveCurrency:
             case QuestObjectiveType.ObtainCurrency:
                 return "Get " + objectName;
             case QuestObjectiveType.LearnSpell:
                 return "Learn " + objectName;
             case QuestObjectiveType.MinReputation:
             case QuestObjectiveType.MaxReputation:
                 return "Earn reputation with " + objectName;
             case QuestObjectiveType.Money:
                 return "Money";
             case QuestObjectiveType.PlayerKills:
                 return "Slain players";
             case QuestObjectiveType.AreaTrigger:
                 return "Reach areatrigger " + questObjective.ObjectId;
             case QuestObjectiveType.WinPetBattleAgainstNpc:
                 return "Defeat " + objectName;
             case QuestObjectiveType.DefeatBattlePet:
                 return objectName + " defeated";
             case QuestObjectiveType.WinPvPPetBattles:
                 return "Win PvP pet battles";
             case QuestObjectiveType.CriteriaTree:
                 return "Complete " + objectName;
             case QuestObjectiveType.ProgressBar:
                 return "Progressbar";
             default:
                 return "Objective " + questObjective.ObjectiveId;
         }
    }

    private string? GenerateNameForObjective(IQuestObjective questObjective)
    {
        if (questObjective.Amount <= 1)
            return GetNameWithoutAmount(questObjective);
        return $"{questObjective.Amount}x " + GetNameWithoutAmount(questObjective);
    }
    
    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var questId = GetQuestIdFromContext(context);
        if (!questId.HasValue)
            return await parameterPickerService.PickParameter(Parameter.Instance, value);

        var objectives = await databaseProvider.GetQuestObjectives((uint)questId.Value);

        var defaultSelection = -1;
        if (byIndex)
            defaultSelection = objectives.IndexIf(x => x.StorageIndex == value);
        else
            defaultSelection = objectives.IndexIf(x => x.ObjectiveId == value);
        
        var result = await tabularDataPicker.PickRow(new TabularDataBuilder<IQuestObjective>()
            .SetData(objectives.AsIndexedCollection())
            .SetColumns(new ITabularDataColumn[]
            {
                new TabularDataColumn(nameof(IQuestObjective.ObjectiveId), "Objective id", 80),
                new TabularDataSyncColumn<IQuestObjective>(".", "Description", GenerateNameForObjective, 200),
                new TabularDataColumn(nameof(IQuestObjective.Type), "Type", 80),
                new TabularDataColumn(nameof(IQuestObjective.ObjectId), "Object id", 80),
                new TabularDataSyncColumn<IQuestObjective>(".", "Object name", GetObjectName),
                new TabularDataColumn(nameof(IQuestObjective.StorageIndex), "Storage index", 80)
            })
            .SetFilter((x, s) => x.ObjectiveId.Contains(s) ||
                                 (x.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                 x.ObjectId.Contains(s) ||
                                 (GetObjectName(x)?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))
            .SetExactMatchPredicate((x, s) => x.ObjectiveId.Is(s))
            .SetExactMatchCreator(s =>
            {
                if (uint.TryParse(s, out var ui))
                    return new AbstractQuestObjective{ ObjectiveId = ui, Description = "Unknown objective", StorageIndex = (int)ui };
                return null;
            })
            .SetTitle("Pick quest objective")
            .Build(), defaultSelection, value == 0 ? "" : value.ToString());

        if (result != null)
            return (byIndex ? result.StorageIndex : result.ObjectiveId, true);
        return (0, false);
    }
    
    public async Task<string> ToStringAsync(long value, CancellationToken token, object? context)
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
    public string ToString(long value, object? context) => ToString(value);
}