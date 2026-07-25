using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.WorldPoints;

/// <summary>One editable <c>spell_target_position</c> row - the destination of a spell with the
/// TARGET_LOCATION_DATABASE (17) implicit target. Keyed by spell id: one destination per spell.</summary>
public sealed class SpellTargetData
{
    public uint SpellId { get; init; }
    public uint Map;
    public Vector3 Position;
    public float Orientation;
}

[UniqueProvider]
public interface ISpellTargetEditorService
{
    bool IsSupported { get; }
    bool HasData { get; }
    int LoadedMap { get; }
    bool AnyDirty { get; }
    int Revision { get; }
    IReadOnlyDictionary<uint, SpellTargetData> Positions { get; }
    Task LoadForMap(int mapId);
    void PumpPendingLoads();
    /// <summary>Spell id -> name, snapshotted from the app-side spell store at load time
    /// (the game thread must not call app services directly).</summary>
    string? GetSpellName(uint spellId);
    /// <summary>Creates (or returns the existing) row for the spell - the table allows exactly one
    /// destination per spell. Returns whether a new row was created.</summary>
    bool CreateForSpell(uint spellId, uint map, Vector3 position, float orientation);
    void NotifyChanged(uint spellId);
    void DeletePosition(uint spellId);
    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    IQuery? BuildSaveQuery();
    Task Save();
}

public class SpellTargetEditorService : ISpellTargetEditorService
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IParameterFactory parameterFactory;
    private readonly IQueryGenerator<ISpellTargetPosition> positionGen;

    private sealed class Loaded
    {
        public required int MapId;
        public required Dictionary<uint, SpellTargetData> Positions;
        public required Dictionary<uint, string> SpellNames;
    }

    private volatile Loaded? pending;

    private readonly Dictionary<uint, SpellTargetData> positions = new();
    private readonly Dictionary<uint, string> spellNames = new();
    private readonly HashSet<uint> dirty = new();
    private readonly HashSet<uint> newRows = new();
    private readonly HashSet<uint> deletedRows = new();

    public bool IsSupported => positionGen.TableName != null;
    public bool HasData { get; private set; }
    public int LoadedMap { get; private set; } = -1;
    public bool AnyDirty => dirty.Count > 0;
    public int Revision { get; private set; }
    public IReadOnlyDictionary<uint, SpellTargetData> Positions => positions;

    public SpellTargetEditorService(IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IParameterFactory parameterFactory,
        IQueryGenerator<ISpellTargetPosition> positionGen)
    {
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.parameterFactory = parameterFactory;
        this.positionGen = positionGen;
    }

    public async Task LoadForMap(int mapId)
    {
        // claim the map BEFORE the first await - the module's per-frame SyncMap compares against
        // LoadedMap, and the getters can stall behind the app cache warm-up; without this a new
        // full load is queued every frame until the first one lands (see PoolEditorService)
        LoadedMap = mapId;

        var rows = await databaseProvider.GetSpellTargetPositionsAsync();
        var map = new Dictionary<uint, SpellTargetData>();
        if (rows != null)
        {
            foreach (var r in rows)
            {
                map[r.SpellId] = new SpellTargetData
                {
                    SpellId = r.SpellId,
                    Map = r.Map,
                    Position = new Vector3(r.X, r.Y, r.Z),
                    Orientation = r.O,
                };
            }
        }

        // spell names live in the app-side parameter store - snapshot them on the main thread
        var names = await mainThread.Schedule(() =>
        {
            var result = new Dictionary<uint, string>();
            var spellParameter = parameterFactory.Factory("SpellParameter");
            if (spellParameter.Items is { } items)
            {
                foreach (var (key, option) in items)
                {
                    if (key >= 0)
                        result[(uint)key] = option.Name;
                }
            }
            return result;
        });

        pending = new Loaded { MapId = mapId, Positions = map, SpellNames = names };
    }

    public void PumpPendingLoads()
    {
        var p = pending;
        if (p == null)
            return;
        pending = null;

        positions.Clear();
        spellNames.Clear();
        dirty.Clear();
        newRows.Clear();
        deletedRows.Clear();
        foreach (var (id, d) in p.Positions)
            positions[id] = d;
        foreach (var (id, name) in p.SpellNames)
            spellNames[id] = name;
        LoadedMap = p.MapId;
        HasData = true;
        Revision++;
    }

    public string? GetSpellName(uint spellId) =>
        spellNames.TryGetValue(spellId, out var name) ? name : null;

    public bool CreateForSpell(uint spellId, uint map, Vector3 position, float orientation)
    {
        if (positions.ContainsKey(spellId))
            return false;

        deletedRows.Remove(spellId); // re-creating a pending-deleted spell id = plain rewrite
        positions[spellId] = new SpellTargetData
        {
            SpellId = spellId,
            Map = map,
            Position = position,
            Orientation = orientation,
        };
        newRows.Add(spellId);
        dirty.Add(spellId);
        Revision++;
        return true;
    }

    public void NotifyChanged(uint spellId)
    {
        if (!positions.ContainsKey(spellId))
            return;
        dirty.Add(spellId);
        Revision++;
    }

    public void DeletePosition(uint spellId)
    {
        if (!positions.Remove(spellId))
            return;

        if (newRows.Remove(spellId))
            dirty.Remove(spellId); // never saved - nothing to delete in the database
        else
        {
            deletedRows.Add(spellId);
            dirty.Add(spellId);
        }
        Revision++;
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

        foreach (var id in dirty)
        {
            // idempotent per-spell rewrite; a deleted row is just the DELETE
            Add(positionGen.TryDelete(new AbstractSpellTargetPosition { SpellId = id }));

            if (deletedRows.Contains(id) || !positions.TryGetValue(id, out var d))
                continue;

            Add(positionGen.TryInsert(new AbstractSpellTargetPosition
            {
                SpellId = d.SpellId,
                Map = d.Map,
                X = d.Position.X,
                Y = d.Position.Y,
                Z = d.Position.Z,
                O = d.Orientation,
            }));
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
        newRows.Clear();
        deletedRows.Clear();
        Revision++;
    }
}
