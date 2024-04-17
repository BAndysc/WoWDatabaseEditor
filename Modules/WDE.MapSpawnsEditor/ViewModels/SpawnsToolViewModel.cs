using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using TheMaths;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Rendering;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.MapSpawnsEditor.ViewModels;

[AutoRegister]
public partial class SpawnsToolViewModel : ObservableBase, ITool
{
    private readonly IGameView gameView;
    private readonly ISpawnViewerProxy spawnViewerProxy;
    private readonly ISpawnSelectionService spawnSelectionService;
    [Notify] private bool visibility;
    [Notify] private bool isSelected;

    [Notify] private string searchText = "";
    [Notify] private bool showAllPhases = false;
    public FlatTreeList<SpawnGroup, SpawnInstance> SpawnItems { get; }
    public ObservableCollection<GameEventViewModel> Events { get; }
    public ObservableCollection<GamePhaseViewModel> AllPhases { get; }

    public string Title => "Spawns";
    public string UniqueId => "map_spawns";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
    public bool OpenOnStart => false;

    public DelegateCommand<INodeType?> AddSpawn { get; }

    public event Action? FocusRequest;

    private System.IDisposable? scheduledFilter;

    [Notify] private bool filterTrigger;
    
    public SpawnsToolViewModel(
        ISpawnsContainer spawnsContainer,
        IGameEventService gameEventService,
        IGamePhaseService gamePhaseService,
        IGameView gameView,
        ISpawnViewerProxy spawnViewerProxy,
        IMainThread mainThread,
        ISpawnSelectionService spawnSelectionService)
    {
        this.gameView = gameView;
        this.spawnViewerProxy = spawnViewerProxy;
        this.spawnSelectionService = spawnSelectionService;
        SpawnItems = spawnsContainer.Spawns;
        Events = gameEventService.GameEvents;
        AllPhases = gamePhaseService.Phases;
        
        On(() => ShowAllPhases, @is => gamePhaseService.ShowAllPhases = @is);
        
        spawnSelectionService.SelectedSpawn.Subscribe(newSelectedSpawn =>
        {
            selectedNode = newSelectedSpawn;
            var parent = newSelectedSpawn?.Parent as SpawnGroup;
            while (parent != null)
            {
                parent.IsTemporaryExpanded = true;
                parent = parent.Parent as SpawnGroup;
            }
            RaisePropertyChanged(nameof(SelectedNode));
            RaisePropertyChanged(nameof(SelectedSpawn));
        });

        AddSpawn = new DelegateCommand<INodeType?>(node =>
        {
            if (node is not SpawnGroup s)
                return;
            FocusRequest?.Invoke();
            spawnViewerProxy.CurrentViewer!.SpawnAndDrag(s.Type == GroupType.Creature, (uint)s.Entry);
        }, node => node is SpawnGroup e && e.Type is GroupType.Creature or GroupType.GameObject);

        SpawnItems.SourceCollectionChanged += (_, _) =>
        {
            scheduledFilter?.Dispose();
            mainThread.Delay(() => DoFilter(searchText), TimeSpan.FromMilliseconds(100));
        };

        AutoDispose(this
            .ToObservable(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(text =>
            {
                UnexpandTemporary();
                DoFilter(text);
            }));
    }

    private void DoFilter(string text)
    {
        uint? searchNumber = null;

        if (uint.TryParse(text, out var searchEntry))
            searchNumber = searchEntry;

        foreach (var root in SpawnItems.GetRoots())
        {
            Filter(text, searchNumber, root, false);
        }

        FilterTrigger = !FilterTrigger;
    }

    private void UnexpandTemporary()
    {
        foreach (var root in SpawnItems.GetRoots())
            UnexpandTemporary(root);
    }

    private void UnexpandTemporary(SpawnGroup node)
    {
        node.IsTemporaryExpanded = false;
        foreach (var p in node.Nested)
            UnexpandTemporary(p);
    }

    private void Filter(string filterText, uint? filterNumber, SpawnGroup node, bool parentVisible)
    {
        if (string.IsNullOrEmpty(filterText))
        {
            node.IsVisible = true;
            for (var index = 0; index < node.Nested.Count; index++)
            {
                var p = node.Nested[index];
                Filter(filterText, filterNumber, p, true);
            }
            for (var index = 0; index < node.Spawns.Count; index++)
            {
                var c = node.Spawns[index];
                c.IsVisible = true;
            }
            return;
        }
        else
        {
            node.IsVisible = false;
        }

        // using for loops to prevent allocations
        if (parentVisible)
        {
            node.IsVisible = true;
            for (var index = 0; index < node.Nested.Count; index++)
            {
                var p = node.Nested[index];
                Filter(filterText, filterNumber, p, true);
            }
            for (var index = 0; index < node.Spawns.Count; index++)
            {
                var c = node.Spawns[index];
                c.IsVisible = true;
            }
        }
        else
        {
            bool isVisible = false;
            if (filterNumber.HasValue && filterNumber == node.Entry ||
                node.Header.Contains(filterText, StringComparison.InvariantCultureIgnoreCase))
            {
                isVisible = true;
                for (var index = 0; index < node.Nested.Count; index++)
                {
                    var p = node.Nested[index];
                    Filter(filterText, filterNumber, p, true);
                }

                for (var index = 0; index < node.Spawns.Count; index++)
                {
                    var c = node.Spawns[index];
                    c.IsVisible = true;
                }
            }
            else
            {
                for (var index = 0; index < node.Nested.Count; index++)
                {
                    var p = node.Nested[index];
                    Filter(filterText, filterNumber, p, false);
                }

                for (var index = 0; index < node.Spawns.Count; index++)
                {
                    var c = node.Spawns[index];
                    c.IsVisible = false;
                    if (filterNumber.HasValue && filterNumber == c.Guid)
                    {
                        isVisible = true;
                        c.IsVisible = true;
                    }
                }
            }

            if (isVisible)
            {
                node.IsVisible = true;
                node.IsTemporaryExpanded = true;
                var parent = node.Parent;
                while (parent != null)
                {
                    parent.IsVisible = true;
                    ((SpawnGroup)parent).IsTemporaryExpanded = true;
                    parent = parent.Parent;
                }   
            }
        }
    }

    private INodeType? selectedNode;
    public INodeType? SelectedNode
    {
        get => selectedNode;
        set
        {
            if (value is SpawnInstance spawnInstance)
            {
                if (spawnSelectionService.SelectedSpawn.Value == spawnInstance)
                {
                    selectedNode = value;
                    RaisePropertyChanged(nameof(SelectedNode));
                }
                else
                    spawnSelectionService.SelectedSpawn.Value = spawnInstance;
            }
            else
            {
                selectedNode = value;
                RaisePropertyChanged(nameof(SelectedNode));
            }
        }
    }
    
    public SpawnInstance? SelectedSpawn
    {
        get => spawnSelectionService.SelectedSpawn.Value;
        set => spawnSelectionService.SelectedSpawn.Value = value;
    }

    public async Task TeleportTo(SpawnInstance spawn)
    {
        var game = await gameView.Open();
        var cameraManager = game.Resolve<CameraManager>();
        var position = spawn.Position + cameraManager!.Rotation.Multiply(Vectors.Up) * 10;
        game.Resolve<IGameContext>()!.SetMap((int)spawn.Map, position);
    }
}