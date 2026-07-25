using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using Avalonia.Threading;
using Prism.Commands;
using Prism.Ioc;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using WDE.Common.DBC;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Modules;
using WDE.MapRenderer.StaticData;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.WorldMap.Models;
using WDE.WorldMap.Services;

namespace WDE.MapRenderer
{
    public class GameCameraViewModel : IMapItem
    {
        private readonly GameViewModel gameViewModel;
        public float X { get; private set; }
        public float Y { get; private set;}
        public float Z { get; private set;}
        public Rect VirtualBounds { get; set; }

        public GameCameraViewModel(GameViewModel gameViewModel)
        {
            this.gameViewModel = gameViewModel;
        }
        
        public void UpdatePosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            gameViewModel.DoRender();
        }
    }
    
    [AutoRegister]
    // ISolutionItemManualUpdateSessionOnSave: the 3D editors register their own session items on
    // save (via the bridge) - the generic post-save session hook must not add a duplicate
    public partial class GameViewModel : ObservableBase, ISolutionItemDocument, ISolutionItemManualUpdateSessionOnSave, IMapContext<GameCameraViewModel>
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private readonly IMainThread mainThread;
        private Func<Game> gameCreator { get; }
        /// <summary>Which host control GameView builds (settings "3D view" page).</summary>
        public bool UseCompositionEnginePanel { get; }

        // ISolutionItemDocument: lets the global "Generate query"/"Copy SQL" buttons work on the 3D
        // document - the produced SQL is exactly what Save would execute (built by the game editors)
        public WDE.Common.ISolutionItem SolutionItem { get; }
        public bool ShowExportToolbarButtons => false;

        public async Task<WDE.SqlQueryGenerator.IQuery> GenerateQuery() =>
            WDE.SqlQueryGenerator.Queries.Raw(WDE.Common.Database.DataDatabaseType.World, await BuildPendingSaveSql());

        private async Task<string> BuildPendingSaveSql()
        {
            if (currentGame is not { } game)
                return "-- the 3D view is not running";
            var changesManager = game.Resolve<IChangesManager>();
            if (changesManager == null)
                return "-- the 3D view is still loading";
            return await changesManager.GenerateQuery();
        }

        private Game? currentGame;
        public Game? CurrentGame
        {
            get => currentGame;
            set
            {
                if (currentGame != null)
                {
                    currentGame.OnAfterDisposed -= CurrentGameOnOnAfterDisposed;
                    currentGame.OnFailedInitialize -= OnFailedGameInitialize;
                }
                SetProperty(ref currentGame, value);
                if (value != null)
                {
                    value.OnAfterDisposed += CurrentGameOnOnAfterDisposed;
                    value.OnFailedInitialize += OnFailedGameInitialize;   
                }
            }
        }

        private void OnFailedGameInitialize()
        {
            Dispatcher.UIThread.Post(() => CloseCommand?.Execute(null), DispatcherPriority.Background);
        }

        private MapViewModel? selectedMap;
        
        public MapViewModel? SelectedMap
        {
            get => selectedMap;
            set => SetProperty(ref selectedMap, value);
        }

        public IMapDataProvider MapData { get; }
        
        private IEnumerable<MapViewModel> maps;
        private bool isMapVisible;

        public IEnumerable<MapViewModel> Maps
        {
            get => maps;
            set => SetProperty(ref maps, value);
        }
        
        public string Stats { get; private set; }

        class GameProxy : IGameModule
        {
            private readonly GameViewModel vm;
            private readonly CameraManager cameraManager;
            private readonly IStatsManager statsManager;
            private readonly ModuleManager moduleManager;
            private readonly DbcManager dbcManager;
            private readonly TimeManager timeManager;
            private readonly IGameContext gameContext;

            private IDisposable? mapSub;
            private IDisposable? activationSub;

            private ObservableCollection<object>? registeredViewModels;
            private IDisposable? gameDisposable;

            // stats overlay churns strings 4x/sec; a reused builder collapses the whole thing to a single
            // ToString (needed for the Avalonia binding), and the raise delegate is cached so the
            // Dispatcher.Post doesn't allocate a fresh closure each time
            private readonly StringBuilder statsBuilder = new();
            private readonly Action raiseStatsChanged;
            // snapshot of ViewModels[0] taken on the game thread (the collection's owner),
            // so that UI-thread handlers never touch the live collection
            private volatile IDocument? firstModuleViewModel;

            public GameProxy(GameViewModel vm,
                CameraManager cameraManager,
                IStatsManager statsManager,
                ModuleManager moduleManager,
                DbcManager dbcManager,
                TimeManager timeManager,
                IGameContext gameContext,
                IChangesManager changesManager)
            {
                this.vm = vm;
                this.cameraManager = cameraManager;
                this.statsManager = statsManager;
                this.moduleManager = moduleManager;
                this.dbcManager = dbcManager;
                this.timeManager = timeManager;
                this.gameContext = gameContext;
                raiseStatsChanged = () => vm.RaisePropertyChanged(nameof(Stats));
                // raised from game-side editors; the INPC raise must happen on the UI thread
                gameDisposable = changesManager.IsModified.SubscribeAction(@is => vm.mainThread.Dispatch(() => vm.IsModified = @is));
            }
            
            // raised on the game thread (ViewModels is mutated by ModuleManager.Update/Dispose)
            private void RegisteredViewModelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                var first = moduleManager.ViewModels.Count > 0 ? (IDocument)moduleManager.ViewModels[0] : null;
                firstModuleViewModel = first;
                Dispatcher.UIThread.Post(() =>
                {
                    if (!vm.IsSelected)
                        return;
                    if (first != null)
                        vm.documentManager.Value.ActivateDocumentInTheBackground(first);
                    else
                        vm.documentManager.Value.ActiveDocument = null;
                }, DispatcherPriority.Background);
            }

            public void Dispose()
            {
                gameDisposable?.Dispose();
                gameDisposable = null;
                mapSub?.Dispose();
                activationSub?.Dispose();
                if (registeredViewModels != null)
                    registeredViewModels.CollectionChanged -= RegisteredViewModelsOnCollectionChanged;
            }

            public object? ViewModel => null;
            
            public unsafe void Initialize()
            {
                vm.LoadMaps(dbcManager);
                
                registeredViewModels = moduleManager.ViewModels;
                registeredViewModels.CollectionChanged += RegisteredViewModelsOnCollectionChanged;
                firstModuleViewModel = registeredViewModels.Count > 0 ? (IDocument)registeredViewModels[0] : null;

                // capture the map id here on the game thread, CurrentMap must not be dereferenced on the UI thread
                uint? currentMapId = gameContext.CurrentMap != null ? (uint)gameContext.CurrentMap->Id : null;
                Dispatcher.UIThread.Post(() => vm.SelectedMap = vm.Maps?.FirstOrDefault(x => currentMapId.HasValue && x.Id == currentMapId.Value), DispatcherPriority.Background);
                gameContext.ChangedMap += newMapId =>
                {
                    Dispatcher.UIThread.Post(() => vm.SelectedMap = vm.Maps.FirstOrDefault(x => x.Id == newMapId), DispatcherPriority.Background);
                };

                // the first emission happens inline here on the game thread, later ones on the UI thread
                activationSub = vm.ToObservable(v => v.IsSelected)
                    .SubscribeAction(@is =>
                    {
                        if (!@is)
                            return;
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (vm.IsSelected && firstModuleViewModel is { } doc)
                                vm.documentManager.Value.ActivateDocumentInTheBackground(doc);
                        }, DispatcherPriority.Background);
                    });

                mapSub = vm
                    .ToObservable(i => i.SelectedMap)
                    .Where(map => map != null)
                    .ObserveOnGameLoop()
                    .SubscribeAction(map =>
                    {
                        gameContext.SetMap((int)map!.Id);
                    });
            }

            // the stats string is a large interpolated allocation; the numbers are rolling averages, so
            // rebuilding it a few times a second (instead of every frame) is visually identical and
            // avoids a per-frame string+PropertyChanged churn when the overlay is shown
            private const float StatsRefreshInterval = 0.25f;
            private float statsRefreshTimer;

            public void Update(float delta)
            {
                var wowPos = cameraManager.Position;
                vm.cameraViewModel.UpdatePosition(wowPos.X, wowPos.Y, wowPos.Z);

                statsRefreshTimer -= delta;
                if (statsRefreshTimer <= 0)
                {
                    statsRefreshTimer = StatsRefreshInterval;
                    UpdateRenderStats();
                }
            }

            private void UpdateRenderStats()
            {
                if (!vm.DisplayStats)
                    return;

                ref var counters = ref statsManager.Counters;
                ref var stats = ref statsManager.RenderStats;
                float w = statsManager.PixelSize.X;
                float h = statsManager.PixelSize.Y;

                // interpolation INTO a StringBuilder formats each float/int straight into its buffer via
                // TryFormat - no intermediate strings, no boxing, no int.ToString. The reused builder
                // means the only allocation is the final ToString the Avalonia binding requires.
                var sb = statsBuilder;
                sb.Clear();
                sb.Append($"[{w:0}x{h:0}]\n");
                sb.Append($"Total frame time: {counters.FrameTime.Average:0.00} ms\n");
                sb.Append($" - Update time: {counters.UpdateTime.Average:0.00} ms\n");
                sb.Append($" - Render time: {counters.TotalRender.Average:0.00} ms\n");
                sb.Append($"   - Bounds: {counters.BoundsCalc.Average:0.00}ms\n");
                sb.Append($"   - Culling: {counters.Culling.Average:0.00}ms\n");
                sb.Append($"   - Drawing: {counters.Drawing.Average:0.00}ms\n");
                sb.Append($"   - Present time: {counters.PresentTime.Average:0.00} ms\n");
                sb.Append($"Shaders: {stats.ShaderSwitches}\n");
                sb.Append($"Materials: {stats.MaterialActivations}\n");
                sb.Append($"Meshes: {stats.MeshSwitches}\n");
                sb.Append($"Batches: {stats.NonInstancedDraws + stats.InstancedDraws}\n");
                sb.Append($"Batches saved by instancing: {stats.InstancedDrawSaved}\n");
                sb.Append($"Tris: {stats.TrianglesDrawn}");
                vm.Stats = sb.ToString();

                Dispatcher.UIThread.Post(raiseStatsChanged, DispatcherPriority.Render);
            }

            public void Render(float delta)
            {
            }

            public void RenderGUI()
            {
            }
        }
        
        public GameViewModel(IMpqService mpqService,
            IMapDataProvider mapData,
            IMessageBoxService messageBoxService,
            IGameView gameView,
            Func<Game> gameCreator,
            Lazy<IDocumentManager> documentManager,
            IMainThread mainThread,
            GameViewSettings gameViewSettings)
        {
            this.documentManager = documentManager;
            this.mainThread = mainThread;
            MapData = mapData;
            this.gameCreator = gameCreator;
            // latched at open: switching the host control requires reopening the 3D view
            UseCompositionEnginePanel = gameViewSettings.UseCompositionPanel;
            SolutionItem = new GameView3DSolutionItem(BuildPendingSaveSql);

            gameView.RegisterGameModule(container => container.Resolve<GameProxy>((typeof(GameViewModel), this)));
            gameView.RegisterGameModule(container => container.Resolve<DebugInfoGameModule>());
            gameView.RegisterGameModule(container => container.Resolve<WorldMapGameModule>());
            gameView.RegisterGameModule(container => container.Resolve<ViewSettingsToolbar>());
            gameView.RegisterGameModule(container => container.Resolve<UsageGameModule>());

            ToggleMapVisibilityCommand = new DelegateCommand(() => IsMapVisible = !IsMapVisible);
            ToggleStatsVisibilityCommand = new DelegateCommand(() => DisplayStats = !DisplayStats);
            
            cameraViewModel = new GameCameraViewModel(this);
            Items.Add(cameraViewModel);

            Visibility = true;
            On(() => Visibility, @is =>
            {
                if (@is && CurrentGame == null)
                {
                    CurrentGame = gameCreator();
                    state = 1;
                    if (!mpqService.IsConfigured())
                    {
                        messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Missing settings")
                            .SetMainInstruction("Missing WoW folder configuration")
                            .SetContent(
                                "In order to use the game view, you need to configure WoW 3.3.5 or 4.3.4 folder path in the settings -> Client Data Files")
                            .WithOkButton(true)
                            .Build()).ListenErrors();
                    }
                }
            });

            CloseCommand = new AsyncCommand(async () =>
            {
                gameDisposedTask = new();
                CanCloseTool();
                await gameDisposedTask.Task;
            });

            Save = new AsyncAutoCommand(() =>
            {
                if (currentGame != null)
                {
                    var changesManager = currentGame.Resolve<IChangesManager>();
                    return changesManager?.Save() ?? Task.CompletedTask;
                }
                return Task.CompletedTask;
            });
        }

        // Called from the game thread
        private unsafe void LoadMaps(DbcManager? dbcManager)
        {
            if (dbcManager == null)
                return;

            var _maps = new List<MapViewModel>();
            foreach (var map in dbcManager.MapStore)
            {
                if (map->MapType == MapType.Transport)
                    continue;
                var vm = new MapViewModel(map->Directory, Encoding.UTF8.GetString(map->Name.AsSpan()), (uint)map->Id);
                _maps.Add(vm);
            }
            maps = _maps.OrderBy(map => // sort by main continents first
                {
                    if (map.Id is 0 or 1)
                        return map.Id;
                    if (map.Id == 530) // outland
                        return 2U;
                    if (map.Id == 571) // northrend
                        return 3U;
                    return map.Id + 4;
                })
                .ToList();
            mainThread.Dispatch(() => RaisePropertyChanged(nameof(Maps)));
        }

        private int state = 0;
        private TaskCompletionSource<bool>? gameDisposedTask;

        public bool CanCloseTool()
        {
            if (state == 1)
            {
                currentGame?.DoDispose();
                state = 2;
                return false;
            }

            state = 0;
            return true;
        }

        private void CurrentGameOnOnAfterDisposed(Game game)
        {
            gameDisposedTask?.SetResult(true);
            game.OnAfterDisposed -= CurrentGameOnOnAfterDisposed;
            mainThread.Schedule(async () =>
            {
                await Task.Delay(10);
                Visibility = false;
                CurrentGame = null;
            });
        }

        public bool DisplayStats
        {
            get => displayStats;
            set => SetProperty(ref displayStats, value);
        }

        public bool IsMapVisible
        {
            get => isMapVisible;
            set => SetProperty(ref isMapVisible, value);
        }
        
        public ICommand ToggleMapVisibilityCommand { get; }
        public ICommand ToggleStatsVisibilityCommand { get; }
        
        public string UniqueId => "game_view";

        public bool Visibility
        {
            get => visibility;
            set
            {
                visibility = value;
                RaisePropertyChanged(nameof(Visibility));
            }
        }

        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.DocumentCenter;
        public bool OpenOnStart => false;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public string Title => "Game view";
        public ImageUri? Icon { get; } = new ImageUri("Icons/document_3d.png");
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; }

        public bool CanClose => true;

        public void Center(double x, double y)
        {
        }

        public event Action? RequestRender;
        public event Action<double, double>? RequestCenter;
        public event Action<double, double, double, double>? RequestBoundsToView;
        public void Initialized()
        {
        }

        private GameCameraViewModel cameraViewModel;
        private bool visibility;
        private bool displayStats = true;
        private bool isSelected;
        public ObservableCollection<GameCameraViewModel> Items { get; } = new();
        public IEnumerable<GameCameraViewModel> VisibleItems => Items;
        public GameCameraViewModel? SelectedItem { get; set; }
        
        public void Move(GameCameraViewModel item, double x, double y)
        {
            if (currentGame == null)
                return;
            
            var cameraManager = currentGame.Resolve<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.Relocate(new Vector3((float)x, (float)y, 200));
                RequestRender?.Invoke();
            }
        }
        public void StartMove() { }
        public void StopMove() { }
        
        public void DoRender()
        {
            RequestRender?.Invoke();
        }

        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public IHistoryManager? History { get; set; }
        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }
        private bool isModified;
    }
    
    public class MapViewModel
    {
        public MapViewModel(string mapPath, string? mapName, uint id)
        {
            MapPath = mapPath;
            MapName = mapName;
            Id = id;
        }

        public string MapPath { get; }
        public string? MapName { get; }
        public uint Id { get; }

        public override string ToString()
        {
            return MapName == null ? $"/{MapPath} ({Id})" : $"{MapName} [/{MapPath}] ({Id})";
        }
    }
}
