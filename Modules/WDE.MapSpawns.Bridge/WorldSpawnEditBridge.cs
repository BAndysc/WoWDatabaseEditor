using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics; // Rendering projects get this via lib3d.props; this project doesn't import it
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using TheMaths;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Bridge;

/// <summary>
/// The full-editor answer to the game-side <see cref="IWorldSpawnEditService"/> facade. It hosts two
/// hidden <see cref="SingleRowDbTableEditorViewModel"/> documents (creature + gameobject) purely as a
/// SQL / session / query engine, and translates the request events (spawn / delete / move / save-undo)
/// into document mutations. It keeps its own unified undo stack across both documents (structural
/// row add/remove is not tracked by the document's own History), and publishes the resulting pending
/// state back to the game via <see cref="WorldSpawnEditStateChangedEvent"/>.
///
/// NOTE on coordinates: <see cref="SpawnCreateRequest.Position"/> / <see cref="SpawnMoveRequest.Position"/>
/// are expected already in DB (server) space; converting from engine space is the game side's job (it
/// owns the coordinate utilities), the bridge only persists what it is given.
/// </summary>
[AutoRegister]
[SingleInstance]
public class WorldSpawnEditBridge
{
    private readonly IContainerProvider containerProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly ITableDefinitionProvider definitionProvider;
    private readonly IDatabaseTableModelGenerator modelGenerator;
    private readonly IIdGeneratorService idGenerator;
    private readonly ISessionService sessionService;
    private readonly IStatusBar statusBar;
    private readonly IMessageBoxService messageBoxService;
    private readonly Lazy<ITableEditorPickerService> tableEditorPicker;
    private readonly Lazy<WDE.Common.Services.QueryParser.IQueryParserService> queryParser;
    private readonly Lazy<IDatabaseTableDataProvider> tableDataProvider;
    private readonly Lazy<WDE.DatabaseEditors.Services.ITableOpenService> tableOpenService;
    private readonly WDE.Common.Tasks.IMainThread uiThread;

    private bool activated;
    private bool saveInProgress; // UI-thread only (all handlers arrive on the UI thread)
    private int revision;
    private int saveCounter;
    private IReadOnlyList<(bool isCreature, uint guid)> lastSaveDeleted = Array.Empty<(bool, uint)>();

    // per-table hosted document (key: isCreature). null value = table unsupported by the active core.
    private readonly Dictionary<bool, SingleRowDbTableEditorViewModel?> docs = new();
    private readonly Dictionary<bool, Task<SingleRowDbTableEditorViewModel?>> docLoads = new();

    // reusable synthetic (existInDatabase) entities for existing rows we delete/move but never loaded.
    private readonly Dictionary<(bool isCreature, uint guid), DatabaseEntity> syntheticRows = new();

    private readonly HashSet<(bool isCreature, uint guid)> pendingDeletes = new();
    private readonly List<PendingSpawn> newSpawns = new();

    private readonly List<EditOp> undoStack = new();
    private readonly List<EditOp> redoStack = new();

    private sealed class EditOp
    {
        public required string Description;
        public required Action Redo;
        public required Action Undo;
    }

    public WorldSpawnEditBridge(IContainerProvider containerProvider,
        IEventAggregator eventAggregator,
        ITableDefinitionProvider definitionProvider,
        IDatabaseTableModelGenerator modelGenerator,
        IIdGeneratorService idGenerator,
        ISessionService sessionService,
        IStatusBar statusBar,
        IMessageBoxService messageBoxService,
        Lazy<ITableEditorPickerService> tableEditorPicker,
        Lazy<WDE.Common.Services.QueryParser.IQueryParserService> queryParser,
        Lazy<IDatabaseTableDataProvider> tableDataProvider,
        Lazy<WDE.DatabaseEditors.Services.ITableOpenService> tableOpenService,
        WDE.Common.Tasks.IMainThread uiThread)
    {
        this.uiThread = uiThread;
        this.containerProvider = containerProvider;
        this.eventAggregator = eventAggregator;
        this.definitionProvider = definitionProvider;
        this.modelGenerator = modelGenerator;
        this.idGenerator = idGenerator;
        this.sessionService = sessionService;
        this.statusBar = statusBar;
        this.messageBoxService = messageBoxService;
        this.tableEditorPicker = tableEditorPicker;
        this.queryParser = queryParser;
        this.tableDataProvider = tableDataProvider;
        this.tableOpenService = tableOpenService;
    }

    public void Activate()
    {
        if (activated)
            return;
        activated = true;

        // all handlers arrive on the Avalonia UI thread - this is what lets us drive the document VMs
        // safely (they assume the main thread) without manual marshalling inside each mutation.
        eventAggregator.GetEvent<SpawnCreateRequestedEvent>()
            .Subscribe(r => Run(() => OnCreate(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnDuplicateRequestedEvent>()
            .Subscribe(r => Run(() => OnDuplicate(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnDeleteToggleRequestedEvent>()
            .Subscribe(r => Run(() => OnToggleDelete(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnMoveRequestedEvent>()
            .Subscribe(r => Run(() => OnMove(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnFieldsUpdateRequestedEvent>()
            .Subscribe(r => Run(() => OnFieldsUpdate(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<WorldEditCommandRequestedEvent>()
            .Subscribe(c => Run(() => OnCommand(c)), ThreadOption.UIThread, true);
        // the game client asks for the current state when it comes online (startup ordering fix)
        eventAggregator.GetEvent<WorldSpawnEditStateRequestedEvent>()
            .Subscribe(PublishState, ThreadOption.UIThread, true);
        // formation / spawn-group editors publish a delta solution item after each live save; merge it
        // into the session's cumulative item so those edits are session-first-class too
        eventAggregator.GetEvent<FormationsSavedEvent>()
            .Subscribe(d => Run(() => OnFormationsSaved(d)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnGroupsSavedEvent>()
            .Subscribe(d => Run(() => OnSpawnGroupsSaved(d)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<PoolsSavedEvent>()
            .Subscribe(d => Run(() => OnPoolsSaved(d)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<WaypointsSavedEvent>()
            .Subscribe(d => Run(() => OnWaypointsSaved(d)), ThreadOption.UIThread, true);
        // PublisherThread ON PURPOSE: Handled must be stamped synchronously during Publish so the
        // publishing editor knows to await the parse; the parse itself hops to the UI thread
        eventAggregator.GetEvent<WorldEditQuerySavingEvent>()
            .Subscribe(OnWorldEditQuerySaving, ThreadOption.PublisherThread, true);
        eventAggregator.GetEvent<OpenSpellAreaEditorEvent>()
            .Subscribe(r => Run(() => OnOpenSpellAreaEditor(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<OpenTemplateEditorEvent>()
            .Subscribe(r => Run(() => OnOpenTemplateEditor(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<OpenGossipMenuEditorEvent>()
            .Subscribe(id => Run(() => OnOpenGossipMenuEditor(id)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<WorldEditGenerateQueryRequestedEvent>()
            .Subscribe(r => Run(() => OnGenerateQuery(r)), ThreadOption.UIThread, true);

        PublishState(); // Available = true
    }

    private void Run(Func<Task> action) => action().ListenErrors();

    // --- request handlers ------------------------------------------------------------------------

    private async Task OnCreate(SpawnCreateRequest r)
    {
        USAGE.Count("3d_action", ("action", "spawn_create"));
        Console.WriteLine($"[SpawnBridge] create request: {(r.IsCreature ? "creature" : "gameobject")} {r.Entry} at {r.Position}");
        var doc = await EnsureDoc(r.IsCreature);
        if (doc == null)
        {
            Notify(NotificationType.Error, $"Can't spawn: no {(r.IsCreature ? "creature" : "gameobject")} table definition for this core");
            return;
        }

        // modal on not-configured / exhausted range (a status-bar toast is too easy to miss)
        var maybeGuid = await idGenerator.GetNextOrShowError(
            r.IsCreature ? new CreatureGuidIdType { Entry = r.Entry } : (IIdType)new GameObjectGuidIdType { Entry = r.Entry },
            messageBoxService);
        if (maybeGuid is not { } longGuid)
            return;
        var guid = (uint)longGuid;

        var definition = definitionProvider.GetDefinitionByTableName(WorldTable(r.IsCreature))!;
        // real-keyed row right away (the guid is final - it was just allocated), NOT a phantom row:
        // phantoms get re-keyed ("materialized") on save, which changes the row's identity mid-session.
        // ExistInDatabase stays false = "session-created": save INSERTs it (DELETE+INSERT, idempotent),
        // and if it is deleted later, the DELETE runs live but is omitted from the exported query.
        var entity = modelGenerator.CreateEmptyEntity(definition, new DatabaseKey((long)guid), false);
        TrySetLong(entity, "guid", guid); // both creature and gameobject spawn tables key on `guid`
        TrySetLong(entity, "id", r.Entry);
        TrySetLong(entity, "map", (uint)r.Map);
        ApplyTransform(entity, r.IsCreature, r.Position.X, r.Position.Y, r.Position.Z, r.Orientation);

        var spawn = new PendingSpawn(r.IsCreature, r.Entry, guid, r.Map, r.Position, r.Orientation);
        int index = doc.Entities.Count;

        ApplyOp(new EditOp
        {
            Description = "Spawn",
            Redo = () =>
            {
                doc.ForceInsertEntity(entity, Math.Min(index, doc.Entities.Count));
                newSpawns.Add(spawn);
            },
            Undo = () =>
            {
                if (doc.Entities.Contains(entity))
                    doc.ForceRemoveEntity(entity);
                newSpawns.Remove(spawn);
            }
        });
        Notify(NotificationType.Success, $"Spawned {(r.IsCreature ? "creature" : "gameobject")} {r.Entry} (guid {guid}) - Save to persist");
    }

    private async Task OnDuplicate(SpawnDuplicateRequest r)
    {
        USAGE.Count("3d_action", ("action", "spawn_duplicate"));
        var doc = await EnsureDoc(r.IsCreature);
        if (doc == null)
        {
            Notify(NotificationType.Error, $"Can't duplicate: no {(r.IsCreature ? "creature" : "gameobject")} table definition for this core");
            return;
        }

        var maybeGuid = await idGenerator.GetNextOrShowError(
            r.IsCreature ? new CreatureGuidIdType { Entry = r.Entry } : (IIdType)new GameObjectGuidIdType { Entry = r.Entry },
            messageBoxService);
        if (maybeGuid is not { } longGuid)
            return;
        var guid = (uint)longGuid;

        // clone the source's FULL row (respawn time, flags, flattened addon columns...) - that is the
        // whole point of Duplicate over "place the same entry". Prefer the fresh DB row; a row hosted
        // in the doc is either that same row (touched earlier) or an EMPTY synthetic, so it is only
        // the fallback for duplicating a spawn created this session (not in the DB yet).
        var table = WorldTable(r.IsCreature);
        DatabaseEntity? source = null;
        try
        {
            var data = await tableDataProvider.Value.Load(table, null, null, null, new[] { new DatabaseKey((long)r.SourceGuid) });
            source = data?.Entities.Count > 0 ? data.Entities[0] : null;
        }
        catch (Exception e)
        {
            LOG.LogError(e, "Failed to load spawn {Guid} for duplication", r.SourceGuid);
        }
        source ??= FindRowByGuid(doc, r.SourceGuid);

        DatabaseEntity entity;
        if (source != null)
            entity = source.Clone(new DatabaseKey((long)guid), existInDatabase: false); // session-created: save INSERTs it
        else
        {
            // source row unavailable - still duplicate as a plain new spawn of the same entry
            var definition = definitionProvider.GetDefinitionByTableName(table)!;
            entity = modelGenerator.CreateEmptyEntity(definition, new DatabaseKey((long)guid), false);
        }

        TrySetLong(entity, "guid", guid);
        TrySetLong(entity, "id", r.Entry);
        TrySetLong(entity, "map", (uint)r.Map);
        ApplyTransform(entity, r.IsCreature, r.Position.X, r.Position.Y, r.Position.Z, r.Orientation);

        var spawn = new PendingSpawn(r.IsCreature, r.Entry, guid, r.Map, r.Position, r.Orientation);
        int index = doc.Entities.Count;

        ApplyOp(new EditOp
        {
            Description = "Duplicate spawn",
            Redo = () =>
            {
                doc.ForceInsertEntity(entity, Math.Min(index, doc.Entities.Count));
                newSpawns.Add(spawn);
            },
            Undo = () =>
            {
                if (doc.Entities.Contains(entity))
                    doc.ForceRemoveEntity(entity);
                newSpawns.Remove(spawn);
            }
        });
        Notify(NotificationType.Success,
            $"Duplicated {(r.IsCreature ? "creature" : "gameobject")} {r.SourceGuid} -> guid {guid}{(source == null ? " (source row not found - fields not copied)" : "")} - Save to persist");
    }

    private async Task OnToggleDelete(SpawnEditRef r)
    {
        USAGE.Count("3d_action", ("action", "spawn_delete_toggle"));
        var doc = await EnsureDoc(r.IsCreature);
        if (doc == null)
            return;

        var key = (r.IsCreature, r.Guid);

        // toggling a spawn we created (still unsaved this session) removes its row entirely: the
        // export then contains nothing for it (self-created delete keys are export-omitted), while
        // the live save still runs a (harmless, idempotent) DELETE in case an earlier save inserted it.
        var newSpawn = newSpawns.FirstOrDefault(s => s.IsCreature == r.IsCreature && s.Guid == r.Guid);
        if (newSpawn.Guid == r.Guid && newSpawn.IsCreature == r.IsCreature)
        {
            var row = FindRowByGuid(doc, r.Guid);
            if (row != null)
            {
                int idx = doc.Entities.IndexOf(row);
                ApplyOp(new EditOp
                {
                    Description = "Delete new spawn",
                    Redo = () => { if (doc.Entities.Contains(row)) doc.ForceRemoveEntity(row); newSpawns.Remove(newSpawn); },
                    Undo = () => { doc.ForceInsertEntity(row, Math.Min(idx, doc.Entities.Count)); newSpawns.Add(newSpawn); }
                });
                return;
            }
        }

        // reuse the row already tracking this guid (materialized new spawn / earlier move) — a second
        // row with the same key would duplicate statements in the generated SQL
        var entity = FindRowByGuid(doc, r.Guid) ?? GetOrCreateSyntheticRow(r.IsCreature, r.Guid);
        bool wasPending = pendingDeletes.Contains(key);

        ApplyOp(new EditOp
        {
            Description = wasPending ? "Restore spawn" : "Delete spawn",
            Redo = () => SetDeleted(doc, entity, key, deleted: !wasPending),
            Undo = () => SetDeleted(doc, entity, key, deleted: wasPending)
        });
    }

    private async Task OnMove(SpawnMoveRequest r)
    {
        USAGE.Count("3d_action", ("action", "spawn_move"));
        var doc = await EnsureDoc(r.IsCreature);
        if (doc == null)
            return;

        // reuse the row already tracking this guid (session-created spawn or synthetic row from an
        // earlier move) so repeated edits coalesce into it.
        var entity = FindRowByGuid(doc, r.Guid) ?? GetOrCreateSyntheticRow(r.IsCreature, r.Guid);
        bool needsInsert = !doc.Entities.Contains(entity);

        // capture the previous transform for undo (gameobjects: the full rotation0-3 quaternion,
        // so undoing a gizmo tilt restores it exactly)
        float px = GetFloat(entity, "position_x"), py = GetFloat(entity, "position_y"),
              pz = GetFloat(entity, "position_z"), po = GetFloat(entity, "orientation");
        Quaternion? previousRotation = r.IsCreature
            ? null
            : new Quaternion(GetFloat(entity, "rotation0"), GetFloat(entity, "rotation1"),
                GetFloat(entity, "rotation2"), GetFloat(entity, "rotation3"));

        ApplyOp(new EditOp
        {
            Description = "Move spawn",
            Redo = () =>
            {
                if (needsInsert && !doc.Entities.Contains(entity))
                    doc.ForceInsertEntity(entity, doc.Entities.Count);
                ApplyTransform(entity, r.IsCreature, r.Position.X, r.Position.Y, r.Position.Z, r.Orientation, r.Rotation);
                UpdateNewSpawnTransform(r.IsCreature, r.Guid, r.Position, r.Orientation);
            },
            Undo = () =>
            {
                ApplyTransform(entity, r.IsCreature, px, py, pz, po, previousRotation);
                UpdateNewSpawnTransform(r.IsCreature, r.Guid, new Vector3(px, py, pz), po);
            }
        });
    }

    // the formation / spawn-group / waypoint editors publish KEY-ONLY solution items after each live
    // save (keyed by their natural key: leader guid / group id / source+path key). Upserting is just
    // UpdateQuery: the item's SQL provider re-reads the current DB state (which the live save just
    // wrote) and the session finds-or-adds the entry by item equality. No-ops without an active session.

    private async Task OnFormationsSaved(IReadOnlyList<FormationsSolutionItem> items)
    {
        USAGE.Count("3d_action", ("action", "formations_save"));
        foreach (var item in items)
            await sessionService.UpdateQuery((WDE.Common.ISolutionItem)item);
    }

    private async Task OnSpawnGroupsSaved(IReadOnlyList<SpawnGroupsSolutionItem> items)
    {
        USAGE.Count("3d_action", ("action", "spawn_groups_save"));
        foreach (var item in items)
            await sessionService.UpdateQuery((WDE.Common.ISolutionItem)item);
    }

    private async Task OnPoolsSaved(IReadOnlyList<PoolsSolutionItem> items)
    {
        USAGE.Count("3d_action", ("action", "pools_save"));
        foreach (var item in items)
            await sessionService.UpdateQuery((WDE.Common.ISolutionItem)item);
    }

    private async Task OnWaypointsSaved(WaypointsSolutionItem item)
    {
        USAGE.Count("3d_action", ("action", "waypoints_save"));
        await sessionService.UpdateQuery((WDE.Common.ISolutionItem)item);
    }

    /// <summary>A 3D editor with no dedicated solution item (graveyards, spell target positions)
    /// is about to run a live save. The query parser resolves the statements against the GENERIC
    /// table definitions into plain table solution items - the session tracks them exactly as if
    /// the rows had been edited in the table editors themselves. Ordering matters twice over:
    /// the parse runs BEFORE the SQL executes (DELETE detection only records rows that exist),
    /// and the session update runs AFTER (UpdateQuery re-reads the just-written DB rows).
    /// Runs on the publisher's (game) thread - only stamps Handled, then hops to the UI thread.</summary>
    private void OnWorldEditQuerySaving(WorldEditQuerySave save)
    {
        save.Handled = true;
        uiThread.Dispatch(() => ProcessWorldEditSave(save).ListenErrors());
    }

    private async Task ProcessWorldEditSave(WorldEditQuerySave save)
    {
        IList<WDE.Common.ISolutionItem> items = Array.Empty<WDE.Common.ISolutionItem>();
        try
        {
            IList<string> errors;
            (items, errors) = await queryParser.Value.GenerateItemsForQuery(save.Query);
            foreach (var error in errors)
                statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, $"3D edit session tracking: {error}"));
        }
        finally
        {
            save.NotifyParsed(); // never leave the awaiting editor hanging, even on a parse error
        }

        try
        {
            await save.Executed;
        }
        catch (OperationCanceledException)
        {
            return; // the save failed - nothing was written, nothing to record
        }

        foreach (var item in items)
            await sessionService.UpdateQuery(item);
    }

    /// <summary>Opens the generic spell_area table editor filtered to the area under the 3D camera
    /// and its parent zone (the core matches spell_area.area on either).</summary>
    private async Task OnOpenSpellAreaEditor(OpenSpellAreaEditorRequest r)
    {
        string condition = r.ZoneId > 0 && r.ZoneId != r.AreaId
            ? $"`area` IN ({r.AreaId}, {r.ZoneId})"
            : $"`area` = {r.AreaId}";
        await tableEditorPicker.Value.ShowTable(DatabaseTable.WorldTable("spell_area"), condition);
    }

    /// <summary>Opens the creature_template / gameobject_template editor for the entry as a new
    /// document (the definitions differ per core - resolved by table name).</summary>
    private async Task OnOpenTemplateEditor(OpenTemplateEditorRequest r)
    {
        var table = DatabaseTable.WorldTable(r.IsCreature ? "creature_template" : "gameobject_template");
        var definition = definitionProvider.GetDefinitionByTableName(table);
        if (definition == null)
        {
            statusBar.PublishNotification(new PlainNotification(NotificationType.Warning,
                $"No {table.Table} editor definition for the current core"));
            return;
        }
        var item = await tableOpenService.Value.Create(definition, new DatabaseKey(r.Entry));
        if (item != null)
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }

    private async Task OnOpenGossipMenuEditor(uint menuId)
    {
        await tableEditorPicker.Value.ShowTable(DatabaseTable.WorldTable("gossip_menu"), null, new DatabaseKey(menuId));
    }

    /// <summary>Sets arbitrary row fields (plain or foreign-table-flattened columns like
    /// "creature_addon.path_id") on the spawn's document row — same coalescing row reuse as moves,
    /// so it exports as one UPDATE (or folds into the INSERT of a session-created spawn).</summary>
    private async Task OnFieldsUpdate(SpawnFieldsUpdateRequest r)
    {
        USAGE.Count("3d_action", ("action", "spawn_fields_update"));
        var doc = await EnsureDoc(r.IsCreature);
        if (doc == null)
            return;

        var entity = FindRowByGuid(doc, r.Guid) ?? GetOrCreateSyntheticRow(r.IsCreature, r.Guid);
        bool needsInsert = !doc.Entities.Contains(entity);

        var previous = r.Fields.Select(f => (f.Column, Value: GetLong(entity, f.Column))).ToList();

        ApplyOp(new EditOp
        {
            Description = r.Description,
            Redo = () =>
            {
                if (needsInsert && !doc.Entities.Contains(entity))
                    doc.ForceInsertEntity(entity, doc.Entities.Count);
                foreach (var (column, value) in r.Fields)
                    TrySetLong(entity, column, value);
            },
            Undo = () =>
            {
                foreach (var (column, value) in previous)
                    TrySetLong(entity, column, value);
            }
        });
    }

    /// <summary>Answers the game's "what SQL would Save execute" request from the hosted documents
    /// (the same GenerateSaveQuery the documents' Save runs) - build only, nothing executes.</summary>
    private async Task OnGenerateQuery(WorldEditGenerateQueryRequest request)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (_, doc) in docs)
            {
                if (doc == null)
                    continue;
                var query = await doc.GenerateSaveQuery();
                if (!string.IsNullOrWhiteSpace(query.QueryString))
                    sb.AppendLine(query.QueryString.Trim());
            }
            request.Result.TrySetResult(sb.Length == 0 ? null : sb.ToString());
        }
        catch (Exception e)
        {
            request.Result.TrySetException(e);
        }
    }

    private Task OnCommand(WorldEditCommand command)
    {
        switch (command)
        {
            case WorldEditCommand.Save: return SaveAll();
            case WorldEditCommand.Undo: Undo(); break;
            case WorldEditCommand.Redo: Redo(); break;
        }
        return Task.CompletedTask;
    }

    // --- delete / move primitives ----------------------------------------------------------------

    // deleted=true: ensure the guid is queued for DELETE (removedKeys) via ForceRemoveEntity of a
    // non-phantom row; deleted=false: re-insert the row (which clears removedKeys) leaving it inert.
    private void SetDeleted(SingleRowDbTableEditorViewModel doc, DatabaseEntity entity, (bool, uint) key, bool deleted)
    {
        if (deleted)
        {
            if (!doc.Entities.Contains(entity))
                doc.ForceInsertEntity(entity, doc.Entities.Count);
            doc.ForceRemoveEntity(entity); // -> removedKeys.Add(guid) -> DELETE on save
            pendingDeletes.Add(key);
        }
        else
        {
            if (!doc.Entities.Contains(entity))
                doc.ForceInsertEntity(entity, doc.Entities.Count); // -> removedKeys.Remove(guid)
            pendingDeletes.Remove(key);
        }
    }

    private static void ApplyTransform(DatabaseEntity entity, bool isCreature, float x, float y, float z, float o,
        Quaternion? rotation = null)
    {
        TrySetFloat(entity, "position_x", x);
        TrySetFloat(entity, "position_y", y);
        TrySetFloat(entity, "position_z", z);
        TrySetFloat(entity, "orientation", o);
        if (!isCreature)
        {
            if (rotation is { } q)
            {
                // the full quaternion is known (gizmo rotation / undo restore) - persist it verbatim,
                // including any tilt
                TrySetFloat(entity, "rotation0", q.X);
                TrySetFloat(entity, "rotation1", q.Y);
                TrySetFloat(entity, "rotation2", q.Z);
                TrySetFloat(entity, "rotation3", q.W);
            }
            else
            {
                // only yaw known (placement drag) - store it as a quaternion z/w pair
                TrySetFloat(entity, "rotation0", 0);
                TrySetFloat(entity, "rotation1", 0);
                TrySetFloat(entity, "rotation2", MathF.Sin(o / 2));
                TrySetFloat(entity, "rotation3", MathF.Cos(o / 2));
            }
        }
    }

    /// <summary>Keeps the published NewSpawns snapshot in sync when a spawn created this session is moved,
    /// so the game (re)creates its live instance at the current spot, not the original placement.</summary>
    private void UpdateNewSpawnTransform(bool isCreature, uint guid, Vector3 position, float orientation)
    {
        var i = newSpawns.FindIndex(s => s.IsCreature == isCreature && s.Guid == guid);
        if (i >= 0)
            newSpawns[i] = newSpawns[i] with { Position = position, Orientation = orientation };
    }

    // --- undo / redo / save ----------------------------------------------------------------------

    private void ApplyOp(EditOp op)
    {
        op.Redo();
        undoStack.Add(op);
        redoStack.Clear();
        PublishState();
    }

    private void Undo()
    {
        if (undoStack.Count == 0)
            return;
        var op = undoStack[^1];
        undoStack.RemoveAt(undoStack.Count - 1);
        op.Undo();
        redoStack.Add(op);
        PublishState();
    }

    private void Redo()
    {
        if (redoStack.Count == 0)
            return;
        var op = redoStack[^1];
        redoStack.RemoveAt(redoStack.Count - 1);
        op.Redo();
        undoStack.Add(op);
        PublishState();
    }

    private async Task SaveAll()
    {
        // re-entrancy guard: the game-side Save button can't await this (fire-and-forget event),
        // so rapid clicks would otherwise interleave two saves over the same documents
        if (saveInProgress)
            return;
        saveInProgress = true;
        try
        {
            await SaveAllCore();
        }
        finally
        {
            saveInProgress = false;
        }
    }

    private async Task SaveAllCore()
    {
        // save each document independently - one failing must not silently abort the other,
        // and the failure has to reach the user (in-view toast + status bar), not just the log
        List<string>? failures = null;
        foreach (var (isCreature, doc) in docs)
        {
            if (doc == null)
                continue;
            try
            {
                await doc.Save.ExecuteAsync(); // runs the live INSERT/DELETE/UPDATE SQL
                // register the change with the active session (no-op if none) - this is what makes spawns
                // first-class session citizens: a self-created-then-deleted spawn nets to nothing here.
                await sessionService.UpdateQuery(doc);
            }
            catch (Exception e)
            {
                LOG.LogError(e, "Failed to save {Kind} spawns", isCreature ? "creature" : "gameobject");
                (failures ??= new()).Add($"{(isCreature ? "creature" : "gameobject")} spawns: {e.Message}");
            }
        }

        if (failures != null)
        {
            // keep ALL pending state (even the successfully saved doc's - a retry Save is
            // idempotent DELETE+INSERT per row) so the user can fix the cause and just Save again
            Notify(NotificationType.Error, "Failed to save: " + string.Join("\n", failures));
            return;
        }

        // saved rows are materialized inside the document; our captured entity/op references become
        // stale, so the pending state resets. The game reads SaveCounter + LastSaveDeleted to remove
        // the committed-delete instances from the world; live instances of saved new spawns stay.
        lastSaveDeleted = pendingDeletes.ToArray();
        saveCounter++;
        undoStack.Clear();
        redoStack.Clear();
        pendingDeletes.Clear();
        syntheticRows.Clear();
        newSpawns.Clear();
        PublishState();
        Notify(NotificationType.Success, "Spawn changes saved to database");
    }

    private void Notify(NotificationType type, string message)
    {
        statusBar.PublishNotification(new PlainNotification(type, message));
        // mirror into the 3D view as a toast - while world editing the status bar goes unnoticed
        eventAggregator.GetEvent<WorldSpawnEditNotificationEvent>()
            .Publish(new WorldSpawnEditNotification(type != NotificationType.Error, message));
    }

    // --- document hosting ------------------------------------------------------------------------

    private Task<SingleRowDbTableEditorViewModel?> EnsureDoc(bool isCreature)
    {
        if (docs.TryGetValue(isCreature, out var existing))
            return Task.FromResult(existing);
        if (docLoads.TryGetValue(isCreature, out var pending))
            return pending;

        var task = LoadDoc(isCreature);
        docLoads[isCreature] = task;
        return task;
    }

    private async Task<SingleRowDbTableEditorViewModel?> LoadDoc(bool isCreature)
    {
        var table = WorldTable(isCreature);
        var definition = definitionProvider.GetDefinitionByTableName(table);
        SingleRowDbTableEditorViewModel? doc = null;
        if (definition != null)
        {
            var solutionItem = new DatabaseTableSolutionItem(table, definition.IgnoreEquality);
            doc = containerProvider.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), solutionItem));

            // keep the hidden document empty: match nothing, so its Entities only ever holds the rows
            // we add for pending edits (the ctor otherwise pages in the first rows of the table).
            doc.FilterViewModel.FilterText = "0 = 1";
            doc.FilterViewModel.SelectedColumn = doc.FilterViewModel.RawSqlColumn;
            await doc.FilterViewModel.ApplyFilter.ExecuteAsync();
        }

        docs[isCreature] = doc;
        docLoads.Remove(isCreature);
        return doc;
    }

    // --- state -----------------------------------------------------------------------------------

    private void PublishState()
    {
        revision++;
        eventAggregator.GetEvent<WorldSpawnEditStateChangedEvent>().Publish(new WorldSpawnEditState
        {
            Available = true,
            PendingDeletes = pendingDeletes.ToImmutableHashSet(),
            NewSpawns = newSpawns.ToArray(),
            HasChanges = undoStack.Count > 0,
            CanUndo = undoStack.Count > 0,
            CanRedo = redoStack.Count > 0,
            NextUndo = undoStack.Count > 0 ? undoStack[^1].Description : null,
            NextRedo = redoStack.Count > 0 ? redoStack[^1].Description : null,
            Revision = revision,
            SaveCounter = saveCounter,
            LastSaveDeleted = lastSaveDeleted
        });
    }

    // --- helpers ---------------------------------------------------------------------------------

    private static DatabaseTable WorldTable(bool isCreature) =>
        DatabaseTable.WorldTable(isCreature ? "creature" : "gameobject");

    private DatabaseEntity GetOrCreateSyntheticRow(bool isCreature, uint guid)
    {
        var key = (isCreature, guid);
        if (syntheticRows.TryGetValue(key, out var e))
            return e;
        var definition = definitionProvider.GetDefinitionByTableName(WorldTable(isCreature))!;
        var entity = modelGenerator.CreateEmptyEntity(definition, new DatabaseKey((long)guid), false);
        entity.ExistInDatabase = true; // an existing DB row we are about to delete / update
        syntheticRows[key] = entity;
        return entity;
    }

    /// <summary>The document row currently tracking this guid (session-created spawn or synthetic row
    /// for a touched existing spawn — all rows carry their real key from the start). Reusing this row
    /// for every subsequent edit is what makes edits COALESCE into a single INSERT (latest values) or
    /// a single UPDATE (all modified fields) per guid — creating a second row for the same guid makes
    /// the generated SQL contain duplicated statements.</summary>
    private static DatabaseEntity? FindRowByGuid(SingleRowDbTableEditorViewModel doc, uint guid)
    {
        var key = new DatabaseKey((long)guid);
        foreach (var e in doc.Entities)
        {
            if (e.Key == key)
                return e;
        }
        return null;
    }

    private static void TrySetLong(DatabaseEntity entity, string column, long value)
    {
        try { entity.SetTypedCellOrThrow(column, value); }
        catch { /* column not present in this core's definition - ignore */ }
    }

    private static void TrySetFloat(DatabaseEntity entity, string column, float value)
    {
        try { entity.SetTypedCellOrThrow(column, value); }
        catch { /* optional column */ }
    }

    private static long GetLong(DatabaseEntity entity, string column)
    {
        var cell = entity.GetCell(ColumnFullName.Parse(column));
        return cell is DatabaseField<long> f ? f.Current.Value : 0;
    }

    private static float GetFloat(DatabaseEntity entity, string column)
    {
        var cell = entity.GetCell(ColumnFullName.Parse(column));
        return cell is DatabaseField<float> f ? f.Current.Value : 0f;
    }
}
