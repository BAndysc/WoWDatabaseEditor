namespace WDE.QueryGenerators.Models;

public struct CreatureGossipUpdate
{
    public uint Entry { get; init; }
    public uint GossipMenuId { get; init; }
    // ReSharper disable once InconsistentNaming
    public string? __comment { get; init; }
}