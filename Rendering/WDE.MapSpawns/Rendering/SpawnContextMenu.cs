using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

public class SpawnContextMenu
{
    private readonly ISpawnSelectionService spawnSelectionService;
    
    private ICommand CopyGuidCommand { get; }

    public SpawnContextMenu(ISpawnSelectionService spawnSelectionService,
        IClipboardService clipboardService)
    {
        this.spawnSelectionService = spawnSelectionService;

        CopyGuidCommand = new DelegateCommand<SpawnInstance>(inst => clipboardService.SetText(inst.Guid.ToString()));
    }
    
    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        if (spawn == null || !spawn.IsSpawned)
            yield break;

        yield return ("Copy guid", CopyGuidCommand, spawn);
    }
}