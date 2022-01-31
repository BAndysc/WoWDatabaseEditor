using System;
using System.Collections.Generic;
using System.Linq;

namespace WDE.QuestChainEditor.Models;

public class QuestGroup : IQuestGroup
{
    public QuestRequirementType RequirementType { get; }
    public int GroupId { get; }
    public IReadOnlyList<uint> Quests => quests;
    
    private List<uint> quests = new();

    public QuestGroup(QuestRequirementType type, int groupId, params uint[] quests)
    {
        if (quests.Length == 0)
            throw new ArgumentOutOfRangeException();
        if (type == QuestRequirementType.MustBeActive && quests.Length != 1)
            throw new ArgumentOutOfRangeException();
        GroupId = groupId;
        RequirementType = type;
        this.quests.AddRange(quests);
    }

    public override string ToString()
    {
        if (RequirementType == QuestRequirementType.AllCompleted)
            return string.Join("&", Quests.OrderBy(o => o));
        if (RequirementType == QuestRequirementType.OnlyOneCompleted)
            return string.Join("|", Quests.OrderBy(o => o));
        if (RequirementType == QuestRequirementType.MustBeActive)
            return "+" + quests[0];
        throw new ArgumentOutOfRangeException();
    }

    public void Merge(QuestGroup other)
    {
        if (RequirementType != other.RequirementType ||
            other.RequirementType == QuestRequirementType.MustBeActive)
            throw new ArgumentOutOfRangeException();
        
        quests.AddRange(other.quests);
    }
}