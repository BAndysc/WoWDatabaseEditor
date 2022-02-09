using System;
using System.Collections;
using System.Collections.Generic;

namespace WDE.QuestChainEditor.Models;

public class QuestStore : IEnumerable<Quest>
{
    public IQuestTemplateSource Source { get; }
    private Dictionary<uint, Quest> entries;

    public QuestStore(IQuestTemplateSource source, Dictionary<uint, Quest>? existing = null)
    {
        Source = source;
        entries = existing ?? new();
    }

    internal Quest AddEmptyQuery(uint entry)
    {
        var quest = new Quest(Source.GetTemplate(entry)!);
        entries.Add(entry, quest);
        return quest;
    }
    
    // loads the quest with all previous and next quests
    public void LoadQuest(uint entry)
    {
        HashSet<uint> loaded = new();
        Queue<uint> toLoad = new();
        toLoad.Enqueue(entry);

        while (toLoad.Count > 0)
        {
            entry = toLoad.Dequeue();
            if (!loaded.Add(entry))
                continue;
            
            var template = Source.GetTemplate(entry);

            if (template == null)
                continue;
            
            var quest = GetOrCreate(entry);

            if (template.PrevQuestId > 0)
            {
                quest.AddRequirement(new QuestGroup(QuestRequirementType.AllCompleted, 0, (uint)template.PrevQuestId));
                toLoad.Enqueue((uint)template.PrevQuestId);
            }
            else if (template.PrevQuestId < 0)
            {
                quest.AddRequirement(new QuestGroup(QuestRequirementType.MustBeActive, 0, (uint)-template.PrevQuestId));
                toLoad.Enqueue((uint)-template.PrevQuestId);
            }

            if (template.BreadcrumbForQuestId > 0)
            {
                var next = (uint)template.BreadcrumbForQuestId;
                var nextQuest = GetOrCreate(next);
                nextQuest.AddRequirement(new QuestGroup(QuestRequirementType.Breadcrumb, template.ExclusiveGroup, entry));
                toLoad.Enqueue(next);
            }

            if (template.NextQuestId > 0)
            {
                var next = (uint)template.NextQuestId;
                var nextQuest = GetOrCreate(next);
                if (template.ExclusiveGroup <= 0)
                    nextQuest.AddRequirement(new QuestGroup(QuestRequirementType.AllCompleted, template.ExclusiveGroup, entry));
                else
                {
                    nextQuest.AddRequirement(new QuestGroup(QuestRequirementType.OnlyOneCompleted, template.ExclusiveGroup, entry));
                }
                toLoad.Enqueue(next);
            }
            else if (template.NextQuestId < 0)
            {
                throw new Exception("what does it mean?");
            }

            var byPrevious = Source.GetByPreviousQuestId(entry);
            foreach (var next in byPrevious)
            {
                toLoad.Enqueue(next.Entry);
            }

            var byNext = Source.GetByNextQuestId(entry);
            foreach (var next in byNext)
            {
                toLoad.Enqueue(next.Entry);
            }

            var byBreadcrumb = Source.GetByBreadCrumbQuestId(entry);
            foreach (var next in byBreadcrumb)
            {
                toLoad.Enqueue(next.Entry);
            }
        }
    }

    public bool HasQuest(uint entry)
    {
        return entries.ContainsKey(entry);
    }

    private Quest GetOrCreate(uint entry)
    {
        if (entries.TryGetValue(entry, out var quest))
            return quest;
        quest = entries[entry] = new(Source.GetTemplate(entry)!);
        return quest;
    }

    public Quest this[uint entry] => entries[entry];
    public IEnumerator<Quest> GetEnumerator() => entries.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries.Values).GetEnumerator();
}