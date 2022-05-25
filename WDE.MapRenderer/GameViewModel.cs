using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using Prism.Commands;
using Prism.Ioc;
using TheEngine.Interfaces;
using TheMaths;
using WDE.Common.DBC;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MapRenderer.Managers;
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
    public class GameViewModel : ObservableBase, ITool, IMapContext<GameCameraViewModel>
    {
        private readonly Lazy<IDocumentManager> documentManager;
        private readonly GameViewSettings settings;
        private readonly IMainThread mainThread;
        private Func<Game> gameCreator { get; }
        private Game? currentGame;
        public Game? CurrentGame
        {
            get => currentGame;
            set
            {
                if (currentGame != null)
                {
                    currentGame.OnFailedInitialize -= OnFailedGameInitialize;
                }
                SetProperty(ref currentGame, value);
                if (value != null)
                {
                    value.OnFailedInitialize += OnFailedGameInitialize;   
                }
            }
        }

        private void OnFailedGameInitialize()
        {
            Dispatcher.UIThread.Post(() => Visibility = false, DispatcherPriority.Background);
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
        
        private GameProperties Properties { get; }

        class GameProxy : IGameModule
        {
            private readonly GameViewModel vm;
            private readonly CameraManager cameraManager;
            private readonly IStatsManager statsManager;
            private readonly ModuleManager moduleManager;
            private readonly TimeManager timeManager;
            private readonly IGameContext gameContext;

            private IDisposable? mapSub;
            private IDisposable? activationSub;

            private ObservableCollection<object>? registeredViewModels;

            public GameProxy(GameViewModel vm,
                CameraManager cameraManager,
                IStatsManager statsManager,
                ModuleManager moduleManager,
                TimeManager timeManager,
                IGameContext gameContext)
            {
                this.vm = vm;
                this.cameraManager = cameraManager;
                this.statsManager = statsManager;
                this.moduleManager = moduleManager;
                this.timeManager = timeManager;
                this.gameContext = gameContext;
            }
            
            private void RegisteredViewModelsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (vm.IsSelected)
                {
                    if (moduleManager.ViewModels.Count > 0)
                    {
                        vm.documentManager.Value.ActivateDocumentInTheBackground((IDocument)moduleManager.ViewModels[0]);
                    }
                    else if (moduleManager.ViewModels.Count == 0)
                        vm.documentManager.Value.ActiveDocument = null;
                }
            }

            public void Dispose()
            {
                mapSub?.Dispose();
                activationSub?.Dispose();
                if (registeredViewModels != null)
                    registeredViewModels.CollectionChanged -= RegisteredViewModelsOnCollectionChanged;
            }

            public object? ViewModel => null;
            
            public void Initialize()
            {
                registeredViewModels = moduleManager.ViewModels;
                registeredViewModels.CollectionChanged += RegisteredViewModelsOnCollectionChanged;
                
                Dispatcher.UIThread.Post(() => vm.SelectedMap = vm.Maps?.FirstOrDefault(x => x.Id == gameContext.CurrentMap.Id), DispatcherPriority.Background);
                gameContext.ChangedMap += newMapId =>
                {
                    Dispatcher.UIThread.Post(() => vm.SelectedMap = vm.Maps.FirstOrDefault(x => x.Id == newMapId), DispatcherPriority.Background);
                };

                activationSub = vm.ToObservable(v => v.IsSelected)
                    .SubscribeAction(@is =>
                    {
                        if (@is)
                        {
                            if (moduleManager.ViewModels.Count > 0)
                            {
                                vm.documentManager.Value.ActivateDocumentInTheBackground((IDocument)moduleManager.ViewModels[0]);
                            }
                        }
                    });

                mapSub = vm
                    .ToObservable(i => i.SelectedMap)
                    .Where(map => map != null)
                    .SubscribeAction(map =>
                    {
                        gameContext.SetMap((int)map!.Id);
                    });
            }

            public void Update(float delta)
            {
                var wowPos = cameraManager.Position;
                vm.cameraViewModel.UpdatePosition(wowPos.X, wowPos.Y, wowPos.Z);
                vm.RaisePropertyChanged(nameof(CurrentTime));

                UpdateRenderStats();
            }

            private void UpdateRenderStats()
            {
                if (!vm.DisplayStats)
                    return;
                
                ref var counters = ref statsManager.Counters;
                ref var stats = ref statsManager.RenderStats;
                float w = statsManager.PixelSize.X;
                float h = statsManager.PixelSize.Y;
                vm.Stats =
                    $@"[{w:0}x{h:0}]
Total frame time: {counters.FrameTime.Average:0.00} ms
 - Update time: {counters.UpdateTime.Average:0.00} ms
 - Render time: {counters.TotalRender.Average:0.00} ms
   - Bounds: {counters.BoundsCalc.Average:0.00}ms
   - Culling: {counters.Culling.Average:0.00}ms
   - Drawing: {counters.Drawing.Average:0.00}ms
   - Present time: {counters.PresentTime.Average:0.00} ms";

                vm.Stats += "\n" + @"Shaders: " + stats.ShaderSwitches + @"
Materials: " + stats.MaterialActivations + @"
Meshes: " + stats.MeshSwitches + @"
Batches: " + (stats.NonInstancedDraws + stats.InstancedDraws) + @"
Batches saved by instancing: " + stats.InstancedDrawSaved + @"
Tris: " + stats.TrianglesDrawn;
                Dispatcher.UIThread.Post(()=>
                {
                    vm.RaisePropertyChanged(nameof(Stats));
                }, DispatcherPriority.Render);
            }

            public void Render()
            {
            }

            public void RenderGUI()
            {
            }
        }
        
        public GameViewModel(IMpqService mpqService,
            IDbcStore dbcStore, 
            IMapDataProvider mapData, 
            ITaskRunner taskRunner,
            IMessageBoxService messageBoxService,
            IGameView gameView,
            Func<Game> gameCreator,
            GameProperties gameProperties,
            Lazy<IDocumentManager> documentManager,
            GameViewSettings settings,
            IMainThread mainThread)
        {
            this.documentManager = documentManager;
            this.settings = settings;
            this.mainThread = mainThread;
            MapData = mapData;
            this.gameCreator = gameCreator;
            Properties = gameProperties;
            Properties.OverrideLighting = settings.OverrideLighting;
            Properties.DisableTimeFlow = settings.DisableTimeFlow;
            Properties.CurrentTime = Time.FromMinutes(settings.CurrentTime);
            Properties.TimeSpeedMultiplier = settings.TimeSpeedMultiplier;
            Properties.ShowGrid = settings.ShowGrid;
            Properties.ViewDistanceModifier = settings.ViewDistanceModifier;
            Properties.ShowAreaTriggers = settings.ShowAreaTriggers;
            Properties.TextureQuality = settings.TextureQuality;

            gameView.RegisterGameModule(container => container.Resolve<GameProxy>((typeof(GameViewModel), this)));
            
            taskRunner.ScheduleTask("Loading maps", async () =>
            {
                maps = dbcStore.MapDirectoryStore
                    .Select(pair =>
                    {
                        dbcStore.MapStore.TryGetValue(pair.Key, out var mapName);
                        return new MapViewModel(pair.Value,  mapName, (uint)pair.Key);
                    }).ToList();
                RaisePropertyChanged(nameof(Maps));
            });
            
            ToggleMapVisibilityCommand = new DelegateCommand(() => IsMapVisible = !IsMapVisible);
            ToggleStatsVisibilityCommand = new DelegateCommand(() => DisplayStats = !DisplayStats);
            
            cameraViewModel = new GameCameraViewModel(this);
            Items.Add(cameraViewModel);
            
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
                                "In order to use the game view, you need to configure WoW 3.3.5 folder path in the settings -> Client Data Files")
                            .WithOkButton(true)
                            .Build()).ListenErrors();
                    }
                }
            });
        }

        private int state = 0;
        
        public bool CanClose()
        {
            if (state == 1)
            {
                currentGame?.DoDispose();
                currentGame = null;
                state = 2;
                mainThread.Delay(() =>
                {
                    Visibility = false;
                }, TimeSpan.FromMilliseconds(10));
                return false;
            }

            state = 0;
            return true;
        }

        public bool OverrideLighting
        {
            get => Properties.OverrideLighting;
            set
            {
                Properties.OverrideLighting = value;
                settings.OverrideLighting = value;
                RaisePropertyChanged(nameof(OverrideLighting));
            }
        }
        
        public bool DisableTimeFlow
        {
            get => Properties.DisableTimeFlow;
            set
            {
                Properties.DisableTimeFlow = value;
                settings.DisableTimeFlow = value;
                RaisePropertyChanged(nameof(DisableTimeFlow));
            }
        }

        public int TimeSpeedMultiplier
        {
            get => Properties.TimeSpeedMultiplier;
            set
            {
                Properties.TimeSpeedMultiplier = value;
                settings.TimeSpeedMultiplier = value;
                RaisePropertyChanged(nameof(TimeSpeedMultiplier));
            }
        }

        public bool ShowAreaTriggers
        {
            get => Properties.ShowAreaTriggers;
            set
            {
                Properties.ShowAreaTriggers = value;
                settings.ShowAreaTriggers = value;
                RaisePropertyChanged(nameof(ShowAreaTriggers));
            }
        }
        
        public bool ShowGrid
        {
            get => Properties.ShowGrid;
            set
            {
                Properties.ShowGrid = value;
                settings.ShowGrid = value;
                RaisePropertyChanged(nameof(ShowGrid));
            }
        }

        public float ViewDistance
        {
            get => Properties.ViewDistanceModifier;
            set
            {
                Properties.ViewDistanceModifier = value;
                settings.ViewDistanceModifier = value;
                RaisePropertyChanged(nameof(ViewDistance));
            }
        }
        
        public bool ShowTextureQualityWarning { get; set; }

        public int TextureQuality
        {
            get => Properties.TextureQuality;
            set
            {
                Properties.TextureQuality = value;
                settings.TextureQuality = value;
                ShowTextureQualityWarning = true;
                RaisePropertyChanged(nameof(ShowTextureQualityWarning));
                RaisePropertyChanged(nameof(TextureQuality));
            }
        }
        
        public float DynamicResolution
        {
            get => Properties.DynamicResolution;
            set
            {
                Properties.DynamicResolution = value;
                RaisePropertyChanged(nameof(DynamicResolution));
            }
        }
        
        public int CurrentTime
        {
            get => Properties.CurrentTime.TotalMinutes;
            set
            {
                Properties.CurrentTime = Time.FromMinutes(value);
                settings.CurrentTime = value;
                RaisePropertyChanged(nameof(CurrentTime));
            }
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
        private bool displayStats;
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