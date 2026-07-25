using System.Collections;
using System.Windows.Input;
using Hexa.NET.ImGui;
using Prism.Ioc;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheEngine.Utils;
using TheMaths;
using WDE.MapRenderer.Inspectors;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer
{
    public class GameManager : IGameContext
    {
        private readonly IContainerProvider containerProvider;
        private readonly IContainerRegistry registry;
        private readonly IGameProperties gameProperties;
        private readonly IRenderManager renderManager;
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        
        private CoroutineManager coroutineManager = null!;
        private NotificationsCenter notificationsCenter = null!;
        private TimeManager timeManager = null!;
        private ScreenSpaceSelector screenSpaceSelector = null!;
        private WoWMeshManager meshManager = null!;
        private WoWTextureManager textureManager = null!;
        private ChunkManager chunkManager = null!;
        private ModuleManager moduleManager = null!;
        private MainUi mainUi = null!;
        private MdxManager mdxManager = null!;
        private WmoManager wmoManager = null!;
        private CameraManager cameraManager = null!;
        private RaycastSystem raycastSystem = null!;
        private DbcManager dbcManager = null!;
        private LightingManager lightingManager = null!;
        private AreaTriggerManager areaTriggerManager = null!;
        private UpdateManager updateLoop = null!;
        private WorldManager worldManager = null!;
        private LoadingManager loadingManager = null!;
        private ZoneAreaManager zoneAreaManager = null!;
        private AnimationSystem animationSystem = null!;
        private LowDetailHeightMapManager lowDetailHeightMapManager = null!;
        private StatusIconsManager statusIconsManager = null!;

        public CoroutineManager CoroutineManager => coroutineManager;
        public NotificationsCenter NotificationsCenter => notificationsCenter;
        public TimeManager TimeManager => timeManager;
        public ScreenSpaceSelector ScreenSpaceSelector => screenSpaceSelector;
        public WoWMeshManager MeshManager => meshManager;
        public WoWTextureManager TextureManager => textureManager;
        public ChunkManager ChunkManager => chunkManager;
        public ModuleManager ModuleManager => moduleManager;
        public MainUi MainUi => mainUi;
        public MdxManager MdxManager => mdxManager;
        public WmoManager WmoManager => wmoManager;
        public CameraManager CameraManager => cameraManager;
        public RaycastSystem RaycastSystem => raycastSystem;
        public DbcManager DbcManager => dbcManager;
        public LightingManager LightingManager => lightingManager;
        public AreaTriggerManager AreaTriggerManager => areaTriggerManager;
        public UpdateManager UpdateLoop => updateLoop;
        public WorldManager WorldManager => worldManager;
        public LoadingManager LoadingManager => loadingManager;
        public AnimationSystem AnimationSystem => animationSystem;
        public ZoneAreaManager ZoneAreaManager => zoneAreaManager;
        public StatusIconsManager StatusIconsManager => statusIconsManager;
        public Engine Engine { get; }
        public IEntityManager EntityManager { get; }
        public ITextureManager EngineTextureManager { get; }
        public IMeshManager EngineMeshManager { get; }
        public IMaterialManager MaterialManager { get; }
        public IUIManager UiManager { get; }
        public Archetypes Archetypes { get; }

        public float Delta { get; private set; }
        public event Action<int>? ChangedMap;
        public unsafe Map* CurrentMap { get; private set; } = null;
        public unsafe int CurrentMapId => CurrentMap == null ? -1 : CurrentMap->Id;
        public bool IsInitialized { get; private set; }
        
        public GameManager(IContainerProvider containerProvider, 
            IContainerRegistry registry,
            Engine engine,
            IGameProperties gameProperties,
            IRenderManager renderManager,
            IEntityManager entityManager,
            ITextureManager engineTextureManager,
            IMeshManager engineMeshManager,
            IMaterialManager materialManager,
            IUIManager uiManager,
            EntityInspector entityInspector,
            Archetypes archetypes)
        {
            this.containerProvider = containerProvider;
            this.registry = registry;
            this.gameProperties = gameProperties;
            this.renderManager = renderManager;
            Engine = engine;
            EntityManager = entityManager;
            EngineTextureManager = engineTextureManager;
            EngineMeshManager = engineMeshManager;
            MaterialManager = materialManager;
            UiManager = uiManager;
            Archetypes = archetypes;
            updateLoop = new UpdateManager();

            entityInspector.RegisterInspectorDrawer(containerProvider.Resolve<MdxRendererInspector>());
            entityInspector.RegisterInspectorDrawer(containerProvider.Resolve<M2Inspector>());
        }
        
        public bool Initialize()
        {
            var gameFiles = ResolveOrCreate<IGameFiles>();
            if (!gameFiles.Initialize())
            {
                return false;
            }

            coroutineManager = ResolveOrCreate<CoroutineManager>();

            dbcManager = ResolveOrCreate<DbcManager>();
            SetMap(1);

            foreach (var store in dbcManager.Stores())
                registry.RegisterInstance(store.Item1, store.Item2);
            
            notificationsCenter = ResolveOrCreate<NotificationsCenter>();
            timeManager = ResolveOrCreate<TimeManager>();
            screenSpaceSelector = ResolveOrCreate<ScreenSpaceSelector>();
            loadingManager = ResolveOrCreate<LoadingManager>();
            zoneAreaManager = ResolveOrCreate<ZoneAreaManager>();
            textureManager = ResolveOrCreate<WoWTextureManager>();
            textureManager.SetQuality(gameProperties.TextureQuality);
            meshManager = ResolveOrCreate<WoWMeshManager>();
            mdxManager = ResolveOrCreate<MdxManager>();
            wmoManager = ResolveOrCreate<WmoManager>();
            worldManager = ResolveOrCreate<WorldManager>();
            chunkManager = ResolveOrCreate<ChunkManager>();
            cameraManager = ResolveOrCreate<CameraManager>();
            lightingManager = ResolveOrCreate<LightingManager>();
            areaTriggerManager = ResolveOrCreate<AreaTriggerManager>();
            raycastSystem = ResolveOrCreate<RaycastSystem>();
            moduleManager = ResolveOrCreate<ModuleManager>();
            mainUi = ResolveOrCreate<MainUi>();
            animationSystem = ResolveOrCreate<AnimationSystem>();
            lowDetailHeightMapManager = ResolveOrCreate<LowDetailHeightMapManager>();
            statusIconsManager = ResolveOrCreate<StatusIconsManager>();

            UiManager.OnMenuBarDraw += OnDrawMenuBar;

            IsInitialized = true;
            return true;
        }

        private bool enableAnimationSystem = true;
        private bool enableCameraManager = true;
        private bool enableLightingManager = true;
        private bool enableScreenSpaceSelector = true;
        private bool enableUpdateLoop = true;
        private bool enableChunkManager = true;
        private bool enableModuleManager = true;
        private bool enableLowDetailManager = true;

        private void OnDrawMenuBar()
        {
            if (ImGui.BeginMenu("Systems\0"u8))
            {
                ImGui.MenuItem("Animations\0"u8, Span<byte>.Empty, ref enableAnimationSystem);
                ImGui.MenuItem("Camera\0"u8, Span<byte>.Empty, ref enableCameraManager);
                ImGui.MenuItem("Lighting\0"u8, Span<byte>.Empty, ref enableLightingManager);
                ImGui.MenuItem("Screen space selector\0"u8, Span<byte>.Empty, ref enableScreenSpaceSelector);
                ImGui.MenuItem("Update loop\0"u8, Span<byte>.Empty, ref enableUpdateLoop);
                ImGui.MenuItem("Chunk Manager\0"u8, Span<byte>.Empty, ref enableChunkManager);
                ImGui.MenuItem("Module Manager\0"u8, Span<byte>.Empty, ref enableModuleManager);
                ImGui.MenuItem("Low Detail Terrain\0"u8, Span<byte>.Empty, ref enableLowDetailManager);

                ImGui.EndMenu();
            }
        }

        private T ResolveOrCreate<T>()
        {
            var t = containerProvider.Resolve<T>();
            if (t is IDisposable disp)
                disposables.Add(disp);
            return t;
        }

        public void Update(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }

            Delta = delta;
            
            loadingManager.Update(delta);
            coroutineManager.Step();

            timeManager.Update(delta);
            worldManager.Update(delta);

            if (enableAnimationSystem)
                animationSystem.Update(delta);

            if (enableCameraManager)
                cameraManager.Update(delta);

            if (enableLightingManager)
                lightingManager.Update(delta);

            if (enableScreenSpaceSelector)
                screenSpaceSelector.Update(delta);

            if (enableUpdateLoop)
                updateLoop.Update(delta);

            if (enableChunkManager)
                chunkManager.Update(delta);

            if (enableModuleManager)
                moduleManager.Update(delta);

            if (enableLowDetailManager)
                lowDetailHeightMapManager.Update(delta);

            statusIconsManager.Update(delta);
        }

        public void Render(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }

            // post-fence: safe to write animation caches to the global GPU buffers
            animationSystem.UploadToGpu();

            meshManager.Render();
            Engine.CameraManager.MainCamera.ViewDistanceModifier = gameProperties.ViewDistanceModifier;
            renderManager.SetDynamicResolutionScale(gameProperties.DynamicResolution);
            moduleManager.Render(delta);
            lightingManager.Render();
        }

        public void RenderTransparent(float delta)
        {
            areaTriggerManager.Render();
            statusIconsManager.Render();
            moduleManager.RenderTransparent();
        }

        public void RenderGui(float delta)
        {
            if (!gameProperties.RenderGui)
                return;
            moduleManager.RenderGUI();
            notificationsCenter.RenderGUI(delta);
            screenSpaceSelector.Render();
            cameraManager.RenderGUI();
            loadingManager.RenderGUI();
            mdxManager.RenderGUI();
            zoneAreaManager.RenderGUI();
        }

        public unsafe void SetMap(int mapId, Vector3? position = null)
        {
            if (dbcManager.MapStore.TryGetValue(mapId, out var map) && (CurrentMap == null || CurrentMap->Id != mapId))
            {
                CurrentMap = map;
                worldManager?.SetNextTeleportPosition(position);
                ChangedMap?.Invoke(mapId);
            }
            else if (CurrentMap != null && CurrentMap->Id == mapId && position.HasValue)
                // same map already loaded -> this is a "fly camera here" action: frame it (stand back)
                // and glide if it's close
                cameraManager.Relocate(position.Value, flyHere: true);
        }

        public void DisposeGame()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;
            for (int i = disposables.Count - 1; i >= 0; --i)
                disposables[i].Dispose();
            disposables.Clear();
        }

        public T? ResolveInstance<T>()
        {
            return containerProvider.Resolve<T>();
        }

        public List<(string, ICommand, object?)>? GenerateContextMenu()
        {
            List<(string, ICommand, object?)>? allItems = null;
            moduleManager.ForEach(mod =>
            {
                var items = mod.GenerateContextMenu();
                if (items != null)
                {
                    allItems ??= new List<(string, ICommand, object?)>();
                    allItems.AddRange(items);
                }
            });
            return allItems == null || allItems.Count == 0 ? null : allItems;
        }
    }
}
