using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Prism.Events;
using TheMaths;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Waypoints;

public class WaypointEditorService : IWaypointEditorService
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ICurrentCoreVersion coreVersion;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<IWaypointData> waypointDataGen;
    private readonly IQueryGenerator<ISmartScriptWaypoint> smartScriptGen;
    private readonly IQueryGenerator<IScriptWaypoint> scriptWaypointGen;
    private readonly IQueryGenerator<IMangosWaypoint> mangosWaypointGen;
    private readonly IQueryGenerator<IMangosCreatureMovement> mangosMovementGen;
    private readonly IQueryGenerator<IMangosCreatureMovementTemplate> mangosMovementTemplateGen;
    private readonly IQueryGenerator<IWaypointPathHeader> pathHeaderGen;
    private readonly IQueryGenerator<IMangosWaypointsPathName> pathNameGen;

    // paths loaded off-thread land here first and are merged into LoadedPaths on the engine
    // thread via PumpPendingLoads, so the render stage never sees the collection mutated mid-frame
    private readonly ConcurrentQueue<EditablePath> pendingAdds = new();

    // The in-memory paths: the ONE currently edited path (SelectedPath) plus any paths that were
    // edited earlier and still have unsaved changes ("staged"). This is NOT a user-browsable list of
    // "loaded paths" - the editor only ever shows/edits a single path at a time. Staged paths just
    // linger here so their pending edits aren't lost and ride the unified toolbar Save, exactly like
    // pending spawn edits. A clean path drops out the moment it stops being the current one.
    public ObservableCollection<EditablePath> LoadedPaths { get; } = new();

    private EditablePath? selectedPath;

    /// <summary>The one path currently open for editing (null = none). Opening another path stages the
    /// outgoing one when it has unsaved edits (kept pending), or drops it when it's clean.</summary>
    public EditablePath? SelectedPath
    {
        get => selectedPath;
        set
        {
            if (ReferenceEquals(selectedPath, value))
                return;
            var previous = selectedPath;
            selectedPath = value;
            // selecting another path never silently keeps the old one armed for appending
            if (!ReferenceEquals(editingPath, value))
                editingPath = null;
            // one editable path at a time: the path we just left stays in memory only if it still
            // has unsaved edits (staged/pending, saved by the unified Save) - a clean one is dropped
            if (previous != null && !ReferenceEquals(previous, value) && !previous.IsDirty)
                LoadedPaths.Remove(previous);
        }
    }

    private EditablePath? editingPath;
    public EditablePath? EditingPath
    {
        get => editingPath;
        set
        {
            editingPath = value;
            if (value != null)
                SelectedPath = value;
        }
    }

    public int SelectedPointIndex { get; set; } = -1;
    public IReadOnlyList<WaypointSource> AvailableSources { get; }

    public WaypointEditorService(IDatabaseProvider databaseProvider,
        ICurrentCoreVersion coreVersion,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<IWaypointData> waypointDataGen,
        IQueryGenerator<ISmartScriptWaypoint> smartScriptGen,
        IQueryGenerator<IScriptWaypoint> scriptWaypointGen,
        IQueryGenerator<IMangosWaypoint> mangosWaypointGen,
        IQueryGenerator<IMangosCreatureMovement> mangosMovementGen,
        IQueryGenerator<IMangosCreatureMovementTemplate> mangosMovementTemplateGen,
        IQueryGenerator<IWaypointPathHeader> pathHeaderGen,
        IQueryGenerator<IMangosWaypointsPathName> pathNameGen,
        IEnumerable<ICreaturePathAttachmentProvider> attachmentProviders,
        IEnumerable<IWaypointSchemaInfoProvider> schemaInfoProviders,
        IWorldSpawnEditService worldEditService)
    {
        this.pathHeaderGen = pathHeaderGen;
        this.pathNameGen = pathNameGen;
        this.databaseProvider = databaseProvider;
        this.coreVersion = coreVersion;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.waypointDataGen = waypointDataGen;
        this.smartScriptGen = smartScriptGen;
        this.scriptWaypointGen = scriptWaypointGen;
        this.mangosWaypointGen = mangosWaypointGen;
        this.mangosMovementGen = mangosMovementGen;
        this.mangosMovementTemplateGen = mangosMovementTemplateGen;

        AvailableSources = WaypointSources.All.Where(s => s.IsSupported(coreVersion.Current)).ToList();

        this.worldEditService = worldEditService;

        // at most one of each is registered per core ([RequiresCore]); none = the feature is hidden
        schemaInfo = schemaInfoProviders.FirstOrDefault();
        attachmentProvider = attachmentProviders.FirstOrDefault();
        creaturePathSource = attachmentProvider == null
            ? null
            : WaypointSources.All.Select(s => (WaypointSource?)s).FirstOrDefault(s => s!.Value.ToFlag() == attachmentProvider.PathTable);
    }

    public EditablePath? FindLoaded(WaypointSource source, uint key, uint key2 = 0) =>
        LoadedPaths.FirstOrDefault(p => p.Source == source && p.Key == key && p.Key2 == key2);

    public async Task<EditablePath?> LoadPath(WaypointSource source, uint key, uint autoLoadedFromCreatureGuid = 0, uint key2 = 0)
    {
        var existing = FindLoaded(source, key, key2);
        if (existing != null)
        {
            SelectedPath = existing; // reopen an already-in-memory (e.g. staged/pending) path
            return existing;
        }

        var points = await LoadPoints(source, key, key2);
        if (points == null)
            return null;

        var path = new EditablePath(source, key, points, autoLoadedFromCreatureGuid, schemaInfo, key2);
        await LoadPathExtras(path);
        // NOTE: awaits started from game code resume on the game loop (TheEngineSynchronizationContext),
        // not the thread pool - the queue stays as a guard for callers awaiting from other contexts
        pendingAdds.Enqueue(path);
        return path;
    }

    /// <summary>Loads the path-LEVEL data some sources have next to their points: the master
    /// waypoint_path metadata row and the cmangos waypoint_path_name row.</summary>
    private async Task LoadPathExtras(EditablePath path)
    {
        var columns = ColumnsFor(path.Source);
        if (columns.HasFlagFast(WaypointColumns.PathHeader))
        {
            var header = await databaseProvider.GetWaypointPathHeader(path.Key);
            path.SetHeaderLoaded(header != null
                ? WaypointPathHeader.From(header)
                : new WaypointPathHeader { PathId = path.Key });
        }
        if (columns.HasFlagFast(WaypointColumns.PathName))
        {
            var name = await databaseProvider.GetMangosPathName(path.Key);
            path.SetPathNameLoaded(name?.Name);
        }
    }

    /// <summary>A brand-new path's defaults for the same path-level extras (nothing to load).</summary>
    private void InitPathExtras(EditablePath path)
    {
        if (ColumnsFor(path.Source).HasFlagFast(WaypointColumns.PathHeader))
            path.SetHeaderLoaded(new WaypointPathHeader { PathId = path.Key });
    }

    public WaypointColumns ColumnsFor(WaypointSource source) =>
        schemaInfo?.Columns(source.ToFlag()) ?? FallbackColumns(source);

    // cores without a schema-info provider (none today) keep the historical field set
    private static WaypointColumns FallbackColumns(WaypointSource source) => source switch
    {
        WaypointSource.TrinityWaypointData => WaypointColumns.Orientation | WaypointColumns.MoveType |
                                              WaypointColumns.Action | WaypointColumns.ActionChance,
        WaypointSource.SmartScriptWaypoint => WaypointColumns.Orientation | WaypointColumns.Comment,
        WaypointSource.ScriptWaypoint => WaypointColumns.Comment,
        WaypointSource.MangosWaypointPath or WaypointSource.MangosCreatureMovement
            or WaypointSource.MangosCreatureMovementTemplate =>
            WaypointColumns.Orientation | WaypointColumns.ScriptId | WaypointColumns.Comment,
        _ => WaypointColumns.None,
    };

    /// <summary>Merges off-thread loaded paths into LoadedPaths and opens the newest one for editing.
    /// Call once per frame on the engine thread.</summary>
    public void PumpPendingLoads()
    {
        while (pendingAdds.TryDequeue(out var path))
        {
            if (path.Unloaded) // unloaded before it was ever pumped (e.g. remove-waypoints) - skip
                continue;
            // reuse the in-memory instance (with its staged edits) if the same path is already known
            var target = FindLoaded(path.Source, path.Key) ?? path;
            if (ReferenceEquals(target, path))
                LoadedPaths.Add(path);
            SelectedPath = target; // a freshly loaded path becomes the one open path
        }
    }

    public EditablePath CreateNew(WaypointSource source, uint key, uint key2 = 0)
    {
        var existing = FindLoaded(source, key, key2);
        if (existing != null)
        {
            SelectedPath = existing;
            return existing;
        }
        var path = new EditablePath(source, key, null, schema: schemaInfo, key2: key2);
        InitPathExtras(path);
        LoadedPaths.Add(path);
        SelectedPath = path;
        return path;
    }

    public void Unload(EditablePath path)
    {
        path.MarkUnloaded();
        LoadedPaths.Remove(path);
        if (EditingPath == path)
            EditingPath = null;
        if (SelectedPath == path)
        {
            SelectedPath = null;
            SelectedPointIndex = -1;
        }
    }

    private Task<List<UniversalWaypoint>?> LoadPoints(WaypointSource source, uint key, uint key2 = 0) =>
        WaypointDbLoader.LoadPoints(databaseProvider, source, key, key2);

    public async Task<IReadOnlyList<Vector3>?> LoadPointsPreview(WaypointSource source, uint key, uint key2 = 0)
    {
        var points = await LoadPoints(source, key, key2);
        if (points == null || points.Count == 0)
            return null;
        return points.Select(p => new Vector3(p.X, p.Y, p.Z)).ToList();
    }

    private DatabaseTable? TableFor(WaypointSource source) => source switch
    {
        WaypointSource.TrinityWaypointData => waypointDataGen.TableName,
        WaypointSource.SmartScriptWaypoint => smartScriptGen.TableName,
        WaypointSource.ScriptWaypoint => scriptWaypointGen.TableName,
        WaypointSource.MangosWaypointPath => mangosWaypointGen.TableName,
        WaypointSource.MangosCreatureMovement => mangosMovementGen.TableName,
        WaypointSource.MangosCreatureMovementTemplate => mangosMovementTemplateGen.TableName,
        _ => null,
    };

    // per-core schema details (path-id columns) - registered per core, no schema knowledge here
    private readonly IWaypointSchemaInfoProvider? schemaInfo;

    public async Task<IReadOnlyList<uint>> EnumeratePathIds(WaypointSource source)
    {
        var table = TableFor(source)?.Table;
        var idCol = schemaInfo?.PathIdColumn(source.ToFlag());
        if (table == null || idCol == null)
            return Array.Empty<uint>();
        var sql = $"SELECT DISTINCT `{idCol}` AS id FROM `{table}` ORDER BY `{idCol}`";
        try
        {
            var result = await mainThread.Schedule(() => mySqlExecutor.ExecuteSelectSql(sql));
            var ids = new List<uint>(result.Rows);
            for (int row = 0; row < result.Rows; ++row)
                ids.Add(result.Value<uint>(row, 0));
            return ids;
        }
        catch (Exception)
        {
            return Array.Empty<uint>();
        }
    }

    public async Task<IReadOnlyList<(uint key, uint key2)>> EnumerateCompoundKeys(WaypointSource source)
    {
        // only compound-keyed sources (creature_movement_template) have a second key column; the two
        // key columns are the source's PathIdColumn (primary) + the fixed secondary "PathId"
        var table = TableFor(source)?.Table;
        var primaryCol = schemaInfo?.PathIdColumn(source.ToFlag());
        if (table == null || primaryCol == null || !source.UsesSecondaryKey())
            return Array.Empty<(uint, uint)>();
        const string secondaryCol = "PathId"; // creature_movement_template's second key component
        var sql = $"SELECT DISTINCT `{primaryCol}`, `{secondaryCol}` FROM `{table}` ORDER BY `{primaryCol}`, `{secondaryCol}`";
        try
        {
            var result = await mainThread.Schedule(() => mySqlExecutor.ExecuteSelectSql(sql));
            var keys = new List<(uint, uint)>(result.Rows);
            for (int row = 0; row < result.Rows; ++row)
                keys.Add((result.Value<uint>(row, 0), result.Value<uint>(row, 1)));
            return keys;
        }
        catch (Exception)
        {
            return Array.Empty<(uint, uint)>();
        }
    }

    private static readonly IReadOnlyDictionary<uint, string> NoPathNames = new Dictionary<uint, string>();

    public async Task<IReadOnlyDictionary<uint, string>> EnumeratePathNames(WaypointSource source)
    {
        // per-path display text: the cmangos waypoint_path_name row, or TC master's
        // waypoint_path.Comment - whichever the source's schema actually has
        var columns = ColumnsFor(source);
        DatabaseTable? table;
        string nameCol;
        if (columns.HasFlagFast(WaypointColumns.PathName))
            (table, nameCol) = (pathNameGen.TableName, "Name");
        else if (columns.HasFlagFast(WaypointColumns.PathHeader))
            (table, nameCol) = (pathHeaderGen.TableName, "Comment");
        else
            return NoPathNames;
        if (table == null)
            return NoPathNames;

        var sql = $"SELECT `PathId`, `{nameCol}` FROM `{table.Value.Table}`";
        try
        {
            var result = await mainThread.Schedule(() => mySqlExecutor.ExecuteSelectSql(sql));
            var names = new Dictionary<uint, string>(result.Rows);
            for (int row = 0; row < result.Rows; ++row)
            {
                if (result.IsNull(row, 1))
                    continue;
                var name = result.Value<string>(row, 1);
                if (!string.IsNullOrWhiteSpace(name))
                    names[result.Value<uint>(row, 0)] = name;
            }
            return names;
        }
        catch (Exception)
        {
            return NoPathNames;
        }
    }

    public bool AnyDirty => LoadedPaths.Any(p => p.IsDirty);

    public async Task SaveAllDirty()
    {
        foreach (var path in LoadedPaths.Where(p => p.IsDirty).ToList())
            await Save(path);
    }

    // --- creature-attached paths -------------------------------------------------------------------

    // full-editor spawn edit facade: attach/detach go through the hosted creature document when the
    // bridge answers (session/undo/save first-class); otherwise the providers' direct SQL runs.
    private readonly IWorldSpawnEditService worldEditService;

    // per-core creature<->path attachment (resolution, path id convention, attach/detach SQL);
    // null = the active core registered no provider and the feature is hidden. ALL core-specific
    // knowledge (tables, columns, conventions) lives in the [RequiresCore] providers, none here.
    private readonly ICreaturePathAttachmentProvider? attachmentProvider;
    private readonly WaypointSource? creaturePathSource;

    public bool SupportsCreaturePaths => attachmentProvider != null && creaturePathSource != null;

    // creature_movement_template is entry-shared (keyed by entry + pathId), separate from the per-guid
    // creature_movement path - available whenever the active core supports the template source
    public bool SupportsCreatureTemplatePaths => AvailableSources.Contains(WaypointSource.MangosCreatureMovementTemplate);

    // entry -> the entry has creature_movement_template rows (null = check in flight). Filled by
    // async checks kicked off from HasCreatureTemplatePath, dropped on template-path saves, so the
    // inspector can tell "edit the existing path" from "add one" without per-frame queries.
    private readonly ConcurrentDictionary<uint, bool?> templatePathExists = new();

    public bool? HasCreatureTemplatePath(uint entry)
    {
        if (!SupportsCreatureTemplatePaths)
            return false;
        if (templatePathExists.TryGetValue(entry, out var exists))
            return exists;
        if (templatePathExists.TryAdd(entry, null))
            CheckTemplatePathExists(entry).ListenErrors();
        return null;
    }

    private async Task CheckTemplatePathExists(uint entry)
    {
        var table = TableFor(WaypointSource.MangosCreatureMovementTemplate)?.Table;
        var entryCol = schemaInfo?.PathIdColumn(WaypointSource.MangosCreatureMovementTemplate.ToFlag());
        if (table == null || entryCol == null)
        {
            templatePathExists[entry] = false;
            return;
        }
        try
        {
            var sql = $"SELECT 1 FROM `{table}` WHERE `{entryCol}` = {entry} LIMIT 1";
            var result = await mainThread.Schedule(() => mySqlExecutor.ExecuteSelectSql(sql));
            templatePathExists[entry] = result.Rows > 0;
        }
        catch (Exception)
        {
            templatePathExists[entry] = false;
        }
    }

    // paths attached this session (creature.Addon only reflects the DB state from map load).
    // Concurrent: written on the engine thread, read by ResolveCreaturePath from the UI thread (menu).
    private readonly ConcurrentDictionary<uint, uint> sessionAttachedPaths = new();

    // add/edit/remove/edit-template requests from UI-thread surfaces, consumed by the module on the
    // engine thread
    private readonly ConcurrentQueue<(CreatureSpawnInstance creature, CreaturePathAction action)> creatureActions = new();

    public void RequestEditCreaturePath(CreatureSpawnInstance creature)
    {
        WDE.Common.USAGE.Count("3d_action", ("action", "waypoints_edit"));
        creatureActions.Enqueue((creature, CreaturePathAction.Edit));
    }

    public void RequestRemoveCreaturePath(CreatureSpawnInstance creature) => creatureActions.Enqueue((creature, CreaturePathAction.Remove));

    public void RequestEditCreatureTemplatePath(CreatureSpawnInstance creature)
    {
        WDE.Common.USAGE.Count("3d_action", ("action", "waypoints_edit_template"));
        creatureActions.Enqueue((creature, CreaturePathAction.EditTemplate));
    }

    public bool TryDequeueCreatureAction(out CreatureSpawnInstance creature, out CreaturePathAction action)
    {
        if (creatureActions.TryDequeue(out var queued))
        {
            creature = queued.creature;
            action = queued.action;
            return true;
        }
        creature = null!;
        action = CreaturePathAction.Edit;
        return false;
    }

    // --- external path import (e.g. sniffed movement) --------------------------------------------

    private readonly ConcurrentQueue<WaypointImportRequest> importRequests = new();

    public List<WaypointPreviewPath> PreviewPaths { get; } = new();

    public void RequestImportPath(WaypointImportRequest request) => importRequests.Enqueue(request);

    public bool TryDequeueImport(out WaypointImportRequest request) => importRequests.TryDequeue(out request!);

    public async Task<EditablePath?> CreateNewStandalone()
    {
        // prefer the source the core actually uses for creature movement; creature_movement is
        // guid-keyed (no standalone ids), so fall back to any other id-keyed source then
        var candidates = new List<WaypointSource>();
        if (creaturePathSource is { } cps)
            candidates.Add(cps);
        candidates.AddRange(AvailableSources);

        foreach (var source in candidates)
        {
            if (source == WaypointSource.MangosCreatureMovement)
                continue;
            var ids = await EnumeratePathIds(source);
            uint key = ids.Count == 0 ? 1 : ids.Max() + 1;
            return FindLoaded(source, key) ?? NewPath(source, key, 0);
        }

        return null;
    }

    public (WaypointSource source, uint key)? ResolveCreaturePath(CreatureSpawnInstance creature)
    {
        if (attachmentProvider == null || creaturePathSource is not { } source)
            return null;

        if (sessionAttachedPaths.TryGetValue(creature.Guid, out var sessionPathId))
            return (source, sessionPathId);

        if (attachmentProvider.ResolveAttachedPathId(creature.Guid, creature.Addon, creature.MovementType) is { } pathId)
            return (source, pathId);

        return null;
    }

    public async Task<EditablePath?> AttachOrLoadCreaturePath(CreatureSpawnInstance creature)
    {
        if (attachmentProvider == null || creaturePathSource is not { } source)
        {
            Console.WriteLine("[Waypoints] no creature-path support on this core");
            return null;
        }

        uint pathId = ResolveCreaturePath(creature)?.key ?? attachmentProvider.AllocatePathId(creature.Guid);
        Console.WriteLine($"[Waypoints] attaching/loading {source.ToName()} path {pathId} for creature {creature.Guid}");
        // full editor: apply the attachment as document field updates (creature_addon columns are
        // flattened into the creature editor), so it is undoable, saved with the unified Save and
        // lands in the SESSION query. Headless: execute the SQL directly. Idempotent either way.
        if (worldEditService.IsAvailable)
            worldEditService.SetSpawnFields(true, creature.Guid, attachmentProvider.AttachFields(pathId), "Attach waypoint path");
        else
            await Execute(attachmentProvider.Attach(creature.Guid, pathId));
        sessionAttachedPaths[creature.Guid] = pathId;

        return await LoadPath(source, pathId, creature.Guid)
               ?? NewPath(source, pathId, creature.Guid);
    }

    /// <summary>Opens (or creates) the creature ENTRY's shared creature_movement_template path for
    /// editing. Unlike <see cref="AttachOrLoadCreaturePath"/> this touches no per-guid state (no
    /// MovementType/addon change) - the template is entry-shared; editing it just loads/creates its
    /// rows. Uses PathId 0 (the primary template path); other path ids are reachable via the Load
    /// picker. Null when the core has no template source.</summary>
    public async Task<EditablePath?> EditOrLoadCreatureTemplatePath(CreatureSpawnInstance creature)
    {
        if (!SupportsCreatureTemplatePaths)
            return null;
        const uint pathId = 0; // primary template path for the entry
        var source = WaypointSource.MangosCreatureMovementTemplate;
        return await LoadPath(source, creature.Entry, 0, pathId)
               ?? NewPath(source, creature.Entry, 0, pathId);
    }

    public async Task RemoveCreaturePath(CreatureSpawnInstance creature)
    {
        if (attachmentProvider == null || ResolveCreaturePath(creature) is not { } attached)
            return;

        // rewrite the path to empty (= DELETE all its rows) through the normal save pipeline, so the
        // deletion also lands in the session query
        var path = FindLoaded(attached.source, attached.key)
                   ?? await LoadPath(attached.source, attached.key, creature.Guid)
                   ?? NewPath(attached.source, attached.key, creature.Guid);
        path.Points.Clear();
        await Save(path);
        Unload(path);

        if (worldEditService.IsAvailable)
            worldEditService.SetSpawnFields(true, creature.Guid, attachmentProvider.DetachFields(), "Detach waypoint path");
        else
            await Execute(attachmentProvider.Detach(creature.Guid, attached.key));
        sessionAttachedPaths.TryRemove(creature.Guid, out _);
    }

    private async Task Execute(IQuery query)
    {
        await mainThread.Schedule(async () =>
        {
            await mySqlExecutor.ExecuteSql(query);
            return true;
        });
    }

    /// <summary>A brand new empty path, added via the pending queue (safe regardless of the calling
    /// thread, unlike <see cref="CreateNew"/> which mutates LoadedPaths directly).</summary>
    private EditablePath NewPath(WaypointSource source, uint key, uint creatureGuid, uint key2 = 0)
    {
        var path = new EditablePath(source, key, null, creatureGuid, schemaInfo, key2);
        InitPathExtras(path);
        pendingAdds.Enqueue(path);
        return path;
    }

    public IQuery? BuildSaveQuery(EditablePath path)
    {
        // path-level extras ride the same query: the master waypoint_path metadata (only while the
        // path still exists - a removal's DeleteAll drops the row and must not resurrect it) and
        // the cmangos name row (a removal always drops it)
        IWaypointPathHeader? header = path.Points.Count > 0 && path.Header is { } h ? h : null;
        IMangosWaypointsPathName? pathName = null;
        if (ColumnsFor(path.Source).HasFlagFast(WaypointColumns.PathName))
        {
            if (path.Points.Count == 0)
                pathName = new MangosPathNameAdapter(path.Key, "");
            else if (path.PathName != null)
                pathName = new MangosPathNameAdapter(path.Key, path.PathName);
        }

        return WaypointSaveSql.Build(path.Source, path.Key, path.Key2, path.Points,
            waypointDataGen, smartScriptGen, scriptWaypointGen, mangosWaypointGen, mangosMovementGen, mangosMovementTemplateGen,
            header, pathHeaderGen, pathName, pathNameGen);
    }

    public async Task Save(EditablePath path)
    {
        var query = BuildSaveQuery(path);
        if (query == null)
            throw new Exception($"No SQL provider for waypoint source {path.Source} on the current core.");

        await mainThread.Schedule(async () =>
        {
            await mySqlExecutor.ExecuteSql(query);
            return true;
        });

        // hand the just-saved path to the session (full app only; the bridge upserts the per-path
        // item — keyed by source + key — and refreshes its session query). The item is only a
        // reference - the exported query re-reads the path's rows from the DB at generate time.
        eventAggregator.GetEvent<WaypointsSavedEvent>()
            .Publish(new WaypointsSolutionItem { Source = (int)path.Source, Key = path.Key, Key2 = path.Key2 });

        // the save may have created the entry's first template rows or deleted its last ones -
        // drop the cached existence so the inspector re-checks
        if (path.Source == WaypointSource.MangosCreatureMovementTemplate)
            templatePathExists.TryRemove(path.Key, out _);

        path.ClearDirty();
    }
}

/// <summary>Loads a path's current rows as universal points, per source table. Shared by the editor,
/// the session solution item's SQL provider and its read-only document.</summary>
public static class WaypointDbLoader
{
    public static async Task<List<UniversalWaypoint>?> LoadPoints(IDatabaseProvider databaseProvider, WaypointSource source, uint key, uint key2 = 0)
    {
        switch (source)
        {
            case WaypointSource.TrinityWaypointData:
                return (await databaseProvider.GetWaypointData(key))?.Select(p => p.ToUniversal()).ToList();
            case WaypointSource.SmartScriptWaypoint:
                return (await databaseProvider.GetSmartScriptWaypoints(key))?.Select(p => p.ToUniversal()).ToList();
            case WaypointSource.ScriptWaypoint:
                return (await databaseProvider.GetScriptWaypoints(key))?.Select(p => p.ToUniversal()).ToList();
            case WaypointSource.MangosWaypointPath:
                return (await databaseProvider.GetMangosWaypoints(key))?.Select(p => p.ToUniversal()).ToList();
            case WaypointSource.MangosCreatureMovement:
                return (await databaseProvider.GetMangosCreatureMovement(key))?.Select(p => p.ToUniversal()).ToList();
            case WaypointSource.MangosCreatureMovementTemplate: // key = Entry, key2 = PathId
                return (await databaseProvider.GetMangosCreatureMovementTemplate(key, key2))?.Select(p => p.ToUniversal()).ToList();
            default:
                return null;
        }
    }
}

/// <summary>The per-source save query for a waypoint path: DELETE the whole path (key-only adapter),
/// then bulk INSERT the current points, then any path-LEVEL rows (master waypoint_path metadata,
/// cmangos waypoint_path_name). Shared by the editor's live save and the session solution item's
/// SQL provider so both always emit identical SQL.</summary>
public static class WaypointSaveSql
{
    public static IQuery? Build(WaypointSource source, uint key, uint key2, IReadOnlyList<UniversalWaypoint> points,
        IQueryGenerator<IWaypointData> waypointDataGen,
        IQueryGenerator<ISmartScriptWaypoint> smartScriptGen,
        IQueryGenerator<IScriptWaypoint> scriptWaypointGen,
        IQueryGenerator<IMangosWaypoint> mangosWaypointGen,
        IQueryGenerator<IMangosCreatureMovement> mangosMovementGen,
        IQueryGenerator<IMangosCreatureMovementTemplate> mangosMovementTemplateGen,
        IWaypointPathHeader? pathHeader = null,
        IQueryGenerator<IWaypointPathHeader>? pathHeaderGen = null,
        IMangosWaypointsPathName? pathName = null,
        IQueryGenerator<IMangosWaypointsPathName>? pathNameGen = null)
    {
        var query = source switch
        {
            WaypointSource.TrinityWaypointData => Build(waypointDataGen, key, key2, points, (w, k, _) => new WaypointDataAdapter(w, k)),
            WaypointSource.SmartScriptWaypoint => Build(smartScriptGen, key, key2, points, (w, k, _) => new SmartScriptWaypointAdapter(w, k)),
            WaypointSource.ScriptWaypoint => Build(scriptWaypointGen, key, key2, points, (w, k, _) => new ScriptWaypointAdapter(w, k)),
            WaypointSource.MangosWaypointPath => Build(mangosWaypointGen, key, key2, points, (w, k, _) => new MangosWaypointAdapter(w, k)),
            WaypointSource.MangosCreatureMovement => Build(mangosMovementGen, key, key2, points, (w, k, _) => new MangosCreatureMovementAdapter(w, k)),
            WaypointSource.MangosCreatureMovementTemplate => Build(mangosMovementTemplateGen, key, key2, points, (w, k, k2) => new MangosCreatureMovementTemplateAdapter(w, k, k2)),
            _ => null,
        };
        if (query == null)
            return null;

        IQuery? headerQuery = source == WaypointSource.TrinityWaypointData && pathHeader != null
            ? pathHeaderGen?.TryUpdate(pathHeader)
            : null;
        IQuery? nameQuery = source == WaypointSource.MangosWaypointPath && pathName != null
            ? pathNameGen?.TryUpdate(pathName)
            : null;
        if (headerQuery == null && nameQuery == null)
            return query;

        var multi = Queries.BeginTransaction(query.Database);
        multi.Add(query);
        if (headerQuery != null)
            multi.Add(headerQuery);
        if (nameQuery != null)
            multi.Add(nameQuery);
        return multi.Close();
    }

    private static IQuery? Build<T>(IQueryGenerator<T> gen, uint key, uint key2, IReadOnlyList<UniversalWaypoint> points, Func<UniversalWaypoint, uint, uint, T> makeAdapter)
    {
        // delete the whole path first (key-only adapter), then re-insert the current points. An empty
        // path means REMOVAL - prefer DeleteAll so cores with path metadata rows drop those too.
        var keyAdapter = makeAdapter(new UniversalWaypoint { PathId = key, PathId2 = key2 }, key, key2);
        var delete = points.Count == 0
            ? gen.TryDeleteAll(keyAdapter) ?? gen.TryDelete(keyAdapter)
            : gen.TryDelete(keyAdapter);
        if (delete == null)
            return null;

        IQuery? insert = null;
        if (points.Count > 0)
            insert = gen.TryBulkInsert(points.Select(p => makeAdapter(p, key, key2)).ToList());

        var multi = Queries.BeginTransaction(delete.Database);
        multi.Add(delete);
        if (insert != null)
            multi.Add(insert);
        return multi.Close();
    }
}
