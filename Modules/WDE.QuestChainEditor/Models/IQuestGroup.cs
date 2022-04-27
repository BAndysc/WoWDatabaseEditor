using System.Collections.Generic;

namespace WDE.QuestChainEditor.Models;

public interface IQuestGroup
{
    public QuestRequirementType RequirementType { get; }
    public int GroupId { get; }
    public IReadOnlyList<uint> Quests { get; }
}