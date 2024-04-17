using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.QuestChainEditor.Models;

public class QuestStore : IEnumerable<QuestModel>
{
    private Dictionary<uint, QuestModel> quests;
    private List<(QuestGroupType, IReadOnlyList<uint>)> additionalGroups;

    public QuestStore()
    {
        quests = new();
        additionalGroups = new();
    }

    public bool HasQuest(uint entry)
    {
        return quests.ContainsKey(entry);
    }

    public async Task<QuestModel> GetOrCreate(uint entry, Func<uint, Task<IQuestTemplate?>> loader)
    {
        if (quests.TryGetValue(entry, out var quest))
            return quest;

        quest = quests[entry] = new((await loader(entry)) ?? new AbstractQuestTemplate(){Entry = entry, Name = "[Unknown quest]"});

        return quest;
    }

    public void AddAdditionalGroup(QuestGroupType type, IReadOnlyList<uint> quests)
    {
        additionalGroups.Add((type, quests));
    }

    public QuestModel this[uint entry] => quests[entry];

    public IEnumerator<QuestModel> GetEnumerator() => quests.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)quests.Values).GetEnumerator();

    public IEnumerable<(QuestGroupType, IReadOnlyList<uint>)> GetAdditionalGroups() => additionalGroups;

    public void Merge(QuestStore other)
    {
        foreach (var (key, value) in other.quests)
        {
            if (!quests.ContainsKey(key))
                quests[key] = value.Clone();
        }

        foreach (var (type, questsInGroup) in other.additionalGroups)
        {
            if (!additionalGroups.Any(x => x.Item1 == type && x.Item2.SequenceEqual(questsInGroup)))
                additionalGroups.Add((type, questsInGroup.ToList()));
        }
    }
}