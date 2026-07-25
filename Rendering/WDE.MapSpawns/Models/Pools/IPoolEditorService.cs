using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models.Pools;

[UniqueProvider]
public interface IPoolEditorService
{
    /// <summary>True when the active core has pool SQL providers (gates the tool).</summary>
    bool IsSupported { get; }

    /// <summary>pool_pool exists (nested/mother pools).</summary>
    bool SupportsNestedPools { get; }
    /// <summary>pool_creature_template/pool_gameobject_template exist (entry-wide pooling).</summary>
    bool SupportsEntryPooling { get; }
    /// <summary>True when the core only honors explicit member chances at max_limit == 1
    /// (CMaNGOS PoolMgr) - the inspector shows a warning otherwise.</summary>
    bool ExplicitChanceRequiresMaxLimitOne { get; }

    int LoadedMap { get; }
    bool AnyDirty { get; }

    /// <summary>The exact SQL Save would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    WDE.SqlQueryGenerator.IQuery? BuildSaveQuery();

    /// <summary>True once pools (DB + in-memory edits) have been loaded at least once.</summary>
    bool HasData { get; }

    /// <summary>Bumped on every state change (including detail edits like chances) -
    /// observers use it to invalidate caches.</summary>
    int Revision { get; }

    /// <summary>Bumped only when what the spawns tree shows changes (membership, entry members,
    /// nesting, names, pool create/delete) - detail edits like chances do NOT bump it.</summary>
    int StructureRevision { get; }

    /// <summary>Pool id -> display name (description, falling back to "Pool {id}").</summary>
    IReadOnlyDictionary<uint, string> PoolNames { get; }

    /// <summary>Loads all pool templates, members and nesting (pools are global, all maps;
    /// <paramref name="mapId"/> is only remembered like the spawn-group editor does).</summary>
    Task LoadForMap(int mapId);

    /// <summary>Merges an off-thread load into the live state. Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();

    /// <summary>The pool a spawn directly belongs to (pool_creature/pool_gameobject), if any.</summary>
    uint? PoolOf(PoolMember member);

    /// <summary>The pool an ENTRY belongs to via entry-wide pooling, if any.</summary>
    uint? PoolOfEntry(PoolEntryKey entry);

    /// <summary>The pool's mother pool (pool_pool), if any.</summary>
    uint? MotherOf(uint poolId);

    /// <summary>Fills <paramref name="output"/> (cleared first) with the ids of pools whose mother
    /// is <paramref name="poolId"/>.</summary>
    void CollectChildren(uint poolId, List<uint> output);

    /// <summary>Fills <paramref name="output"/> (cleared first) with the pool's current direct
    /// members. Allocation-free for callers that reuse the list - use this on hot paths.</summary>
    void CollectMembers(uint poolId, List<PoolMember> output);

    /// <summary>The full editable state of the pool, or null when the pool is unknown. Mutate it
    /// directly, then call <see cref="NotifyDetailsChanged"/>.</summary>
    PoolDetails? GetDetails(uint poolId);

    /// <summary>Marks the pool dirty after its details were mutated and bumps <see cref="Revision"/>.</summary>
    void NotifyDetailsChanged(uint poolId);

    /// <summary>Creates a new pool (auto id, given description) from the members. Returns the new id.</summary>
    uint CreatePool(string description, IReadOnlyList<PoolMember> members);

    /// <summary>Adds spawns as direct members; a spawn belongs to exactly one pool, so members are
    /// detached from a previous pool first.</summary>
    void AddToPool(uint poolId, IReadOnlyList<PoolMember> members);

    void RemoveMember(uint poolId, PoolMember member);

    /// <summary>Adds an entry-wide member; an entry belongs to exactly one pool, so it is detached
    /// from a previous pool first.</summary>
    void AddEntryMember(uint poolId, PoolEntryKey entry);

    void RemoveEntryMember(uint poolId, PoolEntryKey entry);

    /// <summary>Sets (or clears, with null) the pool's mother pool. Returns false when the change
    /// would create a pool_pool cycle (including self-parenting) - nothing changes then.</summary>
    bool TrySetMotherPool(uint poolId, uint? motherPoolId);

    /// <summary>Deletes the whole pool - the template and every member/nesting row. Member spawns
    /// stay in the world, just unpooled; child pools become top-level. The database rows go away on
    /// the next <see cref="Save"/>; deleting a never-saved pool simply forgets it.</summary>
    void DeletePool(uint poolId);

    /// <summary>Writes all dirty pools to the DB (idempotent per-pool rewrite; all-table deletes
    /// for deleted pools).</summary>
    Task Save();
}
