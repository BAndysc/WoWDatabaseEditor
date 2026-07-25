using System.Collections.ObjectModel;
using TheMaths;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Waypoints;

/// <summary>What a queued creature waypoint action does: edit/attach the per-guid path, remove it, or
/// edit the entry-shared creature_movement_template path.</summary>
public enum CreaturePathAction
{
    Edit,
    Remove,
    EditTemplate,
}

[UniqueProvider]
public interface IWaypointEditorService
{
    /// <summary>In-memory paths: the one currently edited path plus any earlier-edited paths that
    /// still have unsaved changes (staged/pending). NOT a user-browsable "loaded paths" list - the
    /// editor shows/edits exactly one path at a time (see <see cref="SelectedPath"/>); staged paths
    /// only linger so their pending edits survive and are written by the unified Save.</summary>
    ObservableCollection<EditablePath> LoadedPaths { get; }

    /// <summary>The one path currently open for editing: its points are clickable/grabbable and the
    /// panel targets it (null = none). Opening another path stages the outgoing one when it has
    /// unsaved edits, or drops it when clean. Selecting a path does NOT arm point-adding - that's
    /// <see cref="EditingPath"/>.</summary>
    EditablePath? SelectedPath { get; set; }

    /// <summary>The path in EDIT (pen) mode, or null: terrain clicks append to it and segment clicks
    /// insert into it. Setting a non-null path also selects it; selecting a different path or
    /// unloading it drops edit mode. This is the explicit gate that separates "viewing a path"
    /// from "laying points" so stray world clicks never add points.</summary>
    EditablePath? EditingPath { get; set; }

    /// <summary>The selected point index within <see cref="SelectedPath"/> (-1 if none).</summary>
    int SelectedPointIndex { get; set; }

    /// <summary>Sources the active core actually supports.</summary>
    IReadOnlyList<WaypointSource> AvailableSources { get; }

    EditablePath? FindLoaded(WaypointSource source, uint key, uint key2 = 0);

    /// <summary>Merges off-thread loaded paths into <see cref="LoadedPaths"/>. Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();

    /// <summary>Loads (or returns the already-loaded) path. Returns null if nothing could be loaded.
    /// <paramref name="key2"/> is the secondary key for compound-keyed sources (the PathId of
    /// creature_movement_template); 0 for single-key sources.</summary>
    Task<EditablePath?> LoadPath(WaypointSource source, uint key, uint autoLoadedFromCreatureGuid = 0, uint key2 = 0);

    /// <summary>Adds a brand new empty path to edit.</summary>
    EditablePath CreateNew(WaypointSource source, uint key, uint key2 = 0);

    void Unload(EditablePath path);

    /// <summary>Which optional columns the source's table actually has on the active core - the
    /// panel only offers fields that really save (velocity is Cata+, move_type is per-point on
    /// Wrath but path-level on master, script_waypoint has no orientation, ...).</summary>
    WDE.QueryGenerators.Base.WaypointColumns ColumnsFor(WaypointSource source);

    /// <summary>Distinct existing path ids (or creature guids) for the source, for the load picker.</summary>
    Task<IReadOnlyList<uint>> EnumeratePathIds(WaypointSource source);

    /// <summary>Distinct existing (key, key2) pairs for a compound-keyed source (the (Entry, PathId)
    /// pairs of creature_movement_template), for the load picker. Empty for single-key sources.</summary>
    Task<IReadOnlyList<(uint key, uint key2)>> EnumerateCompoundKeys(WaypointSource source);

    /// <summary>Loads a path's points from the DB as plain positions for a read-only overlay (the
    /// selected creature's route preview), without touching any editor state. Null if empty/missing.</summary>
    Task<IReadOnlyList<Vector3>?> LoadPointsPreview(WaypointSource source, uint key, uint key2 = 0);

    /// <summary>Builds the DELETE-whole-path + bulk INSERT query for the path (null if unsupported).</summary>
    IQuery? BuildSaveQuery(EditablePath path);

    /// <summary>Executes the save query on the main thread and clears the dirty flag on success.</summary>
    Task Save(EditablePath path);

    /// <summary>Any loaded path with unsaved changes (drives the unified toolbar Save).</summary>
    bool AnyDirty { get; }

    /// <summary>Saves every dirty loaded path.</summary>
    Task SaveAllDirty();

    // --- creature-attached paths (the second workflow: no manual id picking) ---------------------

    /// <summary>True when the active core supports creature-attached paths (Trinity: creature_addon /
    /// creature_template_addon path id → waypoint_data; CMaNGOS: creature_movement keyed by guid).</summary>
    bool SupportsCreaturePaths { get; }

    /// <summary>True when the active core supports the entry-shared creature_movement_template path
    /// (CMaNGOS) - a separate editing target from the per-guid <see cref="SupportsCreaturePaths"/>.</summary>
    bool SupportsCreatureTemplatePaths { get; }

    /// <summary>The path attached to this creature, or null. Trinity: the addon path id (including
    /// paths attached earlier this session); CMaNGOS: (guid, creature_movement) when the creature's
    /// MovementType is a waypoint type — idle creatures read as UNATTACHED even though the guid-keyed
    /// table technically always "exists" (empty). Safe to call from any thread.</summary>
    (WaypointSource source, uint key)? ResolveCreaturePath(CreatureSpawnInstance creature);

    /// <summary>Loads the creature's attached path for editing, creating + attaching one when none
    /// exists (Trinity: allocates path id guid*10, writes creature_addon.path_id and MovementType=2;
    /// CMaNGOS: sets MovementType=2). Idempotent — calling it for an already attached creature just
    /// loads the path. Null when the core has no creature paths. Engine thread only.</summary>
    Task<EditablePath?> AttachOrLoadCreaturePath(CreatureSpawnInstance creature);

    /// <summary>Deletes all waypoints of the creature's attached path (through the normal save
    /// pipeline, so it lands in the session) and detaches it (MovementType=0, Trinity also
    /// creature_addon.path_id=0). Engine thread only.</summary>
    Task RemoveCreaturePath(CreatureSpawnInstance creature);

    /// <summary>Opens (or creates) the creature ENTRY's shared creature_movement_template path (PathId
    /// 0) for editing, without touching any per-guid state. Null when the core has no template source.
    /// Engine thread only.</summary>
    Task<EditablePath?> EditOrLoadCreatureTemplatePath(CreatureSpawnInstance creature);

    // UI-thread surfaces (the spawn context menu) don't touch editor state directly - they enqueue
    // requests which the waypoint editor module consumes on the engine thread (it also opens the
    // editor window, selects the path and activates the Waypoint tool).

    /// <summary>Queue an "add / edit waypoints" action for the creature. Safe from any thread.</summary>
    void RequestEditCreaturePath(CreatureSpawnInstance creature);

    /// <summary>Queue a "remove all waypoints" action for the creature. Safe from any thread.</summary>
    void RequestRemoveCreaturePath(CreatureSpawnInstance creature);

    /// <summary>Queue an "edit template waypoints" action for the creature's entry. Safe from any thread.</summary>
    void RequestEditCreatureTemplatePath(CreatureSpawnInstance creature);

    /// <summary>Dequeues one queued creature action (module-side, engine thread).</summary>
    bool TryDequeueCreatureAction(out CreatureSpawnInstance creature, out CreaturePathAction action);

    // --- external path import (e.g. sniffed movement) --------------------------------------------

    /// <summary>Queue an external path for import into the editor. Safe from any thread; the module
    /// executes it on the engine thread (attach-or-standalone, select, activate the Waypoint tool).</summary>
    void RequestImportPath(WaypointImportRequest request);

    /// <summary>Dequeues one queued import (module-side, engine thread).</summary>
    bool TryDequeueImport(out WaypointImportRequest request);

    /// <summary>Creates a brand new standalone path with a freshly allocated id in the core's
    /// preferred path source (added via the pending queue). Null when no id-keyed source exists.</summary>
    Task<EditablePath?> CreateNewStandalone();

    /// <summary>Read-only path overlays (sniffed movement previews, ...) drawn by the render stage
    /// but not editable/pickable. Owned by whoever displays them; engine thread only.</summary>
    List<WaypointPreviewPath> PreviewPaths { get; }
}
