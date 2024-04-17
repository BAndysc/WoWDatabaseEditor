using System;
using System.Collections.Generic;
using System.Linq;

namespace WDE.QuestChainEditor.Models;

public class QuestGroup : IQuestGroup
{
    public int GroupId { get; }
    public IReadOnlyList<uint> Quests => quests;
    public QuestGroupType GroupType { get; }

    private List<uint> quests = new();

    public QuestGroup(QuestGroupType type, int groupId, params uint[] quests)
    {
        GroupType = type;
        if (quests.Length == 0)
            throw new ArgumentOutOfRangeException();
        GroupId = groupId;
        this.quests.AddRange(quests);
    }

    public override string ToString()
    {
        return string.Join(GroupType == QuestGroupType.All ? "&" : "^", Quests.OrderBy(o => o));
    }

    public void Merge(QuestGroup other)
    {
        quests.AddRange(other.quests);
    }

    public QuestGroup Clone()
    {
        return new(GroupType, GroupId, quests.ToArray());
    }
}