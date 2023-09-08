using System.Collections;
using System.Windows.Input;
using Prism.Ioc;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers;
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

        public CoroutineManager CoroutineManager => coroutineManager;
        public NotificationsCenter NotificationsCenter => notificationsCenter;
        public TimeManager TimeManager => timeManager;
        public ScreenSpaceSelector ScreenSpaceSelector => screenSpaceSelector;
        public WoWMeshManager MeshManager => meshManager;
        public WoWTextureManager TextureManager => textureManager;
        public ChunkManager ChunkManager => chunkManager;
        public ModuleManager ModuleManager => moduleManager;
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
        public Engine Engine { get; }
        public IEntityManager EntityManager { get; }
        public ITextureManager EngineTextureManager { get; }
        public IMeshManager EngineMeshManager { get; }
        public IMaterialManager MaterialManager { get; }
        public IUIManager UiManager { get; }
        public Archetypes Archetypes { get; }

        public float Delta { get; private set; }
        public event Action<int>? ChangedMap;
        public Map CurrentMap { get; private set; } = Map.Empty;
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
            animationSystem = ResolveOrCreate<AnimationSystem>();
            lowDetailHeightMapManager = ResolveOrCreate<LowDetailHeightMapManager>();
            
            IsInitialized = true;
            return true;
        }
        
        private T ResolveOrCreate<T>()
        {
            var t = containerProvider.Resolve<T>();
            if (t is IDisposable disp)
                disposables.Add(disp);
            return t;
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            coroutineManager.Start(coroutine);
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
            
            animationSystem.Update(delta);
            
            cameraManager.Update(delta);
            lightingManager.Update(delta);
            
            screenSpaceSelector.Update(delta);
            updateLoop.Update(delta);
            chunkManager.Update(delta);
            moduleManager.Update(delta);
        }

        public void Render(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }

            lowDetailHeightMapManager.Render();
            meshManager.Render();
            renderManager.ViewDistanceModifier = gameProperties.ViewDistanceModifier;
            renderManager.SetDynamicResolutionScale(gameProperties.DynamicResolution);
            moduleManager.Render(delta);
            lightingManager.Render();
        }

        public void RenderTransparent(float delta)
        {
            areaTriggerManager.Render();
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
            timeManager.RenderGUI();
            mdxManager.RenderGUI();
            zoneAreaManager.RenderGUI();
        }

        public void SetMap(int mapId, Vector3? position = null)
        {
            if (dbcManager.MapStore.Contains(mapId) && CurrentMap.Id != mapId)
            {
                CurrentMap = dbcManager.MapStore[mapId];
                worldManager?.SetNextTeleportPosition(position);
                ChangedMap?.Invoke(mapId);
            }
            else if (CurrentMap.Id == mapId && position.HasValue)
                cameraManager.Relocate(position.Value);
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
