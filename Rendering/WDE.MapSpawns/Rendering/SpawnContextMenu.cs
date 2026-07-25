using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using TheMaths;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.ViewModels;
using WDE.SqlQueryGenerator;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;

namespace WDE.MapSpawns.Rendering;

public class SpawnContextMenu
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMessageBoxService messageBoxService;
    private readonly IWaypointEditorService waypointService;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IWorldSpawnEditService editService;
    private readonly IQueryGenerator<CreatureDiff> creatureQueryGenerator;
    private readonly IQueryGenerator<GameObjectDiff> gameObjectQueryGenerator;

    private ICommand CopyGuidCommand { get; }
    private ICommand CopyEntryCommand { get; }
    private ICommand CopyPositionCommand { get; }
    private ICommand CopyOrientationCommand { get; }
    private ICommand UpdateValuesCommand { get; }
    private ICommand EditWaypointsCommand { get; }
    private ICommand EditTemplateWaypointsCommand { get; }
    private ICommand RemoveWaypointsCommand { get; }

    /// <summary>Opens the row 1:1 table editor for the spawn (public: the double-click path uses it too).</summary>
    public ICommand EditRowCommand { get; }

    /// <summary>Opens the creature_template/gameobject_template 1:1 editor for the spawn's entry.</summary>
    private ICommand EditTemplateCommand { get; }

    // spawns whose editor was closed - SpawnViewer reloads them from the DB on the engine thread
    private readonly ConcurrentQueue<SpawnInstance> reloadRequests = new();

    /// <summary>Dequeues one spawn whose table editor was closed and which therefore needs a DB reload.</summary>
    public bool TryDequeueReload(out SpawnInstance spawn) => reloadRequests.TryDequeue(out spawn!);

    // duplicate requests from the (UI-thread) context menu - SpawnViewer starts the phantom
    // placement on the engine thread, which owns the placement controller
    private readonly ConcurrentQueue<SpawnInstance> duplicateRequests = new();

    /// <summary>Dequeues one spawn the user asked to duplicate (cursor placement of a full row clone).</summary>
    public bool TryDequeueDuplicate(out SpawnInstance spawn) => duplicateRequests.TryDequeue(out spawn!);

    private ICommand DuplicateCommand { get; }

    // The copy/update commands run on the UI thread, but spawn transforms (Vector3/Quaternion) are
    // owned and written by the engine thread and their reads tear; also the WorldObject can be
    // disposed (map change) while the menu is open. So the engine thread publishes an immutable
    // snapshot of the selected spawn's transform every frame and the commands only read that.
    private sealed record SpawnTransformSnapshot(Vector3 Position, float Orientation, Quaternion Rotation);
    private volatile SpawnTransformSnapshot? selectedSpawnTransform;

    /// <summary>Call every frame on the engine thread with the currently selected spawn.</summary>
    public void PublishSelectedSpawnTransform(SpawnInstance? spawn)
    {
        if (spawn == null || !spawn.IsSpawned || spawn.WorldObject == null)
        {
            selectedSpawnTransform = null;
            return;
        }

        var position = spawn.WorldObject.Position;
        float orientation = spawn switch
        {
            CreatureSpawnInstance c when c.Creature != null => c.Creature.Orientation,
            GameObjectSpawnInstance g when g.GameObject != null => g.GameObject.Orientation,
            _ => 0
        };
        var rotation = spawn is GameObjectSpawnInstance { GameObject: { } gameObject } ? gameObject.Rotation : Quaternion.Identity;

        var current = selectedSpawnTransform;
        if (current == null || current.Position != position || current.Orientation != orientation || current.Rotation != rotation)
            selectedSpawnTransform = new SpawnTransformSnapshot(position, orientation, rotation);
    }

    public SpawnContextMenu(
        ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService,
        IMySqlExecutor mySqlExecutor,
        IMessageBoxService messageBoxService,
        IWaypointEditorService waypointService,
        ITableEditorPickerService tableEditorPickerService,
        IWorldSpawnEditService editService,
        IQueryGenerator<CreatureDiff> creatureQueryGenerator,
        IQueryGenerator<GameObjectDiff> gameObjectQueryGenerator)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.mySqlExecutor = mySqlExecutor;
        this.messageBoxService = messageBoxService;
        this.waypointService = waypointService;
        this.tableEditorPickerService = tableEditorPickerService;
        this.editService = editService;
        this.creatureQueryGenerator = creatureQueryGenerator;
        this.gameObjectQueryGenerator = gameObjectQueryGenerator;

        EditRowCommand = new AsyncAutoCommand<SpawnInstance>(EditRow);

        DuplicateCommand = new DelegateCommand<SpawnInstance>(inst => duplicateRequests.Enqueue(inst!));

        // executed on the UI thread - only ENQUEUE; the waypoint editor module runs the actual
        // load/attach/remove on the engine thread and opens the editor
        EditWaypointsCommand = new DelegateCommand<CreatureSpawnInstance>(creature =>
        {
            Console.WriteLine($"[Waypoints] add/edit requested for creature {creature!.Guid}");
            waypointService.RequestEditCreaturePath(creature);
        });
        EditTemplateWaypointsCommand = new DelegateCommand<CreatureSpawnInstance>(creature =>
            waypointService.RequestEditCreatureTemplatePath(creature!));
        RemoveWaypointsCommand = new AsyncAutoCommand<CreatureSpawnInstance>(async creature =>
        {
            var confirm = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Warning)
                .SetTitle("Remove all waypoints")
                .SetMainInstruction($"Remove all waypoints of creature {creature.Guid}?")
                .SetContent("Deletes the whole attached waypoint path and sets the creature's movement type to Idle.")
                .WithYesButton(true)
                .WithNoButton(false)
                .Build());
            if (confirm)
                waypointService.RequestRemoveCreaturePath(creature);
        });

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
        CopyEntryCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Entry.ToString()));
        EditTemplateCommand = new AsyncAutoCommand<SpawnInstance>(async spawn =>
        {
            var table = DatabaseTable.WorldTable(spawn is CreatureSpawnInstance ? "creature_template" : "gameobject_template");
            await tableEditorPickerService.ShowForeignKey1To1(table, new DatabaseKey(spawn.Entry));
        });
        CopyPositionCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            if (selectedSpawnTransform is { } transform)
                clipboardService.SetText($"X: {transform.Position.X} Y: {transform.Position.Y} Z: {transform.Position.Z}");
        });
        CopyOrientationCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            if (selectedSpawnTransform is { } transform)
                clipboardService.SetText(transform.Orientation.ToString(CultureInfo.InvariantCulture));
        });
        UpdateValuesCommand = new AsyncAutoCommand<SpawnInstance>(async (inst) =>
        {
            IQuery? query = null;

            if (selectedSpawnTransform is not { } transform)
                return;

            if (inst is CreatureSpawnInstance creature)
            {
                var diff = new CreatureDiff()
                {
                    Guid = inst.Guid,
                    Entry = inst.Entry,
                    Position = transform.Position,
                    Orientation = transform.Orientation
                };
                query = creatureQueryGenerator.Update(diff);
            }
            else if (inst is GameObjectSpawnInstance go)
            {
                var diff = new GameObjectDiff()
                {
                    Guid = inst.Guid,
                    Entry = inst.Entry,
                    Position = transform.Position,
                    Orientation = transform.Orientation,
                    Rotation = transform.Rotation
                };
                query = gameObjectQueryGenerator.Update(diff);
            }

            if (query == null)
                return;
            
            try
            {
                await mySqlExecutor.ExecuteSql(query!);
            }
            catch (Exception e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Error while saving")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        });
    }
    
    /// <summary>Opens the 1:1 row editor for the spawn's creature/gameobject table row, AWAITS it, and
    /// then requests a DB reload of this single spawn (the row may have been changed - or deleted - in
    /// the editor). Pending world edits must be saved first (the editor shows the database state and
    /// the post-close reload would clobber unsaved changes), so the user is asked to save.</summary>
    private async Task EditRow(SpawnInstance spawn)
    {
        if (editService.IsAvailable && editService.HasChanges)
        {
            var save = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Warning)
                .SetTitle("Unsaved changes")
                .SetMainInstruction("Save pending spawn changes?")
                .SetContent("The table editor shows the database state and this spawn is reloaded from the database after the editor closes. Unsaved changes must be saved to open it.")
                .WithYesButton(true)
                .WithCancelButton(false)
                .Build());
            if (!save || !await SaveAndWait())
                return;
        }

        var table = DatabaseTable.WorldTable(spawn is CreatureSpawnInstance ? "creature" : "gameobject");
        await tableEditorPickerService.ShowForeignKey1To1(table, new DatabaseKey(spawn.Guid));

        reloadRequests.Enqueue(spawn); // SpawnViewer picks it up on the engine thread
    }

    /// <summary>The bridge save is event-driven (no completion task) - Save() and wait for the
    /// published SaveCounter to advance.</summary>
    private async Task<bool> SaveAndWait()
    {
        int counterBefore = editService.SaveCounter;
        editService.Save();
        for (int i = 0; i < 100; ++i)
        {
            if (editService.SaveCounter != counterBefore)
                return true;
            await Task.Delay(100);
        }
        return false; // save didn't complete in ~10s - don't open the editor on unsaved state
    }

    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        if (spawn == null || !spawn.IsSpawned)
            yield break;

        yield return (spawn is CreatureSpawnInstance ? "Edit creature" : "Edit gameobject", EditRowCommand, spawn);
        yield return ("Edit template", EditTemplateCommand, spawn);
        if (editService.IsAvailable)
            yield return ("Duplicate (Ctrl+D)", DuplicateCommand, spawn);
        yield return ("Copy guid", CopyGuidCommand, spawn);
        yield return ("Copy entry", CopyEntryCommand, spawn);
        yield return ("Copy position", CopyPositionCommand, spawn);
        yield return ("Copy orientation", CopyOrientationCommand, spawn);
        yield return ("Update values", UpdateValuesCommand, spawn);

        if (spawn is CreatureSpawnInstance creature)
        {
            if (waypointService.SupportsCreaturePaths)
            {
                // trinity: attached = addon path id exists; mangos: attached = the creature actually
                // moves on waypoints (the guid-keyed table alone says nothing - empty paths have no rows)
                bool attached = waypointService.ResolveCreaturePath(creature) != null;
                yield return (attached ? "Edit waypoints" : "Add waypoints", EditWaypointsCommand, creature);
                if (attached)
                    yield return ("Remove all waypoints", RemoveWaypointsCommand, creature);
            }
            // the entry-shared creature_movement_template path (CMaNGOS) - a separate editing
            // target; "Edit" only when the entry really has rows (null = check in flight - say
            // "Add", the action is create-or-edit either way)
            if (waypointService.SupportsCreatureTemplatePaths)
            {
                bool hasTemplate = waypointService.HasCreatureTemplatePath(creature.Entry) == true;
                yield return (hasTemplate ? "Edit template waypoints" : "Add template waypoints", EditTemplateWaypointsCommand, creature);
            }
        }
    }
}