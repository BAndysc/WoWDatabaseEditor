using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Rendering;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawns.ViewModels;

//[AutoRegister]
public partial class SpawnsToolViewModel : ObservableBase, ITool
{
    private readonly ISpawnSelectionService spawnSelectionService;
    [Notify] private bool visibility;
    [Notify] private bool isSelected;

    [Notify] private string searchText = "";
    public FlatTreeList<SpawnEntry, SpawnInstance> SpawnItems { get; }
    public ObservableCollection<GameEventViewModel> Events { get; }
    public ObservableCollection<GamePhaseViewModel> AllPhases { get; }

    public string Title => "Spawns";
    public string UniqueId => "map_spawns";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
    public bool OpenOnStart => false;

    public SpawnsToolViewModel(
        ISpawnsContainer spawnsContainer,
        IGameEventService gameEventService,
        IGamePhaseService gamePhaseService,
        ISpawnSelectionService spawnSelectionService)
    {
        this.spawnSelectionService = spawnSelectionService;
        SpawnItems = spawnsContainer.Spawns;
        Events = gameEventService.GameEvents;
        AllPhases = gamePhaseService.Phases;
        spawnSelectionService.SelectedSpawn.Subscribe(_ => RaisePropertyChanged(nameof(SelectedSpawn)));
    }

    public SpawnInstance? SelectedSpawn
    {
        get => spawnSelectionService.SelectedSpawn.Value;
        set => spawnSelectionService.SelectedSpawn.Value = value;
    }
}