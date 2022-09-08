namespace WDE.QueryGenerators.Models;

public struct QuestChainDiff
{
    public uint Id { get; init; }
    public int? PrevQuestId { get; init; }
    public int? NextQuestId { get; init; }
    public int? ExclusiveGroup { get; init; }
    public int? BreadcrumbQuestId { get; init; }
}