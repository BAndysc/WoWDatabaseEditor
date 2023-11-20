using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Solution;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Rendering.Modules;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Rendering;

public class GenericSpawnContextMenu : ISpawnContextMenu
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly IEventAggregator eventAggregator;
    private readonly SpawnDeleter spawnDeleter;
    private readonly GenericExternalEdits externalEdits;
    private readonly SpawnGroupTool spawnGroupTool;
    private readonly PathEditor pathEditor;

    private ICommand CopyGuidCommand { get; }
    private ICommand CopyEntryCommand { get; }
    private ICommand CopyPositionCommand { get; }
    private ICommand OpenCreatureTemplate { get; }
    private ICommand OpenGameobjectTemplate { get; }
    private ICommand OpenScriptCommand { get; }
    private ICommand OpenSpawnScriptCommand { get; }
    private ICommand CreateSpawnGroupCommand { get; }
    private ICommand LeaveCurrentSpawnGroupCommand { get; }
    private ICommand EditCurrentSpawnGroupCommand { get; }
    private ICommand AssignToTheLastSpawnGroupCommand { get; }
    private ICommand DeleteSpawnCommand { get; }
    private ICommand EditWaypoints { get; }

    internal GenericSpawnContextMenu(ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService,
        ICurrentCoreVersion currentCoreVersion,
        IEventAggregator eventAggregator,
        
        SpawnDeleter spawnDeleter,
        GenericExternalEdits externalEdits,
        SpawnGroupTool spawnGroupTool,
        PathEditor pathEditor)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.currentCoreVersion = currentCoreVersion;
        this.eventAggregator = eventAggregator;
        this.spawnDeleter = spawnDeleter;
        this.externalEdits = externalEdits;
        this.spawnGroupTool = spawnGroupTool;
        this.pathEditor = pathEditor;

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
        CopyEntryCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Entry.ToString()));
        CopyPositionCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            var pos = inst.WorldObject?.Position ?? inst.Position;
            clipboardService.SetText($"{pos.X}, {pos.Y}, {pos.Z}");
        });
        OpenScriptCommand = new DelegateCommand<SpawnInstance>(externalEdits.OpenScript);
        OpenSpawnScriptCommand = new DelegateCommand<SpawnInstance>(externalEdits.OpenSpawnScript);
        OpenCreatureTemplate = new DelegateCommand<CreatureSpawnInstance>(inst => Open(new DatabaseTableSolutionItem(new DatabaseKey(inst.Entry), true, false, DatabaseTable.WorldTable("creature_template"), false)));
        OpenGameobjectTemplate = new DelegateCommand<GameObjectSpawnInstance>(inst => Open(new DatabaseTableSolutionItem(new DatabaseKey(inst.Entry), true, false, DatabaseTable.WorldTable("gameobject_template"), false)));
        CreateSpawnGroupCommand = new AsyncAutoCommand<SpawnInstance>(spawnGroupTool.CreateAndAssignSpawnGroup);
        LeaveCurrentSpawnGroupCommand = new AsyncAutoCommand<SpawnInstance>(spawnGroupTool.LeaveSpawnGroup);
        AssignToTheLastSpawnGroupCommand = new AsyncAutoCommand<SpawnInstance>(spawnGroupTool.AssignSpawnGroup, spawn => spawnGroupTool.LastSpawnGroup != null && spawn?.SpawnGroup == null);
        EditCurrentSpawnGroupCommand = new AsyncAutoCommand<SpawnInstance>(spawnGroupTool.EditSpawnGroup, spawn => spawn?.SpawnGroup != null);
        DeleteSpawnCommand = new DelegateCommand<SpawnInstance>(spawnDeleter.Delete);
        EditWaypoints = new DelegateCommand<CreatureSpawnInstance>(inst => pathEditor.Edit(inst));
    }

    private void Open(ISolutionItem item)
    {
        eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }
    
    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        if (spawn == null || !spawn.IsSpawned)
            yield break;

        var creatureSpawn = spawn as CreatureSpawnInstance;
        var gameobjectSpawn = spawn as GameObjectSpawnInstance;

        if ((creatureSpawn != null && externalEdits.CanEditCreature) ||
            gameobjectSpawn != null && externalEdits.CanEditGameObject)
        {
            yield return ("Edit script", OpenScriptCommand, spawn);
            yield return ("Edit spawn script", OpenSpawnScriptCommand, spawn);
            yield return ("-", null!, null);
        }

        if (spawn.SpawnGroup == null)
            yield return ("Create spawn group", CreateSpawnGroupCommand, spawn);
        else
        {
            yield return ($"Edit spawn group: {spawn.SpawnGroup.Name.TrimToLength(40)}", EditCurrentSpawnGroupCommand, spawn);
            yield return ($"Leave current spawn group", LeaveCurrentSpawnGroupCommand, spawn);
        }

        if (spawnGroupTool.LastSpawnGroup == null)
            yield return ("Assign to the last spawn group", AlwaysDisabledCommand.Command, null);
        else
            yield return ($"Assign to the group '{spawnGroupTool.LastSpawnGroup.Name.TrimToLength(40)}'", AssignToTheLastSpawnGroupCommand, spawn);
        
        yield return ("-", null!, null);
        
        if (creatureSpawn != null)
            yield return ("Edit template", OpenCreatureTemplate, creatureSpawn);
        else if (gameobjectSpawn != null)
            yield return ("Edit template", OpenGameobjectTemplate, gameobjectSpawn);

        if (creatureSpawn != null)
        {
            yield return ("-", null!, null);
            if (creatureSpawn.MovementType == MovementType.Idle)
                yield return ("Add waypoints", EditWaypoints, creatureSpawn);
            else
                yield return ("Edit waypoints", EditWaypoints, creatureSpawn);
        }
        
        yield return ("-", null!, null);
        yield return ("Delete spawn", DeleteSpawnCommand, spawn);
        
        yield return ("-", null!, null);

        yield return ("Copy guid", CopyGuidCommand, spawn);
        yield return ("Copy entry", CopyEntryCommand, spawn);
        yield return ("Copy position", CopyPositionCommand, spawn);
    }
}