using System.Collections;
using Prism.Ioc;
using TheEngine.Coroutines;
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
        private CreatureManager creatureManager = null!;
        private GameObjectManager gameObjectManager = null!;
        private AnimationSystem animationSystem = null!;

        public float Delta { get; private set; }
        public event Action<int>? ChangedMap;
        public Map CurrentMap { get; private set; } = Map.Empty;
        public bool IsInitialized { get; private set; }
        
        public GameManager(IContainerProvider containerProvider, 
            IContainerRegistry registry,
            IGameProperties gameProperties,
            IRenderManager renderManager)
        {
            this.containerProvider = containerProvider;
            this.registry = registry;
            this.gameProperties = gameProperties;
            this.renderManager = renderManager;
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
            textureManager = ResolveOrCreate<WoWTextureManager>();
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
            creatureManager = ResolveOrCreate<CreatureManager>();
            gameObjectManager = ResolveOrCreate<GameObjectManager>();
            animationSystem = ResolveOrCreate<AnimationSystem>();
            
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

            renderManager.ViewDistanceModifier = gameProperties.ViewDistanceModifier;
            renderManager.SetDynamicResolutionScale(gameProperties.DynamicResolution);
            moduleManager.Render();
            lightingManager.Render();
        }

        public void RenderTransparent(float delta)
        {
            areaTriggerManager.Render();
            moduleManager.RenderTransparent();
        }

        public void RenderGui(float delta)
        {
            moduleManager.RenderGUI();
            notificationsCenter.RenderGUI(delta);
            screenSpaceSelector.Render();
            cameraManager.RenderGUI();
            loadingManager.RenderGUI();
            timeManager.RenderGUI();
            mdxManager.RenderGUI();
        }

        public void SetMap(int mapId, Vector3? position = null)
        {
            if (dbcManager.MapStore.Contains(mapId) && CurrentMap.Id != mapId)
            {
                CurrentMap = dbcManager.MapStore[mapId];
                worldManager?.SetNextTeleportPosition(position);
                ChangedMap?.Invoke(mapId);
            }
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
    }
}
