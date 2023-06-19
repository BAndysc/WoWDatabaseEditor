using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;
using WDE.SqlQueryGenerator;
using WDE.Common.Database;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;

namespace WDE.MapSpawns.Rendering;

public class SpawnContextMenu
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMessageBoxService messageBoxService;
    private readonly IQueryGenerator<PositionAndRotationDiff> positionAndRotation;

    private ICommand CopyGuidCommand { get; }
    private ICommand CopyPositionCommand { get; }
    private ICommand CopyOrientationCommand { get; }
    private ICommand UpdateValuesCommand { get; }

    public SpawnContextMenu(
        ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService,
        IMySqlExecutor mySqlExecutor,
        IMessageBoxService messageBoxService,
        IQueryGenerator<PositionAndRotationDiff> positionAndRotation)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.mySqlExecutor = mySqlExecutor;
        this.messageBoxService = messageBoxService;
        this.positionAndRotation = positionAndRotation;

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
        CopyPositionCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText($"X: {inst.WorldObject!.Position.X} Y: {inst.WorldObject!.Position.Y} Z: {inst.WorldObject!.Position.Z}"));
        CopyOrientationCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            CreatureSpawnInstance? creature = inst as CreatureSpawnInstance;
            GameObjectSpawnInstance? go = inst as GameObjectSpawnInstance;

            if (creature != null) 
            { 
                clipboardService.SetText(creature.Creature!.Orientation.ToString()); 
            }
            else if (go != null) 
            {
                clipboardService.SetText(go.GameObject!.Orientation.ToString());
            }
        });
        UpdateValuesCommand = new DelegateCommand<SpawnInstance>(async (inst) =>
        {
            var transaction = Queries.BeginTransaction();

            CreatureSpawnInstance? creature = inst as CreatureSpawnInstance;
            GameObjectSpawnInstance? go = inst as GameObjectSpawnInstance;

            var typeString = creature != null ? "creature" : "gameobject";

            var orientation = creature != null ? creature.Creature!.Orientation : go!.GameObject!.Orientation;

            var diff = new PositionAndRotationDiff()
            {
                Guid = inst.Guid,
                Type = typeString,
                Position = inst.WorldObject!.Position,
                Orientation = orientation,
            };

            if (go != null)
            {
                diff.Rotation = go.GameObject!.Rotation;
            }

            var query = positionAndRotation.Update(diff);

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
    
    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        if (spawn == null || !spawn.IsSpawned)
            yield break;

        yield return ("Copy guid", CopyGuidCommand, spawn);
        yield return ("Copy position", CopyPositionCommand, spawn);
        yield return ("Copy orientation", CopyOrientationCommand, spawn);
        yield return ("Update values", UpdateValuesCommand, spawn);
    }
}