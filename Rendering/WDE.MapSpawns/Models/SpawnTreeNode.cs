using System.Collections.Generic;
using TheMaths;

namespace WDE.MapSpawns.Models;

internal enum SpawnNodeKind : byte
{
    Map,
    Zone,
    Area,
    SpawnGroup,
    Pool,
    CreatureEntry,
    GameObjectEntry,
    CreatureSpawn,
    GameObjectSpawn,
}

/// <summary>
/// User expand override. Default means "let the filter decide" (phantom expand);
/// Expanded/Collapsed are sticky choices the user made by clicking and survive filter changes.
/// </summary>
internal enum ExpandState : byte
{
    Default,
    Expanded,
    Collapsed,
}

internal sealed class SpawnTreeNode
{
    public SpawnNodeKind Kind;
    public string Label = "";
    public string SearchText = "";
    public List<SpawnTreeNode>? Children;
    public SpawnTreeNode? Parent;

    // leaf payload (only meaningful for CreatureSpawn/GameObjectSpawn, Map/Entry reuse Map/Entry)
    public int Map;
    public uint Entry;
    public uint Guid;
    public Vector3 Position;
    /// <summary>Pool badge: on a leaf, the pool it belongs to while it sits under a non-pool
    /// container (grouped or entry-wide pooled); 0 = unpooled. Pool nodes carry their id in Entry.</summary>
    public uint PoolId;

    // ui / filter transient state
    public ExpandState Expand;
    public bool MatchesFilter;
    public bool HasVisibleDescendant;

    // lazily built row-decoration strings - the visible-row loop runs every frame and must not
    // concatenate per row
    private string? pendingDeleteLabel;
    private string? loadedLabel;
    private string? poolBadge;
    public string PendingDeleteLabel => pendingDeleteLabel ??= Label + "  (pending delete)";
    public string LoadedLabel => loadedLabel ??= Label + "  (loaded)";
    public string PoolBadge => poolBadge ??= $"[pool {PoolId}]";

    public bool IsLeaf => Kind == SpawnNodeKind.CreatureSpawn || Kind == SpawnNodeKind.GameObjectSpawn;
    public bool HasChildren => Children is { Count: > 0 };

    public List<SpawnTreeNode> EnsureChildren() => Children ??= new List<SpawnTreeNode>();
}
