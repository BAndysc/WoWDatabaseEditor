using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Collections;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Services;

[AutoRegister]
[SingleInstance]
public class QuestChainLoader : IQuestChainLoader
{
    private readonly IQuestTemplateSource source;
    private readonly IQuestChainEditorConfiguration configuration;
    private readonly IDatabaseProvider databaseProvider;

    public QuestChainLoader(IQuestTemplateSource source,
        IQuestChainEditorConfiguration configuration,
        IDatabaseProvider databaseProvider)
    {
        this.source = source;
        this.configuration = configuration;
        this.databaseProvider = databaseProvider;
    }

    public async Task LoadChain(uint[] entries, QuestStore quests, List<string>? nonFatalErrors = null, CancellationToken token = default)
    {
        HashSet<uint> loaded = new();
        Queue<uint> toLoad = new();
        Dictionary<uint, QuestModel> questModels = new();
        foreach (var entry in entries)
            toLoad.Enqueue(entry);

        MultiDictionary<uint, uint> mustBeCompleted = new(); // values are quests that must be completed before key
        MultiDictionary<uint, uint> mustBeActive = new(); // values are quests that must be active to get key
        MultiDictionary<uint, uint> breadcrumbs = new(); // values are quests that are breadcrumbs for key

        MultiDictionary<uint, uint> andGroups = new();
        MultiDictionary<uint, uint> orGroups = new();

        Dictionary<uint, int> questToExclusiveGroup = new();
        MultiDictionary<int, uint> exclusiveGroupToQuests = new();

        async Task RecursiveLoad()
        {
            while (toLoad.Count > 0)
            {
                var entry = toLoad.Dequeue();
                if (!loaded.Add(entry))
                    continue;

                var questModel = await quests.GetOrCreate(entry, source.GetTemplate);
                questModels[questModel.Entry] = questModel;
                token.ThrowIfCancellationRequested();
                var template = questModel.Template;

                if (template.PrevQuestId != 0)
                {
                    if (Math.Abs(template.PrevQuestId) == template.Entry)
                        nonFatalErrors?.Add($"Quest {template.Entry} has itself as previous quest. Ignoring.");
                    else if (template.PrevQuestId > 0)
                        mustBeCompleted.Add(template.Entry, (uint)template.PrevQuestId);
                    else if (template.PrevQuestId < 0)
                        mustBeActive.Add(template.Entry, (uint)(-template.PrevQuestId));

                    toLoad.Enqueue((uint)Math.Abs(template.PrevQuestId));
                }

                if (template.NextQuestId != 0)
                {
                    if (Math.Abs(template.NextQuestId) == template.Entry)
                        nonFatalErrors?.Add($"Quest {template.Entry} has itself as next quest. Ignoring.");
                    else if (template.NextQuestId > 0)
                        mustBeCompleted.Add((uint)template.NextQuestId, template.Entry);
                    else if (template.NextQuestId < 0)
                        mustBeActive.Add((uint)(-template.NextQuestId), template.Entry);
                    toLoad.Enqueue((uint)Math.Abs(template.NextQuestId));
                }

                if (template.BreadcrumbForQuestId != 0)
                {
                    if (Math.Abs(template.BreadcrumbForQuestId) == template.Entry)
                        nonFatalErrors?.Add($"Quest {template.Entry} has itself as breadcrumb quest. Ignoring.");
                    else if (template.BreadcrumbForQuestId > 0)
                        breadcrumbs.Add((uint)template.BreadcrumbForQuestId, template.Entry);
                    else if (template.BreadcrumbForQuestId < 0)
                    {
                        nonFatalErrors?.Add($"Quest {template.Entry} has negative breadcrumb quest id {template.BreadcrumbForQuestId}. Not sure what it is supposed to mean. Changed to positive breadcrumb quest id.");
                        breadcrumbs.Add((uint)-template.BreadcrumbForQuestId, template.Entry);
                        //throw new Exception("BreadcrumbForQuestId < 0 this is not legal");
                    }
                    toLoad.Enqueue((uint)Math.Abs(template.BreadcrumbForQuestId));
                }

                if (template.ExclusiveGroup != 0)
                {
                    if (template.ExclusiveGroup > 0)
                        orGroups.Add((uint)template.ExclusiveGroup, template.Entry);
                    else if (template.ExclusiveGroup < 0)
                        andGroups.Add((uint)(-template.ExclusiveGroup), template.Entry);
                    questToExclusiveGroup[template.Entry] = template.ExclusiveGroup;
                    exclusiveGroupToQuests.Add(template.ExclusiveGroup, template.Entry);
                }

                var byPrevious = await source.GetByPreviousQuestId(entry);
                token.ThrowIfCancellationRequested();
                foreach (var next in byPrevious)
                {
                    toLoad.Enqueue(next.Entry);
                }

                var byNext = await source.GetByNextQuestId(entry);
                token.ThrowIfCancellationRequested();
                foreach (var next in byNext)
                {
                    toLoad.Enqueue(next.Entry);
                }

                if (template.ExclusiveGroup != 0)
                {
                    var byExclusiveGroup = await source.GetByExclusiveGroup(template.ExclusiveGroup);
                    token.ThrowIfCancellationRequested();
                    foreach (var next in byExclusiveGroup)
                    {
                        toLoad.Enqueue(next.Entry);
                    }
                }

                var byBreadcrumb = await source.GetByBreadCrumbQuestId(entry);
                token.ThrowIfCancellationRequested();
                foreach (var next in byBreadcrumb)
                {
                    toLoad.Enqueue(next.Entry);
                }
            }
        }

        await RecursiveLoad();

        var factionOpposite = await source.GetQuestFactionChange(loaded.ToArray());
        Dictionary<uint, IQuestTemplate> allianceToHorde = new();
        factionOpposite.Each(pair =>
        {
            if (allianceToHorde.TryGetValue(pair.AllianceQuest.Entry, out var hordeQuest))
            {
                nonFatalErrors?.Add($"Quest '{pair.AllianceQuest.Name}' ({pair.AllianceQuest.Entry}) already has a horde counterpart '{hordeQuest.Name}' ({hordeQuest.Entry}). Ignoring this pair, using '{pair.HordeQuest.Name}' ({pair.HordeQuest.Entry}) instead.");
            }
            allianceToHorde[pair.AllianceQuest.Entry] = pair.HordeQuest;
        });

        foreach (var (alliance, horde) in allianceToHorde)
        {
            toLoad.Enqueue(alliance);
            toLoad.Enqueue(horde.Entry);
        }

        await RecursiveLoad();

        factionOpposite.Each(pair =>
        {
            if (questModels.TryGetValue(pair.AllianceQuest.Entry, out var allianceQuest) &&
                questModels.TryGetValue(pair.HordeQuest.Entry, out var hordeQuest))
            {
                // override with the last value
                if (allianceQuest.OtherFactionQuest.HasValue &&
                    allianceQuest.OtherFactionQuest.Value.Id != pair.HordeQuest.Entry)
                {
                    if (questModels.TryGetValue(allianceQuest.OtherFactionQuest.Value.Id, out var previousQuestModel))
                        previousQuestModel.OtherFactionQuest = null;
                }
                if (hordeQuest.OtherFactionQuest.HasValue &&
                    hordeQuest.OtherFactionQuest.Value.Id != pair.AllianceQuest.Entry)
                {
                    if (questModels.TryGetValue(hordeQuest.OtherFactionQuest.Value.Id, out var previousQuestModel))
                        previousQuestModel.OtherFactionQuest = null;
                }
                allianceQuest.OtherFactionQuest = new OtherFactionQuest(pair.HordeQuest.Entry, OtherFactionQuest.Hint.Horde);
                hordeQuest.OtherFactionQuest = new OtherFactionQuest(pair.AllianceQuest.Entry, OtherFactionQuest.Hint.Alliance);
            }
            else
            {
                nonFatalErrors?.Add($"Quest '{pair.AllianceQuest.Name}' ({pair.AllianceQuest.Entry}) or '{pair.HordeQuest.Name}' ({pair.HordeQuest.Entry}) not found in the loaded quest chain. Ignoring this pair.");
            }
        });

        List<(uint, uint)> toRemove = new List<(uint, uint)>();
        foreach (var quest in breadcrumbs.Keys)
        {
            foreach (var breadcrumbQuest in breadcrumbs[quest])
            {
                foreach (var mustBeCompletedQuest in mustBeCompleted[quest])
                {
                    if (mustBeCompletedQuest == breadcrumbQuest)
                    {
                        nonFatalErrors?.Add($"Quest {breadcrumbQuest} is both breadcrumb and must be completed for quest {quest}. Ignoring breadcrumb.");
                        toRemove.Add((quest, breadcrumbQuest));
                    }
                    else if (questToExclusiveGroup.TryGetValue(mustBeCompletedQuest,
                                 out var mustBeCompletedQuestExGroup) &&
                             mustBeCompletedQuestExGroup < 0)
                    {
                        foreach (var otherQuestInGroup in exclusiveGroupToQuests[mustBeCompletedQuestExGroup])
                        {
                            if (otherQuestInGroup == breadcrumbQuest)
                            {
                                nonFatalErrors?.Add($"Quest {breadcrumbQuest} is both breadcrumb and must be completed for quest {quest} via exclusive group {mustBeCompletedQuestExGroup}. Ignoring breadcrumb.");
                                toRemove.Add((quest, breadcrumbQuest));
                            }
                        }
                    }
                }
            }
        }

        foreach (var (quest, breadcrumb) in toRemove)
            breadcrumbs.Remove(new KeyValuePair<uint, uint>(quest, breadcrumb));

        toRemove.Clear();

        foreach (var quest in mustBeCompleted)
        {
            if (questToExclusiveGroup.TryGetValue(quest.Key, out var questExclusiveGroup) && questExclusiveGroup < 0)
            {
                foreach (var prerequisite in quest)
                {
                    if (questToExclusiveGroup.TryGetValue(prerequisite, out var prerequisiteExclusiveGroup))
                    {
                        if (questExclusiveGroup == prerequisiteExclusiveGroup)
                        {
                            nonFatalErrors?.Add($"Quest {prerequisite} is a prerequisite for {quest.Key}, but also they are in the same exclusive group, which doesn't make sense. Ignoring the prerequisite.");
                            toRemove.Add((quest.Key, prerequisite));
                        }
                    }
                }
            }
        }

        foreach (var (quest, prerequisite) in toRemove)
            mustBeCompleted.Remove(new KeyValuePair<uint, uint>(quest, prerequisite));

        toRemove.Clear();

        HashSet<int> processedAdditionalGroups = new();

        foreach (var questId in loaded)
        {
            void Process(QuestRequirementType requirementType, IEnumerable<uint> requirements)
            {
                foreach (var requirement in requirements)
                {
                    if (questToExclusiveGroup.TryGetValue(requirement, out var requirementQuestGroup))
                    {
                        if (exclusiveGroupToQuests[requirementQuestGroup].Count() > 1)
                            processedAdditionalGroups.Add(requirementQuestGroup);

                        if (requirementQuestGroup > 0)
                        {
                            foreach (var orQuestId in orGroups[(uint)requirementQuestGroup])
                            {
                                quests[questId].AddRequirement(requirementType, new QuestGroup(QuestGroupType.OneOf, requirementQuestGroup, orQuestId));
                            }
                        }
                        else if (requirementQuestGroup < 0)
                        {
                            foreach (var andQuestId in andGroups[(uint)-requirementQuestGroup])
                            {
                                quests[questId].AddRequirement(requirementType, new QuestGroup(QuestGroupType.All, requirementQuestGroup, andQuestId));
                            }
                        }
                        else
                            throw new InvalidOperationException("Invalid state: 0 group not allowed in questToExclusiveGroup");
                    }
                    else
                    {
                        quests[questId].AddRequirement(requirementType, new QuestGroup(QuestGroupType.All, 0, requirement));
                    }
                }
            }

            Process(QuestRequirementType.Completed, mustBeCompleted[questId]);
            Process(QuestRequirementType.MustBeActive, mustBeActive[questId]);

            foreach (var breadcrumbQuestId in breadcrumbs[questId])
            {
                quests[questId].AddRequirement(QuestRequirementType.Breadcrumb, new QuestGroup(QuestGroupType.All, 0, breadcrumbQuestId));
            }
        }

        foreach (var questId in loaded)
        {
            if (questToExclusiveGroup.TryGetValue(questId, out var questGroup) &&
                questGroup != 0 &&
                processedAdditionalGroups.Add(questGroup))
            {
                var groupType = questGroup < 0 ? QuestGroupType.All : QuestGroupType.OneOf;
                quests.AddAdditionalGroup(groupType, exclusiveGroupToQuests[questGroup].ToList());
            }
        }

        if (configuration.ShowMarkConditionSourceType is { } condSourceType)
        {
            var conditions = await databaseProvider.GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask.SourceEntry,
                loaded.Select(id => new IDatabaseProvider.ConditionKey(condSourceType, sourceEntry: (int)id)).ToList());
            var conditionsByQuest = conditions.GroupBy(c => c.SourceEntry).ToDictionary(c => c.Key, c => c.ToList());
            foreach (var (questId, condList) in conditionsByQuest)
            {
                quests[(uint)questId].Conditions = condList.Select(c => new AbstractCondition(c)).ToList();
            }
        }
        token.ThrowIfCancellationRequested();
    }
}