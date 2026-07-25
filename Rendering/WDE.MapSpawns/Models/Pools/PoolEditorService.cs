using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Pools;

public class PoolEditorService : IPoolEditorService
{
    public bool MemberPickArmed { get; set; }

    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<IPoolTemplate> templateGen;
    private readonly IQueryGenerator<IPoolCreatureMember> creatureGen;
    private readonly IQueryGenerator<IPoolGameObjectMember> gameObjectGen;
    private readonly IQueryGenerator<IPoolCreatureEntryMember> creatureEntryGen;
    private readonly IQueryGenerator<IPoolGameObjectEntryMember> gameObjectEntryGen;
    private readonly IQueryGenerator<IPoolNesting> nestingGen;
    private readonly IPoolSchemaInfoProvider? schemaInfo;

    private sealed class Loaded
    {
        public required int MapId;
        public required Dictionary<uint, string> Names;
        public required Dictionary<uint, HashSet<PoolMember>> Members;
        public required Dictionary<uint, PoolDetails> Details;
    }

    // swapped in on the engine thread by PumpPendingLoads
    private volatile Loaded? pending;

    private readonly Dictionary<uint, string> names = new();
    private readonly Dictionary<uint, HashSet<PoolMember>> original = new();
    private readonly Dictionary<uint, HashSet<PoolMember>> current = new();
    private readonly Dictionary<PoolMember, uint> memberToPool = new();
    private readonly Dictionary<PoolEntryKey, uint> entryToPool = new();
    private readonly Dictionary<uint, PoolDetails> details = new();
    private readonly HashSet<uint> dirty = new();
    private readonly HashSet<uint> newPools = new();
    private readonly HashSet<uint> deletedPools = new();

    public int LoadedMap { get; private set; } = -1;
    public bool AnyDirty => dirty.Count > 0;
    public bool HasData { get; private set; }
    public int Revision { get; private set; }
    public int StructureRevision { get; private set; }
    public IReadOnlyDictionary<uint, string> PoolNames => names;
    public bool IsSupported => templateGen.TableName != null && creatureGen.TableName != null &&
                               gameObjectGen.TableName != null;
    public bool SupportsNestedPools => nestingGen.TableName != null;
    public bool SupportsEntryPooling => creatureEntryGen.TableName != null && gameObjectEntryGen.TableName != null;
    public bool ExplicitChanceRequiresMaxLimitOne => schemaInfo?.ExplicitChanceRequiresMaxLimitOne ?? false;

    public PoolEditorService(IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<IPoolTemplate> templateGen,
        IQueryGenerator<IPoolCreatureMember> creatureGen,
        IQueryGenerator<IPoolGameObjectMember> gameObjectGen,
        IQueryGenerator<IPoolCreatureEntryMember> creatureEntryGen,
        IQueryGenerator<IPoolGameObjectEntryMember> gameObjectEntryGen,
        IQueryGenerator<IPoolNesting> nestingGen,
        IEnumerable<IPoolSchemaInfoProvider> schemaInfoProviders)
    {
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.templateGen = templateGen;
        this.creatureGen = creatureGen;
        this.gameObjectGen = gameObjectGen;
        this.creatureEntryGen = creatureEntryGen;
        this.gameObjectEntryGen = gameObjectEntryGen;
        this.nestingGen = nestingGen;
        schemaInfo = schemaInfoProviders.FirstOrDefault(); // at most one per core ([RequiresCore])
    }

    private static string NameOf(uint id, string description) =>
        string.IsNullOrWhiteSpace(description) ? $"Pool {id}" : description;

    public async Task LoadForMap(int mapId)
    {
        // claim the map BEFORE the first await: the module's per-frame SyncMap compares against
        // LoadedMap, and the getters below can stall for seconds behind the app cache warm-up
        // (CachedDatabaseProvider.WaitForCache) - without this, a new full load would be queued
        // EVERY FRAME until the first one lands (hundreds of 6-query loads + a tree rebuild per
        // pumped result: massive load time + memory churn)
        LoadedMap = mapId;

        // every getter may return null (e.g. a CMaNGOS profile over HttpDatabase, where the
        // generators claim support but the provider has no pass-through) - treat null as empty
        var templates = (await databaseProvider.GetPoolTemplatesAsync()) ?? Array.Empty<IPoolTemplate>();
        var creatures = (await databaseProvider.GetPoolCreaturesAsync()) ?? Array.Empty<IPoolCreatureMember>();
        var gameObjects = (await databaseProvider.GetPoolGameObjectsAsync()) ?? Array.Empty<IPoolGameObjectMember>();
        var creatureEntries = SupportsEntryPooling
            ? (await databaseProvider.GetPoolCreatureEntryPoolsAsync()) ?? Array.Empty<IPoolCreatureEntryMember>()
            : Array.Empty<IPoolCreatureEntryMember>();
        var gameObjectEntries = SupportsEntryPooling
            ? (await databaseProvider.GetPoolGameObjectEntryPoolsAsync()) ?? Array.Empty<IPoolGameObjectEntryMember>()
            : Array.Empty<IPoolGameObjectEntryMember>();
        var nestings = SupportsNestedPools
            ? (await databaseProvider.GetPoolNestingsAsync()) ?? Array.Empty<IPoolNesting>()
            : Array.Empty<IPoolNesting>();

        var namesMap = new Dictionary<uint, string>();
        var detailsMap = new Dictionary<uint, PoolDetails>();

        PoolDetails Details(uint id)
        {
            if (!detailsMap.TryGetValue(id, out var d))
            {
                // member row referencing a missing pool_template - still editable, saved on rewrite
                detailsMap[id] = d = new PoolDetails { Id = id };
                namesMap[id] = NameOf(id, "");
            }
            return d;
        }

        foreach (var t in templates.GroupBy(x => x.Entry).Select(g => g.First()))
        {
            detailsMap[t.Entry] = new PoolDetails { Id = t.Entry, Description = t.Description, MaxLimit = t.MaxLimit };
            namesMap[t.Entry] = NameOf(t.Entry, t.Description);
        }

        var members = new Dictionary<uint, HashSet<PoolMember>>();
        void AddMember(uint pool, PoolMember m, float chance, string? description)
        {
            if (!members.TryGetValue(pool, out var set))
                members[pool] = set = new HashSet<PoolMember>();
            set.Add(m);
            Details(pool).SetData(m, new PoolMemberData { Chance = chance, Description = description });
        }

        foreach (var c in creatures)
            AddMember(c.PoolEntry, new PoolMember(true, c.Guid), c.Chance, c.Description);
        foreach (var g in gameObjects)
            AddMember(g.PoolEntry, new PoolMember(false, g.Guid), g.Chance, g.Description);

        foreach (var e in creatureEntries)
            Details(e.PoolEntry).EntryMembers[new PoolEntryKey(true, e.Entry)] =
                new PoolMemberData { Chance = e.Chance, Description = e.Description };
        foreach (var e in gameObjectEntries)
            Details(e.PoolEntry).EntryMembers[new PoolEntryKey(false, e.Entry)] =
                new PoolMemberData { Chance = e.Chance, Description = e.Description };

        foreach (var n in nestings)
        {
            var child = Details(n.PoolId);
            Details(n.MotherPool); // make sure the mother exists too
            child.MotherPool = n.MotherPool;
            child.MotherChance = n.Chance;
            child.MotherDescription = n.Description;
        }

        pending = new Loaded
        {
            MapId = mapId,
            Names = namesMap,
            Members = members,
            Details = detailsMap,
        };
    }

    public void PumpPendingLoads()
    {
        var p = pending;
        if (p == null)
            return;
        pending = null;

        names.Clear();
        original.Clear();
        current.Clear();
        memberToPool.Clear();
        entryToPool.Clear();
        details.Clear();
        dirty.Clear();
        newPools.Clear();
        deletedPools.Clear();

        foreach (var (id, name) in p.Names)
            names[id] = name;
        foreach (var (id, set) in p.Members)
        {
            original[id] = new HashSet<PoolMember>(set);
            current[id] = new HashSet<PoolMember>(set);
            foreach (var m in set)
                memberToPool[m] = id;
        }
        foreach (var (id, d) in p.Details)
        {
            details[id] = d;
            foreach (var entry in d.EntryMembers.Keys)
                entryToPool[entry] = id;
        }
        LoadedMap = p.MapId;
        HasData = true;
        Revision++;
        StructureRevision++;
    }

    public uint? PoolOf(PoolMember member) =>
        memberToPool.TryGetValue(member, out var id) ? id : null;

    public uint? PoolOfEntry(PoolEntryKey entry) =>
        entryToPool.TryGetValue(entry, out var id) ? id : null;

    public uint? MotherOf(uint poolId) =>
        details.TryGetValue(poolId, out var d) ? d.MotherPool : null;

    public void CollectChildren(uint poolId, List<uint> output)
    {
        output.Clear();
        foreach (var (id, d) in details)
        {
            if (d.MotherPool == poolId)
                output.Add(id);
        }
    }

    public void CollectMembers(uint poolId, List<PoolMember> output)
    {
        output.Clear();
        if (current.TryGetValue(poolId, out var set))
            output.AddRange(set); // AddRange over a HashSet (ICollection) uses CopyTo - no per-item alloc
    }

    public PoolDetails? GetDetails(uint poolId) =>
        details.TryGetValue(poolId, out var d) ? d : null;

    public void NotifyDetailsChanged(uint poolId)
    {
        if (!details.TryGetValue(poolId, out var d))
            return;
        // structure bump only when the tree-visible name changed - chance edits must not
        // trigger a spawns-tree regroup
        var newName = NameOf(poolId, d.Description);
        if (!names.TryGetValue(poolId, out var oldName) || oldName != newName)
            StructureRevision++;
        names[poolId] = newName; // keep the tree/picker name in sync
        dirty.Add(poolId);
        Revision++;
    }

    public uint CreatePool(string description, IReadOnlyList<PoolMember> members)
    {
        uint id = NextFreeId();
        names[id] = NameOf(id, description);
        current[id] = new HashSet<PoolMember>();
        details[id] = new PoolDetails { Id = id, Description = description };
        newPools.Add(id);
        AddToPool(id, members);
        dirty.Add(id); // even with no members, the template row itself is new
        return id;
    }

    public void AddToPool(uint poolId, IReadOnlyList<PoolMember> members)
    {
        if (!current.TryGetValue(poolId, out var set))
            current[poolId] = set = new HashSet<PoolMember>();

        var d = GetDetails(poolId);

        foreach (var m in members)
        {
            // a spawn belongs to exactly one pool - detach from a previous one
            if (memberToPool.TryGetValue(m, out var old) && old != poolId)
            {
                if (current.TryGetValue(old, out var oldSet) && oldSet.Remove(m))
                {
                    GetDetails(old)?.RemoveData(m);
                    dirty.Add(old);
                }
            }
            if (set.Add(m))
            {
                if (d != null && !((m.IsCreature ? d.CreatureMemberData : d.GameObjectMemberData).ContainsKey(m.Guid)))
                    d.SetData(m, default);
                dirty.Add(poolId);
            }
            memberToPool[m] = poolId;
        }
        Revision++;
        StructureRevision++;
    }

    public void RemoveMember(uint poolId, PoolMember member)
    {
        if (current.TryGetValue(poolId, out var set) && set.Remove(member))
        {
            GetDetails(poolId)?.RemoveData(member);
            dirty.Add(poolId);
            if (memberToPool.TryGetValue(member, out var p) && p == poolId)
                memberToPool.Remove(member);
            Revision++;
            StructureRevision++;
        }
    }

    public void AddEntryMember(uint poolId, PoolEntryKey entry)
    {
        if (GetDetails(poolId) is not { } d)
            return;

        // an entry belongs to exactly one pool - detach from a previous one
        if (entryToPool.TryGetValue(entry, out var old) && old != poolId)
        {
            if (GetDetails(old) is { } oldDetails && oldDetails.EntryMembers.Remove(entry))
                dirty.Add(old);
        }
        if (d.EntryMembers.TryAdd(entry, default))
            dirty.Add(poolId);
        entryToPool[entry] = poolId;
        Revision++;
        StructureRevision++;
    }

    public void RemoveEntryMember(uint poolId, PoolEntryKey entry)
    {
        if (GetDetails(poolId) is { } d && d.EntryMembers.Remove(entry))
        {
            dirty.Add(poolId);
            if (entryToPool.TryGetValue(entry, out var p) && p == poolId)
                entryToPool.Remove(entry);
            Revision++;
            StructureRevision++;
        }
    }

    public bool TrySetMotherPool(uint poolId, uint? motherPoolId)
    {
        if (GetDetails(poolId) is not { } d)
            return false;
        if (motherPoolId is { } mother)
        {
            if (mother == poolId || GetDetails(mother) == null)
                return false;
            // walking up from the proposed mother must never reach the pool itself - since child
            // lists are derived from MotherPool, this single check prevents all cycles
            for (uint? walk = mother; walk != null; walk = MotherOf(walk.Value))
            {
                if (walk == poolId)
                    return false;
            }
        }
        if (d.MotherPool == motherPoolId)
            return true;
        d.MotherPool = motherPoolId;
        if (motherPoolId == null)
        {
            d.MotherChance = 0;
            d.MotherDescription = null;
        }
        dirty.Add(poolId);
        Revision++;
        StructureRevision++; // nesting is part of the tree
        return true;
    }

    public void DeletePool(uint poolId)
    {
        if (!names.Remove(poolId))
            return;

        if (current.TryGetValue(poolId, out var set))
        {
            foreach (var m in set)
            {
                if (memberToPool.TryGetValue(m, out var p) && p == poolId)
                    memberToPool.Remove(m);
            }
            current.Remove(poolId);
        }
        if (details.TryGetValue(poolId, out var d))
        {
            foreach (var entry in d.EntryMembers.Keys)
            {
                if (entryToPool.TryGetValue(entry, out var p) && p == poolId)
                    entryToPool.Remove(entry);
            }
            details.Remove(poolId);
        }

        // children become top-level - their pool_pool rows must go away on their own rewrite
        foreach (var (otherId, other) in details)
        {
            if (other.MotherPool == poolId)
            {
                other.MotherPool = null;
                other.MotherChance = 0;
                other.MotherDescription = null;
                dirty.Add(otherId);
            }
        }

        if (newPools.Remove(poolId))
            dirty.Remove(poolId); // never saved - nothing to delete in the database
        else
        {
            deletedPools.Add(poolId);
            dirty.Add(poolId);
        }

        Revision++;
        StructureRevision++;
    }

    private uint NextFreeId()
    {
        uint max = 0;
        foreach (var id in names.Keys)
            max = Math.Max(max, id);
        // ids deleted this session stay reserved until Save applies the deletes - handing one out
        // again would let a later unsaved delete of the new pool forget the pending DB delete
        foreach (var id in deletedPools)
            max = Math.Max(max, id);
        return max + 1;
    }

    public IQuery? BuildSaveQuery()
    {
        if (dirty.Count == 0)
            return null;

        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        foreach (var pid in dirty)
        {
            if (deletedPools.Contains(pid))
            {
                // the whole pool is gone: delete every table's rows, insert nothing
                // (Try* are no-ops on cores without the table; nesting DeleteAll covers both
                // directions - the pool's own mother link and its children's links)
                Add(templateGen.TryDelete(new AbstractPoolTemplate { Entry = pid }));
                Add(creatureGen.TryDeleteAll(new AbstractPoolCreatureMember { PoolEntry = pid }));
                Add(gameObjectGen.TryDeleteAll(new AbstractPoolGameObjectMember { PoolEntry = pid }));
                Add(creatureEntryGen.TryDeleteAll(new AbstractPoolCreatureEntryMember { PoolEntry = pid }));
                Add(gameObjectEntryGen.TryDeleteAll(new AbstractPoolGameObjectEntryMember { PoolEntry = pid }));
                Add(nestingGen.TryDeleteAll(new AbstractPoolNesting { PoolId = pid }));
                continue;
            }

            var cur = current.TryGetValue(pid, out var c) ? c : new HashSet<PoolMember>();
            var d = GetDetails(pid) ?? new PoolDetails { Id = pid };

            // idempotent per-pool rewrite: DELETE everything first, then INSERT the current state
            var tpl = new AbstractPoolTemplate { Entry = pid, MaxLimit = d.MaxLimit, Description = d.Description };
            Add(templateGen.TryDelete(tpl));
            Add(templateGen.TryInsert(tpl));

            Add(creatureGen.TryDeleteAll(new AbstractPoolCreatureMember { PoolEntry = pid }));
            var creatureRows = cur.Where(m => m.IsCreature).Select(m =>
            {
                var data = d.DataOf(m);
                return (IPoolCreatureMember)new AbstractPoolCreatureMember
                    { Guid = m.Guid, PoolEntry = pid, Chance = data.Chance, Description = data.Description };
            }).ToList();
            if (creatureRows.Count > 0)
                Add(creatureGen.TryBulkInsert(creatureRows));

            Add(gameObjectGen.TryDeleteAll(new AbstractPoolGameObjectMember { PoolEntry = pid }));
            var gameObjectRows = cur.Where(m => !m.IsCreature).Select(m =>
            {
                var data = d.DataOf(m);
                return (IPoolGameObjectMember)new AbstractPoolGameObjectMember
                    { Guid = m.Guid, PoolEntry = pid, Chance = data.Chance, Description = data.Description };
            }).ToList();
            if (gameObjectRows.Count > 0)
                Add(gameObjectGen.TryBulkInsert(gameObjectRows));

            Add(creatureEntryGen.TryDeleteAll(new AbstractPoolCreatureEntryMember { PoolEntry = pid }));
            var creatureEntryRows = d.EntryMembers.Where(kv => kv.Key.IsCreature).Select(kv =>
                (IPoolCreatureEntryMember)new AbstractPoolCreatureEntryMember
                    { Entry = kv.Key.Entry, PoolEntry = pid, Chance = kv.Value.Chance, Description = kv.Value.Description }).ToList();
            if (creatureEntryRows.Count > 0)
                Add(creatureEntryGen.TryBulkInsert(creatureEntryRows));

            Add(gameObjectEntryGen.TryDeleteAll(new AbstractPoolGameObjectEntryMember { PoolEntry = pid }));
            var gameObjectEntryRows = d.EntryMembers.Where(kv => !kv.Key.IsCreature).Select(kv =>
                (IPoolGameObjectEntryMember)new AbstractPoolGameObjectEntryMember
                    { Entry = kv.Key.Entry, PoolEntry = pid, Chance = kv.Value.Chance, Description = kv.Value.Description }).ToList();
            if (gameObjectEntryRows.Count > 0)
                Add(gameObjectEntryGen.TryBulkInsert(gameObjectEntryRows));

            // nesting: DeleteAll covers both directions (own mother link + children's links), so the
            // reinserts must too - own row from this pool's details, children's rows from the
            // CHILDREN's current details (an unsaved child edit is never clobbered)
            Add(nestingGen.TryDeleteAll(new AbstractPoolNesting { PoolId = pid }));
            var nestingRows = new List<IPoolNesting>();
            if (d.MotherPool is { } mother)
                nestingRows.Add(new AbstractPoolNesting
                    { PoolId = pid, MotherPool = mother, Chance = d.MotherChance, Description = d.MotherDescription });
            foreach (var (childId, child) in details)
            {
                if (child.MotherPool == pid)
                    nestingRows.Add(new AbstractPoolNesting
                        { PoolId = childId, MotherPool = pid, Chance = child.MotherChance, Description = child.MotherDescription });
            }
            if (nestingRows.Count > 0)
                Add(nestingGen.TryBulkInsert(nestingRows));
        }

        return multi?.Close();
    }

    public async Task Save()
    {
        var query = BuildSaveQuery();
        if (query == null)
            return;

        await mainThread.Schedule(async () =>
        {
            await mySqlExecutor.ExecuteSql(query);
            return true;
        });

        // hand the just-saved pools to the session (full app only; the bridge upserts each per-pool
        // KEY-ONLY item — the session query re-reads the pool's template + members + nesting from
        // the DB, which the live save above just wrote)
        var items = dirty.Select(pid => new PoolsSolutionItem { PoolId = pid }).ToList();
        eventAggregator.GetEvent<PoolsSavedEvent>().Publish(items);

        // commit in-memory: originals now match currents; deleted pools are gone for good
        foreach (var pid in dirty)
        {
            if (deletedPools.Contains(pid))
                original.Remove(pid);
            else
                original[pid] = current.TryGetValue(pid, out var c) ? new HashSet<PoolMember>(c) : new HashSet<PoolMember>();
        }
        dirty.Clear();
        newPools.Clear();
        deletedPools.Clear();
        Revision++;
    }
}
