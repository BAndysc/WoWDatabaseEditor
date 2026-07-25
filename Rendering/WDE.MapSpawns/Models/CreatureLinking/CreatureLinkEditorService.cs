using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.CreatureLinking;

/// <summary>
/// State + persistence for the creature-linking 3D tool. Loads creature_linking (guid) and
/// creature_linking_template (entry) rows for the current map, tracks edits/additions/removals, and
/// saves an idempotent per-row rewrite (DELETE by key + INSERT, a removed row is just the DELETE).
/// No dedicated solution item: like the graveyard/spell-target editors it publishes the save SQL
/// through <see cref="WorldEditQuerySave"/> so the bridge parses it against the generic
/// creature_linking / creature_linking_template table definitions into plain table session items.
/// </summary>
public class CreatureLinkEditorService : ICreatureLinkEditorService
{
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<ICreatureLinking> guidGen;
    private readonly IQueryGenerator<ICreatureLinkingTemplate> templateGen;

    private readonly record struct TemplateKey(uint Entry, uint Map);

    // Guid/entry -> spawn index. The container's CreatureSpawnInstance set only changes on map load or
    // session add/remove (streaming in/out just flips .Creature, it never adds/removes spawn objects),
    // so the index is rebuilt ONLY when the container signals a change - not on a blind frame timer,
    // which was re-walking every spawn on the map several times a second.
    private readonly Dictionary<uint, CreatureSpawnInstance> creatureByGuid = new();
    private readonly Dictionary<uint, List<CreatureSpawnInstance>> creaturesByEntry = new();
    private volatile bool indexDirty = true;

    // links loaded off-thread land here first, merged on the engine thread.
    private readonly ConcurrentQueue<EditableCreatureLink> pendingGuid = new();
    private readonly ConcurrentQueue<EditableCreatureLinkTemplate> pendingTemplate = new();
    private volatile bool pendingReset;

    // keys present in the DB at load time - only these emit a DELETE when removed.
    private readonly HashSet<uint> originalGuids = new();
    private readonly HashSet<TemplateKey> originalTemplates = new();
    private readonly HashSet<uint> deletedGuids = new();
    private readonly HashSet<TemplateKey> deletedTemplates = new();

    public ObservableCollection<EditableCreatureLink> GuidLinks { get; } = new();
    public ObservableCollection<EditableCreatureLinkTemplate> TemplateLinks { get; } = new();

    public CreatureLinkMode Mode { get; set; } = CreatureLinkMode.Guid;
    public object? Selected { get; set; }
    public bool ToolEnabled { get; set; }
    public int LoadedMap { get; private set; } = -1;

    public bool DragActive { get; set; }
    public Vector3 DragFrom { get; set; }
    public Vector3 DragTo { get; set; }
    public bool DragSnapped { get; set; }

    public bool IsSupported => guidGen.TableName != null;
    public bool SupportsTemplateLinks => templateGen.TableName != null;

    public bool AnyDirty =>
        deletedGuids.Count > 0 || deletedTemplates.Count > 0 ||
        GuidLinks.Any(l => l.IsDirty) || TemplateLinks.Any(l => l.IsDirty);

    public CreatureLinkEditorService(ISpawnsContainer spawnsContainer,
        IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<ICreatureLinking> guidGen,
        IQueryGenerator<ICreatureLinkingTemplate> templateGen)
    {
        this.spawnsContainer = spawnsContainer;
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.guidGen = guidGen;
        this.templateGen = templateGen;
        // the spawn set changed (map load, session add/remove) -> the index needs a rebuild
        spawnsContainer.Spawns.SourceCollectionChanged += (_, _) => indexDirty = true;
    }

    public async Task LoadForMap(int mapId)
    {
        // claim the map before the first await so the module's per-frame SyncMap doesn't re-queue
        LoadedMap = mapId;
        pendingReset = true;
        pendingGuid.Clear();
        pendingTemplate.Clear();

        // snapshot the map's creatures so guid links can be filtered to this map
        RebuildIndex();

        var guidRows = IsSupported
            ? (await databaseProvider.GetCreatureLinkingsAsync()) ?? Array.Empty<ICreatureLinking>()
            : Array.Empty<ICreatureLinking>();
        var templateRows = SupportsTemplateLinks
            ? (await databaseProvider.GetCreatureLinkingTemplatesAsync()) ?? Array.Empty<ICreatureLinkingTemplate>()
            : Array.Empty<ICreatureLinkingTemplate>();

        foreach (var row in guidRows)
        {
            // keep links whose slave spawn is on this map; the master may still be streaming in
            if (!creatureByGuid.ContainsKey(row.Guid))
                continue;
            pendingGuid.Enqueue(new EditableCreatureLink(row.Guid, row.MasterGuid, row.Flag));
        }

        foreach (var row in templateRows)
        {
            if (row.Map != (uint)mapId)
                continue;
            pendingTemplate.Enqueue(new EditableCreatureLinkTemplate(row.Entry, row.Map,
                row.MasterEntry, row.Flag, row.SearchRange));
        }
    }

    public void PumpPendingLoads()
    {
        if (pendingReset)
        {
            pendingReset = false;
            GuidLinks.Clear();
            TemplateLinks.Clear();
            Selected = null;
            originalGuids.Clear();
            originalTemplates.Clear();
            deletedGuids.Clear();
            deletedTemplates.Clear();
        }

        while (pendingGuid.TryDequeue(out var link))
        {
            if (GuidLinks.Any(x => x.Guid == link.Guid))
                continue;
            GuidLinks.Add(link);
            originalGuids.Add(link.Guid);
        }
        while (pendingTemplate.TryDequeue(out var link))
        {
            var key = new TemplateKey(link.Entry, link.Map);
            if (TemplateLinks.Any(x => x.Entry == link.Entry && x.Map == link.Map))
                continue;
            TemplateLinks.Add(link);
            originalTemplates.Add(key);
        }

        if (indexDirty)
            RebuildIndex();
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private void RebuildIndex()
    {
        indexDirty = false;
        creatureByGuid.Clear();
        foreach (var list in creaturesByEntry.Values)
            list.Clear();

        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (spawn is not CreatureSpawnInstance creature)
                continue;
            creatureByGuid[creature.Guid] = creature;
            if (!creaturesByEntry.TryGetValue(creature.Entry, out var list))
                creaturesByEntry[creature.Entry] = list = new List<CreatureSpawnInstance>();
            list.Add(creature);
        }
    }

    /// <summary>Resolves a creature by guid. The index is authoritative while clean (it's rebuilt
    /// whenever the container's spawn set changes), so a miss only triggers a rebuild if the index is
    /// stale - never a per-call full scan for a guid that genuinely isn't on the map.</summary>
    private CreatureSpawnInstance? TryGetCreature(uint guid)
    {
        if (creatureByGuid.TryGetValue(guid, out var creature))
            return creature;
        if (!indexDirty)
            return null; // clean index -> the guid simply isn't in this map
        RebuildIndex();
        return creatureByGuid.TryGetValue(guid, out creature) ? creature : null;
    }

    private static Vector3 LivePos(SpawnInstance spawn) => spawn.WorldObject?.Position ?? spawn.Position;

    public bool TryGetCreaturePosition(uint guid, out Vector3 pos)
    {
        if (TryGetCreature(guid) is { } creature)
        {
            pos = LivePos(creature);
            return true;
        }
        pos = default;
        return false;
    }

    public bool TryGetCreatureEntry(uint guid, out uint entry)
    {
        if (TryGetCreature(guid) is { } creature)
        {
            entry = creature.Entry;
            return true;
        }
        entry = 0;
        return false;
    }

    public bool TryGetEndpoints(EditableCreatureLink link, out Vector3 slavePos, out Vector3 masterPos)
    {
        masterPos = default;
        return TryGetCreaturePosition(link.Guid, out slavePos) &&
               TryGetCreaturePosition(link.MasterGuid, out masterPos);
    }

    public void CollectTemplateArrows(EditableCreatureLinkTemplate link, List<(Vector3 slave, Vector3 master)> output)
    {
        output.Clear();
        if (!creaturesByEntry.TryGetValue(link.Entry, out var slaves) || slaves.Count == 0)
            return;
        if (!creaturesByEntry.TryGetValue(link.MasterEntry, out var masters) || masters.Count == 0)
            return;

        float rangeSq = (float)link.SearchRange * link.SearchRange;
        foreach (var slave in slaves)
        {
            var sp = LivePos(slave);
            CreatureSpawnInstance? best = null;
            float bestSq = float.MaxValue;
            foreach (var master in masters)
            {
                if (ReferenceEquals(master, slave))
                    continue;
                var mp = LivePos(master);
                float d2 = DistSq2D(sp, mp);
                if (link.SearchRange > 0 && d2 > rangeSq)
                    continue;
                if (d2 < bestSq)
                {
                    bestSq = d2;
                    best = master;
                }
            }
            if (best != null)
                output.Add((sp, LivePos(best)));
        }
    }

    private static float DistSq2D(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    public EditableCreatureLink? AddGuidLink(uint slaveGuid, uint masterGuid)
    {
        if (slaveGuid == masterGuid)
            return null;
        if (TryGetCreature(slaveGuid) == null || TryGetCreature(masterGuid) == null)
            return null;

        // re-adding a key that was removed this session cancels its pending DELETE - otherwise the
        // save's trailing delete-loop would wipe the row this INSERT just wrote
        deletedGuids.Remove(slaveGuid);

        var existing = GuidLinks.FirstOrDefault(x => x.Guid == slaveGuid);
        uint flag = existing?.Flag ?? 0;
        if (existing != null)
            GuidLinks.Remove(existing);

        var link = new EditableCreatureLink(slaveGuid, masterGuid, flag, dirty: true);
        GuidLinks.Add(link);
        Selected = link;
        return link;
    }

    public EditableCreatureLinkTemplate? AddTemplateLink(uint slaveEntry, uint map, uint masterEntry)
    {
        if (slaveEntry == masterEntry || slaveEntry == 0 || masterEntry == 0)
            return null;

        // re-adding a key removed this session cancels its pending DELETE (see AddGuidLink)
        deletedTemplates.Remove(new TemplateKey(slaveEntry, map));

        var existing = TemplateLinks.FirstOrDefault(x => x.Entry == slaveEntry && x.Map == map);
        uint flag = existing?.Flag ?? 0;
        uint range = existing?.SearchRange ?? 0;
        if (existing != null)
            TemplateLinks.Remove(existing);

        var link = new EditableCreatureLinkTemplate(slaveEntry, map, masterEntry, flag, range, dirty: true);
        TemplateLinks.Add(link);
        Selected = link;
        return link;
    }

    public void RemoveGuidLink(EditableCreatureLink link)
    {
        GuidLinks.Remove(link);
        if (originalGuids.Contains(link.Guid))
            deletedGuids.Add(link.Guid);
        if (ReferenceEquals(Selected, link))
            Selected = null;
    }

    public void RemoveTemplateLink(EditableCreatureLinkTemplate link)
    {
        TemplateLinks.Remove(link);
        var key = new TemplateKey(link.Entry, link.Map);
        if (originalTemplates.Contains(key))
            deletedTemplates.Add(key);
        if (ReferenceEquals(Selected, link))
            Selected = null;
    }

    public IQuery? BuildSaveQuery()
    {
        if (!AnyDirty)
            return null;

        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        // guid links: idempotent per-row rewrite
        foreach (var link in GuidLinks.Where(l => l.IsDirty))
        {
            Add(guidGen.TryDelete(new AbstractCreatureLinking { Guid = link.Guid }));
            Add(guidGen.TryInsert(link));
        }
        foreach (var guid in deletedGuids)
            Add(guidGen.TryDelete(new AbstractCreatureLinking { Guid = guid }));

        // template links
        foreach (var link in TemplateLinks.Where(l => l.IsDirty))
        {
            Add(templateGen.TryDelete(new AbstractCreatureLinkingTemplate { Entry = link.Entry, Map = link.Map }));
            Add(templateGen.TryInsert(link));
        }
        foreach (var key in deletedTemplates)
            Add(templateGen.TryDelete(new AbstractCreatureLinkingTemplate { Entry = key.Entry, Map = key.Map }));

        return multi?.Close();
    }

    public async Task Save()
    {
        var query = BuildSaveQuery();
        if (query == null)
            return;

        // no dedicated solution item: the bridge parses this SQL against the generic
        // creature_linking / creature_linking_template definitions into table session items. The
        // parse must see the pre-save database (DELETE detection skips rows that don't exist), hence
        // the parse-then-execute handshake. Inert in headless hosts (nobody sets Handled).
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
        save.NotifyExecuted();

        // commit in-memory: everything currently loaded is now the DB state
        deletedGuids.Clear();
        deletedTemplates.Clear();
        originalGuids.Clear();
        originalTemplates.Clear();
        foreach (var link in GuidLinks)
        {
            link.ClearDirty();
            originalGuids.Add(link.Guid);
        }
        foreach (var link in TemplateLinks)
        {
            link.ClearDirty();
            originalTemplates.Add(new TemplateKey(link.Entry, link.Map));
        }
    }
}
