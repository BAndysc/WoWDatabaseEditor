using System.Collections.Generic;

namespace WDE.MapSpawns.Models.Pools;

/// <summary>One guid-based pool member: a concrete creature or gameobject spawn.</summary>
public readonly record struct PoolMember(bool IsCreature, uint Guid);

/// <summary>One entry-wide pool member (pool_creature_template/pool_gameobject_template): every
/// spawn of the entry belongs to the pool.</summary>
public readonly record struct PoolEntryKey(bool IsCreature, uint Entry);

/// <summary>Per-member extra columns (chance/description) of the pool member tables.</summary>
public struct PoolMemberData
{
    /// <summary>Explicit roll chance in percent; 0 = equal-chance member.</summary>
    public float Chance;
    public string? Description;
}

/// <summary>
/// The full editable state of one spawn pool beyond bare guid membership - everything the CMaNGOS
/// pool tables can express. The UI mutates it directly and calls
/// <see cref="IPoolEditorService.NotifyDetailsChanged"/> so the pool is marked dirty and observers
/// refresh. Child-pool lists are DERIVED from each child's <see cref="MotherPool"/> (single source
/// of truth) - use <see cref="IPoolEditorService.CollectChildren"/>.
/// </summary>
public sealed class PoolDetails
{
    public uint Id { get; init; }

    // pool_template row
    public string Description = "";
    /// <summary>Max members spawned at once (0 = no limit).</summary>
    public uint MaxLimit = 1;

    /// <summary>This pool's own pool_pool row (as a child); null = top-level pool.</summary>
    public uint? MotherPool;
    /// <summary>Roll chance of this pool within its mother pool (0 = equal-chance).</summary>
    public float MotherChance;
    public string? MotherDescription;

    /// <summary>pool_creature extra columns, keyed by guid; membership itself lives in the
    /// service's current sets.</summary>
    public readonly Dictionary<uint, PoolMemberData> CreatureMemberData = new();

    /// <summary>pool_gameobject extra columns, keyed by guid.</summary>
    public readonly Dictionary<uint, PoolMemberData> GameObjectMemberData = new();

    /// <summary>pool_creature_template/pool_gameobject_template rows: entry-wide members.</summary>
    public readonly Dictionary<PoolEntryKey, PoolMemberData> EntryMembers = new();

    public PoolMemberData DataOf(PoolMember member)
    {
        var dict = member.IsCreature ? CreatureMemberData : GameObjectMemberData;
        return dict.TryGetValue(member.Guid, out var d) ? d : default;
    }

    public void SetData(PoolMember member, PoolMemberData data)
    {
        var dict = member.IsCreature ? CreatureMemberData : GameObjectMemberData;
        dict[member.Guid] = data;
    }

    public void RemoveData(PoolMember member)
    {
        var dict = member.IsCreature ? CreatureMemberData : GameObjectMemberData;
        dict.Remove(member.Guid);
    }
}
