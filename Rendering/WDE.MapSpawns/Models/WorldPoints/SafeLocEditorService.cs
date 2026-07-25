using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.WorldPoints;

/// <summary>The full editable state of one <c>world_safe_locs</c> row plus its
/// <c>game_graveyard_zone</c> links. The UI mutates it directly and calls
/// <see cref="ISafeLocEditorService.NotifyChanged"/>.</summary>
public sealed class SafeLocData
{
    public uint Id { get; init; }
    public uint Map;
    public Vector3 Position;
    public float Orientation;
    public string Name = "";
    public readonly List<GraveyardLinkRow> Links = new();
}

public struct GraveyardLinkRow
{
    public uint GhostLoc;
    public GraveyardLinkKind Kind;
    /// <summary>0 = both teams, 67 = Horde, 469 = Alliance.</summary>
    public uint Faction;
}

/// <summary>
/// World safe locs (graveyards) editor state: all rows are loaded globally (links may cross maps),
/// the 3D module renders only the current map's. Same lifecycle as the spawn-group service -
/// per-id dirty/new/deleted tracking, pending-deleted id reservation, idempotent per-id
/// DELETE+INSERT save, and a key-only solution item per saved id handed to the session.
/// </summary>
[UniqueProvider]
public interface ISafeLocEditorService
{
    bool IsSupported { get; }
    bool HasData { get; }
    int LoadedMap { get; }
    bool AnyDirty { get; }
    /// <summary>Bumped on every state change - observers use it to invalidate caches.</summary>
    int Revision { get; }
    IReadOnlyDictionary<uint, SafeLocData> Locs { get; }
    Task LoadForMap(int mapId);
    /// <summary>Merges an off-thread load into the live state. Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();
    uint CreateAt(uint map, Vector3 position, float orientation);
    /// <summary>Marks the loc dirty after direct mutation of its <see cref="SafeLocData"/>.</summary>
    void NotifyChanged(uint id);
    void DeleteLoc(uint id);
    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    IQuery? BuildSaveQuery();
    Task Save();
}

public class SafeLocEditorService : ISafeLocEditorService
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<IWorldSafeLoc> locGen;
    private readonly IQueryGenerator<IGraveyardLink> linkGen;

    private sealed class Loaded
    {
        public required int MapId;
        public required Dictionary<uint, SafeLocData> Locs;
    }

    private volatile Loaded? pending;

    private readonly Dictionary<uint, SafeLocData> locs = new();
    private readonly HashSet<uint> dirty = new();
    private readonly HashSet<uint> newLocs = new();
    private readonly HashSet<uint> deletedLocs = new();

    public bool IsSupported => locGen.TableName != null && linkGen.TableName != null;
    public bool HasData { get; private set; }
    public int LoadedMap { get; private set; } = -1;
    public bool AnyDirty => dirty.Count > 0;
    public int Revision { get; private set; }
    public IReadOnlyDictionary<uint, SafeLocData> Locs => locs;

    public SafeLocEditorService(IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<IWorldSafeLoc> locGen,
        IQueryGenerator<IGraveyardLink> linkGen)
    {
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.locGen = locGen;
        this.linkGen = linkGen;
    }

    public async Task LoadForMap(int mapId)
    {
        // claim the map BEFORE the first await - the module's per-frame SyncMap compares against
        // LoadedMap, and the getters can stall behind the app cache warm-up; without this a new
        // full load is queued every frame until the first one lands (see PoolEditorService)
        LoadedMap = mapId;

        var rows = await databaseProvider.GetWorldSafeLocsAsync();
        var links = await databaseProvider.GetGraveyardLinksAsync();
        var map = new Dictionary<uint, SafeLocData>();
        if (rows != null)
        {
            foreach (var r in rows)
            {
                map[r.Id] = new SafeLocData
                {
                    Id = r.Id,
                    Map = r.Map,
                    Position = new Vector3(r.X, r.Y, r.Z),
                    Orientation = r.O,
                    Name = r.Name,
                };
            }
        }
        if (links != null)
        {
            foreach (var l in links)
            {
                if (map.TryGetValue(l.SafeLocId, out var d))
                    d.Links.Add(new GraveyardLinkRow { GhostLoc = l.GhostLoc, Kind = l.LinkKind, Faction = l.Faction });
            }
        }

        pending = new Loaded { MapId = mapId, Locs = map };
    }

    public void PumpPendingLoads()
    {
        var p = pending;
        if (p == null)
            return;
        pending = null;

        locs.Clear();
        dirty.Clear();
        newLocs.Clear();
        deletedLocs.Clear();
        foreach (var (id, d) in p.Locs)
            locs[id] = d;
        LoadedMap = p.MapId;
        HasData = true;
        Revision++;
    }

    public uint CreateAt(uint map, Vector3 position, float orientation)
    {
        uint id = NextFreeId();
        locs[id] = new SafeLocData
        {
            Id = id,
            Map = map,
            Position = position,
            Orientation = orientation,
            Name = $"Safe loc {id}",
        };
        newLocs.Add(id);
        dirty.Add(id);
        Revision++;
        return id;
    }

    public void NotifyChanged(uint id)
    {
        if (!locs.ContainsKey(id))
            return;
        dirty.Add(id);
        Revision++;
    }

    public void DeleteLoc(uint id)
    {
        if (!locs.Remove(id))
            return;

        if (newLocs.Remove(id))
            dirty.Remove(id); // never saved - nothing to delete in the database
        else
        {
            deletedLocs.Add(id);
            dirty.Add(id);
        }
        Revision++;
    }

    private uint NextFreeId()
    {
        uint max = 0;
        foreach (var id in locs.Keys)
            max = Math.Max(max, id);
        // pending-deleted ids stay reserved until Save applies the deletes (see spawn groups)
        foreach (var id in deletedLocs)
            max = Math.Max(max, id);
        return max + 1;
    }

    private IWorldSafeLoc Row(SafeLocData d) => new AbstractWorldSafeLoc
    {
        Id = d.Id,
        Map = d.Map,
        X = d.Position.X,
        Y = d.Position.Y,
        Z = d.Position.Z,
        O = d.Orientation,
        Name = d.Name,
    };

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

        foreach (var id in dirty)
        {
            // idempotent per-loc rewrite; a deleted loc is just the DELETEs
            Add(locGen.TryDelete(new AbstractWorldSafeLoc { Id = id }));
            Add(linkGen.TryDeleteAll(new AbstractGraveyardLink { SafeLocId = id }));

            if (deletedLocs.Contains(id) || !locs.TryGetValue(id, out var d))
                continue;

            Add(locGen.TryInsert(Row(d)));
            if (d.Links.Count > 0)
                Add(linkGen.TryBulkInsert(d.Links
                    .DistinctBy(l => (l.GhostLoc, l.Kind))
                    .Select(l => (IGraveyardLink)new AbstractGraveyardLink
                    {
                        SafeLocId = id, GhostLoc = l.GhostLoc, LinkKind = l.Kind, Faction = l.Faction,
                    }).ToList()));
        }

        return multi?.Close();
    }

    public async Task Save()
    {
        var query = BuildSaveQuery();
        if (query == null)
            return;

        // no dedicated solution item: the bridge parses this SQL against the generic table
        // definitions into plain table solution items. Parsing MUST see the pre-save database
        // (DELETE detection skips rows that don't exist), hence the parse-then-execute handshake.
        var save = new WorldEditQuerySave(query.QueryString);
        eventAggregator.GetEvent<WorldEditQuerySavingEvent>().Publish(save);
        if (save.Handled)
            await save.Parsed;

        try
        {
            await mainThread.Schedule(async () =>
            {
                await mySqlExecutor.ExecuteSql(query);
                return true;
            });
        }
        catch
        {
            save.NotifyExecutionFailed();
            throw;
        }
        save.NotifyExecuted(); // the bridge now pushes the parsed items into the session

        dirty.Clear();
        newLocs.Clear();
        deletedLocs.Clear();
        Revision++;
    }
}
