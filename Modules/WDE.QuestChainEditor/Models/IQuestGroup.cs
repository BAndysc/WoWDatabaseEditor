using System.Collections.Generic;

namespace WDE.QuestChainEditor.Models;

public interface IQuestGroup
{
    public int GroupId { get; }
    public IReadOnlyList<uint> Quests { get; }
    public QuestGroupType GroupType { get; }
}