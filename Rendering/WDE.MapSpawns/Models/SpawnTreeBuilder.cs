using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMaths;
using WDE.Common.Database;
using WDE.MapRenderer.Managers;
using WDE.MpqReader;

namespace WDE.MapSpawns.Models;

/// <summary>
/// One spawn, with its zone/area already resolved from position but WITHOUT group membership.
/// This (the expensive DB part) is loaded once and cached; the tree is (re)built from these rows
/// plus a group overlay, so unsaved group edits can be reflected without re-querying the DB.
/// </summary>
internal readonly struct SpawnRow
{
    public readonly bool IsCreature;
    public readonly uint Entry;
    public readonly uint Guid;
    public readonly int Map;
    public readonly Vector3 Position;
    public readonly int? ZoneId; // parent zone (null = unknown)
    public readonly int? AreaId; // distinct sub-area (null when the area IS the zone, or unknown)
    public readonly string EntryName;

    public SpawnRow(bool isCreature, uint entry, uint guid, int map, Vector3 position,
        int? zoneId, int? areaId, string entryName)
    {
        IsCreature = isCreature;
        Entry = entry;
        Guid = guid;
        Map = map;
        Position = position;
        ZoneId = zoneId;
        AreaId = areaId;
        EntryName = entryName;
    }
}

/// <summary>Pool membership snapshot for the tree build (from the pool editor when loaded, else the
/// DB): direct guid members, entry-wide pooled entries, the mother links and display names.</summary>
internal sealed class PoolOverlay
{
    public required Dictionary<(bool isCreature, uint guid), uint> MemberToPool { get; init; }
    public required Dictionary<(bool isCreature, uint entry), uint> EntryToPool { get; init; }
    public required Dictionary<uint, uint> MotherOf { get; init; }
    public required Dictionary<uint, string> PoolNames { get; init; }

    public static PoolOverlay Empty => new()
    {
        MemberToPool = new(),
        EntryToPool = new(),
        MotherOf = new(),
        PoolNames = new(),
    };
}

/// <summary>
/// Builds the all-maps spawns hierarchy: Map -> Zone -> [Area] -> [SpawnGroup|Pool] -> Entry -> Spawn.
/// Split so the expensive spawn/template/position load (<see cref="LoadRowsAsync"/>) can be cached
/// and only the cheap node build (<see cref="BuildTree"/>) reruns when group membership changes.
/// </summary>
internal static class SpawnTreeBuilder
{
    private enum GroupAnchor : byte { Area, Zone, Map }

    public static async Task<List<SpawnRow>> LoadRowsAsync(
        IDatabaseProvider db, DbcManager dbc, ZoneAreaManager zoneArea,
        Action<string, int, int>? progress = null,
        Func<Task>? yieldEveryChunk = null)
    {
        progress?.Invoke("querying spawns", 0, 0);
        var creatures = await db.GetCreaturesAsync();
        var gameobjects = await db.GetGameObjectsAsync();

        progress?.Invoke("querying templates", 0, 0);
        var creatureTemplates = (await db.GetCreatureTemplatesAsync())
            .GroupBy(x => x.Entry).ToDictionary(x => x.Key, x => x.First());
        var goTemplates = (await db.GetGameObjectTemplatesAsync())
            .GroupBy(x => x.Entry).ToDictionary(x => x.Key, x => x.First());

        var rows = new List<SpawnRow>(creatures.Count + gameobjects.Count);
        int total = creatures.Count + gameobjects.Count;
        int done = 0;

        foreach (var c in creatures)
        {
            if ((++done & 4095) == 1)
            {
                progress?.Invoke("resolving areas", done, total);
                // when the caller runs on the game loop, this yields a frame per chunk so the
                // build doesn't freeze rendering (awaits resume on the game loop, not a pool thread)
                if (yieldEveryChunk != null)
                    await yieldEveryChunk();
            }
            var (zone, area) = ResolveZoneArea(dbc, zoneArea, c.Map, new Vector3(c.X, c.Y, c.Z));
            creatureTemplates.TryGetValue(c.Entry, out var tpl);
            rows.Add(new SpawnRow(true, c.Entry, c.Guid, c.Map, new Vector3(c.X, c.Y, c.Z),
                zone, area, tpl?.Name ?? "Unknown"));
        }

        foreach (var o in gameobjects)
        {
            if ((++done & 4095) == 1)
            {
                progress?.Invoke("resolving areas", done, total);
                if (yieldEveryChunk != null)
                    await yieldEveryChunk();
            }
            var (zone, area) = ResolveZoneArea(dbc, zoneArea, o.Map, new Vector3(o.X, o.Y, o.Z));
            goTemplates.TryGetValue(o.Entry, out var tpl);
            rows.Add(new SpawnRow(false, o.Entry, o.Guid, o.Map, new Vector3(o.X, o.Y, o.Z),
                zone, area, tpl?.Name ?? "Unknown"));
        }

        return rows;
    }

    /// <summary>Loads the spawn-group membership straight from the DB (guid->group + id->name),
    /// for use when the in-memory editor overlay isn't available yet.</summary>
    public static async Task<(Dictionary<(bool, uint), uint> memberToGroup, Dictionary<uint, string> names)>
        LoadDbGroupOverlayAsync(IDatabaseProvider db)
    {
        var templates = (await db.GetSpawnGroupTemplatesAsync())
            .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        var creatureGroupIds = templates.Values
            .Where(t => t.Type == SpawnGroupTemplateType.Creature)
            .Select(t => t.Id).ToHashSet();

        var memberToGroup = new Dictionary<(bool, uint), uint>();
        foreach (var gs in await db.GetSpawnGroupSpawnsAsync())
        {
            var type = gs.Type == SpawnGroupTemplateType.Any
                ? (creatureGroupIds.Contains(gs.TemplateId) ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject)
                : gs.Type;
            memberToGroup[(type == SpawnGroupTemplateType.Creature, gs.Guid)] = gs.TemplateId;
        }

        var names = templates.ToDictionary(x => x.Key, x => x.Value.Name);
        return (memberToGroup, names);
    }

    /// <summary>Loads the pool membership straight from the DB, for use when the in-memory pool
    /// editor overlay isn't available yet. Every getter may return null (core without pools /
    /// HttpDatabase) - null means empty, so the tree simply has no pool nodes.</summary>
    public static async Task<PoolOverlay> LoadDbPoolOverlayAsync(IDatabaseProvider db)
    {
        var overlay = PoolOverlay.Empty;

        foreach (var t in await db.GetPoolTemplatesAsync() ?? (IReadOnlyList<IPoolTemplate>)Array.Empty<IPoolTemplate>())
            overlay.PoolNames[t.Entry] = string.IsNullOrWhiteSpace(t.Description) ? $"Pool {t.Entry}" : t.Description;

        // member rows referencing a missing pool_template still get a (fallback-named) pool node
        void EnsureName(uint pool)
        {
            if (!overlay.PoolNames.ContainsKey(pool))
                overlay.PoolNames[pool] = $"Pool {pool}";
        }

        foreach (var c in await db.GetPoolCreaturesAsync() ?? (IReadOnlyList<IPoolCreatureMember>)Array.Empty<IPoolCreatureMember>())
        {
            overlay.MemberToPool[(true, c.Guid)] = c.PoolEntry;
            EnsureName(c.PoolEntry);
        }
        foreach (var g in await db.GetPoolGameObjectsAsync() ?? (IReadOnlyList<IPoolGameObjectMember>)Array.Empty<IPoolGameObjectMember>())
        {
            overlay.MemberToPool[(false, g.Guid)] = g.PoolEntry;
            EnsureName(g.PoolEntry);
        }
        foreach (var e in await db.GetPoolCreatureEntryPoolsAsync() ?? (IReadOnlyList<IPoolCreatureEntryMember>)Array.Empty<IPoolCreatureEntryMember>())
        {
            overlay.EntryToPool[(true, e.Entry)] = e.PoolEntry;
            EnsureName(e.PoolEntry);
        }
        foreach (var e in await db.GetPoolGameObjectEntryPoolsAsync() ?? (IReadOnlyList<IPoolGameObjectEntryMember>)Array.Empty<IPoolGameObjectEntryMember>())
        {
            overlay.EntryToPool[(false, e.Entry)] = e.PoolEntry;
            EnsureName(e.PoolEntry);
        }
        foreach (var n in await db.GetPoolNestingsAsync() ?? (IReadOnlyList<IPoolNesting>)Array.Empty<IPoolNesting>())
        {
            overlay.MotherOf[n.PoolId] = n.MotherPool;
            EnsureName(n.PoolId);
            EnsureName(n.MotherPool);
        }

        return overlay;
    }

    /// <summary>Walking pool mothers is bounded - the editor's overlay is cycle-free by
    /// construction, but a hand-edited DB can contain pool_pool cycles.</summary>
    private const int MaxPoolDepth = 64;

    /// <summary>
    /// Builds the node tree from cached rows plus group and pool overlays. A spawn group / pool is
    /// anchored by how far its members spread: single area -> under that Area, single zone/many
    /// areas -> under the Zone, many zones/maps -> under the Map. A spawn in both a group and a
    /// pool stays under its group node and only gets a pool badge; nested pools nest their nodes
    /// (mother chain root-first, the mother anchored around ALL its descendants' members). Pools
    /// with no direct member anywhere (e.g. entry-wide only) aren't in the tree - they stay
    /// editable through the pool inspector's picker. Pure/synchronous (no DB, thread-safe over
    /// its inputs).
    /// </summary>
    public static List<SpawnTreeNode> BuildTree(
        IReadOnlyList<SpawnRow> rows, DbcManager dbc,
        IReadOnlyDictionary<(bool isCreature, uint guid), uint> memberToGroup,
        IReadOnlyDictionary<uint, string> groupNames,
        PoolOverlay pools)
    {
        uint? GroupOf(in SpawnRow r) =>
            memberToGroup.TryGetValue((r.IsCreature, r.Guid), out var gid) && groupNames.ContainsKey(gid)
                ? gid : (uint?)null;

        uint? PoolOf(in SpawnRow r) =>
            pools.MemberToPool.TryGetValue((r.IsCreature, r.Guid), out var pid) && pools.PoolNames.ContainsKey(pid)
                ? pid : (uint?)null;

        uint? MotherOf(uint pid) =>
            pools.MotherOf.TryGetValue(pid, out var mother) && pools.PoolNames.ContainsKey(mother)
                ? mother : (uint?)null;

        // --- pass 1: how far does each spawn group spread? ---
        var spread = new Dictionary<uint, (HashSet<int> maps, HashSet<int> zones, HashSet<int> areas)>();
        foreach (var r in rows)
        {
            if (GroupOf(r) is not uint gid)
                continue;
            if (!spread.TryGetValue(gid, out var s))
                spread[gid] = s = (new HashSet<int>(), new HashSet<int>(), new HashSet<int>());
            s.maps.Add(r.Map);
            if (r.ZoneId is int z) s.zones.Add(z);
            if (r.AreaId is int a) s.areas.Add(a);
        }

        var anchor = new Dictionary<uint, GroupAnchor>();
        foreach (var (gid, s) in spread)
        {
            if (s.maps.Count > 1 || s.zones.Count > 1)
                anchor[gid] = GroupAnchor.Map;
            else if (s.areas.Count > 1)
                anchor[gid] = GroupAnchor.Zone;
            else
                anchor[gid] = GroupAnchor.Area;
        }

        // --- pass 1b: how far does each pool spread? Direct members credit their pool AND every
        // ancestor (the mother must anchor around all its descendants); grouped members still
        // count (the pool may hold other, ungrouped spawns), entry-pooled rows don't contribute
        // (they don't move in the tree). ---
        var poolSpread = new Dictionary<uint, (HashSet<int> maps, HashSet<int> zones, HashSet<int> areas)>();
        foreach (var r in rows)
        {
            if (PoolOf(r) is not uint pid)
                continue;
            int depth = 0;
            for (uint? walk = pid; walk is uint p && depth++ < MaxPoolDepth; walk = MotherOf(p))
            {
                if (!poolSpread.TryGetValue(p, out var s))
                    poolSpread[p] = s = (new HashSet<int>(), new HashSet<int>(), new HashSet<int>());
                s.maps.Add(r.Map);
                if (r.ZoneId is int z) s.zones.Add(z);
                if (r.AreaId is int a) s.areas.Add(a);
            }
        }

        var poolAnchor = new Dictionary<uint, GroupAnchor>();
        foreach (var (pid, s) in poolSpread)
        {
            if (s.maps.Count > 1 || s.zones.Count > 1)
                poolAnchor[pid] = GroupAnchor.Map;
            else if (s.areas.Count > 1)
                poolAnchor[pid] = GroupAnchor.Zone;
            else
                poolAnchor[pid] = GroupAnchor.Area;
        }

        // --- pass 2: build the node tree ---
        var maps = new Dictionary<int, SpawnTreeNode>();
        var zones = new Dictionary<(int, int), SpawnTreeNode>();
        var areas = new Dictionary<(int, int), SpawnTreeNode>();
        var groups = new Dictionary<(uint, SpawnTreeNode), SpawnTreeNode>();
        var poolNodes = new Dictionary<(uint, SpawnTreeNode), SpawnTreeNode>();
        var entries = new Dictionary<(SpawnTreeNode, bool, uint), SpawnTreeNode>();
        var roots = new List<SpawnTreeNode>();

        SpawnTreeNode GetMap(int mapId)
        {
            if (!maps.TryGetValue(mapId, out var n))
            {
                n = new SpawnTreeNode { Kind = SpawnNodeKind.Map, Map = mapId, Label = GetMapName(dbc, mapId) };
                maps[mapId] = n;
                roots.Add(n);
            }
            return n;
        }

        SpawnTreeNode GetZone(in SpawnRow r)
        {
            var mapNode = GetMap(r.Map);
            if (r.ZoneId is not int zid)
                return mapNode;
            var key = (r.Map, zid);
            if (!zones.TryGetValue(key, out var n))
            {
                n = new SpawnTreeNode { Kind = SpawnNodeKind.Zone, Map = r.Map, Entry = (uint)zid, Label = GetAreaName(dbc, zid), Parent = mapNode };
                zones[key] = n;
                mapNode.EnsureChildren().Add(n);
            }
            return n;
        }

        SpawnTreeNode GetArea(in SpawnRow r)
        {
            var zoneNode = GetZone(r);
            if (r.AreaId is not int aid)
                return zoneNode; // the area is the zone itself (or unknown) -> no extra level
            var key = (r.Map, aid);
            if (!areas.TryGetValue(key, out var n))
            {
                n = new SpawnTreeNode { Kind = SpawnNodeKind.Area, Map = r.Map, Entry = (uint)aid, Label = GetAreaName(dbc, aid), Parent = zoneNode };
                areas[key] = n;
                zoneNode.EnsureChildren().Add(n);
            }
            return n;
        }

        SpawnTreeNode GetGroup(uint gid, SpawnTreeNode anchorNode)
        {
            var key = (gid, anchorNode);
            if (!groups.TryGetValue(key, out var n))
            {
                var name = groupNames.TryGetValue(gid, out var t) && !string.IsNullOrEmpty(t) ? t : $"Group {gid}";
                n = new SpawnTreeNode { Kind = SpawnNodeKind.SpawnGroup, Entry = gid, Label = name, Parent = anchorNode };
                groups[key] = n;
                anchorNode.EnsureChildren().Add(n);
            }
            return n;
        }

        SpawnTreeNode GetPool(uint pid, SpawnTreeNode parentNode)
        {
            var key = (pid, parentNode);
            if (!poolNodes.TryGetValue(key, out var n))
            {
                var name = pools.PoolNames.TryGetValue(pid, out var t) && !string.IsNullOrEmpty(t) ? t : $"Pool {pid}";
                n = new SpawnTreeNode { Kind = SpawnNodeKind.Pool, Entry = pid, PoolId = pid, Label = name, Parent = parentNode };
                poolNodes[key] = n;
                parentNode.EnsureChildren().Add(n);
            }
            return n;
        }

        SpawnTreeNode GetEntry(SpawnTreeNode container, bool isCreature, uint entry, string name)
        {
            var key = (container, isCreature, entry);
            if (!entries.TryGetValue(key, out var n))
            {
                var kind = isCreature ? SpawnNodeKind.CreatureEntry : SpawnNodeKind.GameObjectEntry;
                n = new SpawnTreeNode { Kind = kind, Entry = entry, Label = $"{entry} {name}", Parent = container };
                entries[key] = n;
                container.EnsureChildren().Add(n);
            }
            return n;
        }

        var motherChain = new List<uint>();

        foreach (var r in rows)
        {
            var directPool = PoolOf(r);
            bool underPoolNode = false;

            SpawnTreeNode container;
            if (GroupOf(r) is uint gid)
            {
                // group placement wins - a spawn in both a group and a pool keeps its group node
                // and only gets a [pool N] badge
                var anchorNode = anchor[gid] switch
                {
                    GroupAnchor.Area => GetArea(r),
                    GroupAnchor.Zone => GetZone(r),
                    _ => GetMap(r.Map),
                };
                container = GetGroup(gid, anchorNode);
            }
            else if (directPool is uint pid)
            {
                // nested pools: build the mother chain root-first, the whole chain anchored where
                // the ROOT pool spreads (an ancestor always spreads at least as far as its children)
                motherChain.Clear();
                int depth = 0;
                for (uint? walk = pid; walk is uint p && depth++ < MaxPoolDepth; walk = MotherOf(p))
                    motherChain.Add(p);
                motherChain.Reverse();

                container = poolAnchor[motherChain[0]] switch
                {
                    GroupAnchor.Area => GetArea(r),
                    GroupAnchor.Zone => GetZone(r),
                    _ => GetMap(r.Map),
                };
                foreach (var p in motherChain)
                    container = GetPool(p, container);
                underPoolNode = true;
            }
            else
            {
                container = GetArea(r);
            }

            var entryNode = GetEntry(container, r.IsCreature, r.Entry, r.EntryName);
            var leaf = new SpawnTreeNode
            {
                Kind = r.IsCreature ? SpawnNodeKind.CreatureSpawn : SpawnNodeKind.GameObjectSpawn,
                Entry = r.Entry, Guid = r.Guid, Map = r.Map, Position = r.Position,
                Label = $"#{r.Guid}", Parent = entryNode,
            };
            // badge leaves that belong to a pool but sit elsewhere (grouped, or entry-wide pooled)
            if (!underPoolNode)
                leaf.PoolId = directPool
                    ?? (pools.EntryToPool.TryGetValue((r.IsCreature, r.Entry), out var ep) && pools.PoolNames.ContainsKey(ep) ? ep : 0);
            entryNode.EnsureChildren().Add(leaf);
        }

        // --- post pass: entry-wide pool members as display-only children of their pool nodes
        // ("every spawn of the entry" has no single world position, so no real leaves). Pools whose
        // node never materialized (no direct member anywhere) simply skip these. ---
        if (pools.EntryToPool.Count > 0 && poolNodes.Count > 0)
        {
            var entryNames = new Dictionary<(bool, uint), string>();
            foreach (var r in rows)
                entryNames.TryAdd((r.IsCreature, r.Entry), r.EntryName);

            foreach (var ((isCreature, entry), pid) in pools.EntryToPool)
            {
                foreach (var ((nodePid, _), poolNode) in poolNodes)
                {
                    if (nodePid != pid)
                        continue;
                    var name = entryNames.TryGetValue((isCreature, entry), out var n) ? n : "";
                    poolNode.EnsureChildren().Add(new SpawnTreeNode
                    {
                        Kind = isCreature ? SpawnNodeKind.CreatureEntry : SpawnNodeKind.GameObjectEntry,
                        Entry = entry, PoolId = pid, Parent = poolNode,
                        Label = $"{entry} {name} (all spawns)",
                    });
                }
            }
        }

        foreach (var root in roots)
            Finalize(root);
        roots.Sort(CompareNodes);
        return roots;
    }

    /// <summary>Sorts children, computes SearchText and appends spawn counts; returns the subtree's spawn-leaf count.</summary>
    private static int Finalize(SpawnTreeNode n)
    {
        int spawns = n.IsLeaf ? 1 : 0;
        if (n.Children != null)
        {
            n.Children.Sort(CompareNodes);
            foreach (var c in n.Children)
                spawns += Finalize(c);
        }
        // container ids are searchable too ("1519" or "Stormwind" both find the zone; map ids work);
        // SearchText is taken before the count suffix so "(1,234)" never matches a query.
        // Leaves skip it: SearchText would just be Label.ToLowerInvariant() (leaves get no suffix), a
        // second string on every one of ~367K leaf nodes - the filter matches their Label directly instead.
        if (!n.IsLeaf)
        {
            n.SearchText = n.Kind switch
            {
                SpawnNodeKind.Map => $"{n.Label} {n.Map}".ToLowerInvariant(),
                SpawnNodeKind.Zone or SpawnNodeKind.Area => $"{n.Label} {n.Entry}".ToLowerInvariant(),
                _ => n.Label.ToLowerInvariant(),
            };
        }
        if (n.Kind == SpawnNodeKind.Map)
            n.Label = $"{n.Label}  ({n.Map})";
        else if (n.Kind is SpawnNodeKind.CreatureEntry or SpawnNodeKind.GameObjectEntry && n.Children != null)
            n.Label = $"{n.Label}  ({n.Children.Count})";
        else if (!n.IsLeaf && spawns > 0)
            n.Label = $"{n.Label}  ({spawns:N0})";
        return spawns;
    }

    private static int CompareNodes(SpawnTreeNode a, SpawnTreeNode b)
    {
        if (a.IsLeaf && b.IsLeaf)
            return a.Guid.CompareTo(b.Guid);

        // entry nodes: creatures first, then numeric by entry ("200 Bar" before "1000 Foo" was
        // the lexicographic bug); groups/pools/containers sort before the plain entry list
        bool aEntry = a.Kind is SpawnNodeKind.CreatureEntry or SpawnNodeKind.GameObjectEntry;
        bool bEntry = b.Kind is SpawnNodeKind.CreatureEntry or SpawnNodeKind.GameObjectEntry;
        if (aEntry && bEntry)
        {
            if (a.Kind != b.Kind)
                return a.Kind == SpawnNodeKind.CreatureEntry ? -1 : 1;
            int byEntry = a.Entry.CompareTo(b.Entry);
            if (byEntry != 0)
                return byEntry;
        }
        else if (aEntry != bEntry)
            return aEntry ? 1 : -1;

        return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
    }

    // public: the tree window also uses it to splice freshly committed spawns into cached rows
    public static unsafe (int? zone, int? area) ResolveZoneArea(
        DbcManager dbc, ZoneAreaManager zoneArea, int map, Vector3 pos)
    {
        var areaId = zoneArea.GetAreaId(map, pos);
        if (areaId == null)
            return (null, null);
        if (!dbc.AreaTableStore.TryGetValue((uint)areaId.Value, out var a))
            return (null, null);
        if (a->ParentAreaId == 0)
            return (areaId.Value, null); // this area is itself a zone
        return ((int)a->ParentAreaId, areaId.Value);
    }

    private static unsafe string GetMapName(DbcManager dbc, int mapId)
    {
        if (dbc.MapStore.TryGetValue(mapId, out var m))
        {
            var s = Utf8ToString(m->Name);
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return $"Map {mapId}";
    }

    private static unsafe string GetAreaName(DbcManager dbc, int areaId)
    {
        if (dbc.AreaTableStore.TryGetValue((uint)areaId, out var a))
        {
            var s = Utf8ToString(a->Name);
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return $"Area {areaId}";
    }

    private static unsafe string Utf8ToString(Utf8NativeString s)
        => s.IsNull || s.Length == 0 ? "" : Encoding.UTF8.GetString(s.AsSpan());
}
