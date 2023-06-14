using System;
using System.Globalization;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.MapSpawns.Models;
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
    private readonly IQueryGenerator<CreatureDiff> creatureQueryGenerator;
    private readonly IQueryGenerator<GameObjectDiff> gameObjectQueryGenerator;

    private ICommand CopyGuidCommand { get; }
    private ICommand CopyPositionCommand { get; }
    private ICommand CopyOrientationCommand { get; }
    private ICommand UpdateValuesCommand { get; }

    public SpawnContextMenu(
        ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService,
        IMySqlExecutor mySqlExecutor,
        IMessageBoxService messageBoxService,
        IQueryGenerator<CreatureDiff> creatureQueryGenerator,
        IQueryGenerator<GameObjectDiff> gameObjectQueryGenerator)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.mySqlExecutor = mySqlExecutor;
        this.messageBoxService = messageBoxService;
        this.creatureQueryGenerator = creatureQueryGenerator;
        this.gameObjectQueryGenerator = gameObjectQueryGenerator;

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
        CopyPositionCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText($"X: {inst.WorldObject!.Position.X} Y: {inst.WorldObject!.Position.Y} Z: {inst.WorldObject!.Position.Z}"));
        CopyOrientationCommand = new DelegateCommand<SpawnInstance>(inst =>
        {
            if (inst is CreatureSpawnInstance creature) 
            { 
                clipboardService.SetText(creature.Creature!.Orientation.ToString(CultureInfo.InvariantCulture)); 
            }
            else if (inst is GameObjectSpawnInstance go) 
            {
                clipboardService.SetText(go.GameObject!.Orientation.ToString(CultureInfo.InvariantCulture));
            }
        });
        UpdateValuesCommand = new AsyncAutoCommand<SpawnInstance>(async (inst) =>
        {
            IQuery? query = null;

            if (inst is CreatureSpawnInstance creature)
            {
                var diff = new CreatureDiff()
                {
                    Guid = inst.Guid,
                    Entry = inst.Entry,
                    Position = inst.WorldObject!.Position,
                    Orientation = creature.Creature!.Orientation
                };
                query = creatureQueryGenerator.Update(diff);
            }
            else if (inst is GameObjectSpawnInstance go)
            {
                var diff = new GameObjectDiff()
                {
                    Guid = inst.Guid,
                    Entry = inst.Entry,
                    Position = inst.WorldObject!.Position,
                    Orientation = go.GameObject!.Orientation,
                    Rotation = go.GameObject!.Rotation
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