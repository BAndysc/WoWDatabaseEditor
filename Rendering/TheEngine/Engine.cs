using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using TheEngine;
using TheEngine.Resources;
using TheEngine.ECS;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Physics;
using TheEngine.Utils;
using TheEngine.Vulkan;

[assembly: InternalsVisibleTo("TheEngine.Test")]
[assembly: InternalsVisibleTo("SponzaDemo")]
[assembly: InternalsVisibleTo("TheEngineAvalonia")]
namespace TheEngine
{
    public class Engine : IDisposable
    {
        internal int GameThreadId = Environment.CurrentManagedThreadId;

        public bool IsOnGameThread => Environment.CurrentManagedThreadId == GameThreadId;

        internal Rendering.IRenderBackend Backend { get; }

        public string BackendName => Backend.Name;

        /// <summary>False when the present is paced by an external compositor (the Avalonia
        /// composition panel) - vsync is then effectively always on and <see cref="VSync"/>
        /// ignores writes.</summary>
        public bool SupportsVSyncControl => Backend.SupportsVSyncControl;

        /// <summary>Desired vsync state; applied at the start of the next frame.</summary>
        public bool VSync
        {
            get => Backend.VSync;
            set => Backend.VSync = value;
        }

        internal IConfiguration Configuration { get; }

        internal ShaderManager shaderManager { get; }
        public IShaderManager ShaderManager => shaderManager;


        internal MeshManager meshManager { get; }
        public IMeshManager MeshManager => meshManager;


        internal RenderManager renderManager { get; }
        public IRenderManager RenderManager => renderManager;


        internal CameraManager cameraManger { get; }
        public ICameraManager CameraManager => cameraManger;

        internal InputManager inputManager { get; }
        public IInputManager InputManager => inputManager;

        internal LightManager lightManager { get; }
        public ILightManager LightManager => lightManager;

        internal DecalManager decalManager { get; }
        public IDecalManager DecalManager => decalManager;

        internal TextureManager textureManager { get; }
        public ITextureManager TextureManager => textureManager;

        internal MaterialManager materialManager { get; }
        public IMaterialManager MaterialManager => materialManager;

        internal PipelineManager pipelineManager { get; }
        public IPipelineManager PipelineManager => pipelineManager;

        public IWindowHost WindowHost { get; }

        internal EntityManager entityManager { get; }
        public IEntityManager EntityManager => entityManager;

        internal PhysicsManager physicsManager { get; }
        public IPhysicsManager PhysicsManager => physicsManager;

        internal StatsManager statsManager { get; }
        public IStatsManager StatsManager => statsManager;
        
        internal FontManager fontManager { get; }
        public IFontManager FontManager => fontManager;
        
        internal UIManager uiManager { get; }
        public IUIManager Ui => uiManager;

        internal EngineGameView gameView;
        public IEngineView GameView => gameView;

        internal EngineSceneView sceneView;
        public IEngineView SceneView => sceneView;

        public TheEngineUi EngineUi { get; }

        public EntityInspector EntityInspector;

        public double TotalTime;

        public EnterThreadPoolAwaitable EnterThreadPool;
        public EnterGameLoopAwaitable EnterGameLoop;
        public NextFrameAwaitable NextFrame;

        public long FrameCount { get; internal set; }

        internal Engine(Rendering.IRenderBackend backend, IConfiguration configuration, IWindowHost host, bool flipY)
        {
            Static.MainThreadId = Environment.CurrentManagedThreadId;
            WindowHost = host;
            //windowHost.Bind(this);

            EnterThreadPool = new EnterThreadPoolAwaitable(this);
            EnterGameLoop = new EnterGameLoopAwaitable(this);
            NextFrame = new NextFrameAwaitable(this);

            Configuration = configuration;
            Backend = backend;

            statsManager = new StatsManager();
            entityManager = new EntityManager(statsManager, this);
            physicsManager = new PhysicsManager(this);

            lightManager = new LightManager(this);
            decalManager = new DecalManager(this);
            inputManager = new InputManager(this);
            cameraManger = new CameraManager(this);

            pipelineManager = new PipelineManager(this);
            materialManager = new MaterialManager(this);
            shaderManager = new ShaderManager(this);
            meshManager = new MeshManager(this);
            textureManager = new TextureManager(this);
            renderManager = new RenderManager(this, flipY);

            gameView = new EngineGameView(this);
            sceneView = new EngineSceneView(this);

            fontManager = new FontManager(this);
            uiManager = new UIManager(this);

            EngineUi = new TheEngineUi(this);
            EntityInspector = new(this);
        }
        
        internal void UpdateGui(float delta)
        {
            // todo
            meshManager.Update();
            textureManager.Update();
            materialManager.Update();
            Backend.CollectDisposedResources();
            statsManager.BufferBytes = Backend.TotalBufferBytes;

            uiManager.UpdateGui(delta);
            EntityInspector.UpdateGui(delta);
        }

        internal void Render3DGUI()
        {
            renderManager.CommandList.InsertDebugMarker("  Rendering 3D GUI");
            uiManager.Render3D();
        }

        internal void RenderGUI()
        {
            renderManager.CommandList.InsertDebugMarker("  Rendering GUI");
            uiManager.Render();
        }

        public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data) where T : unmanaged => Backend.CreateBuffer<T>(bufferType, data);
        public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size) where T : unmanaged => Backend.CreateBuffer<T>(bufferType, size);

        /// <summary>
        /// Registers a per-frame (dynamic) global storage buffer at the given set-3 binding (must
        /// match the shader's <c>set = 3, binding = N</c>). Must be called before the first draw.
        /// The returned handle's <see cref="IGlobalBuffer{T}.BeginWrite"/> is valid only during the
        /// render phase (post-BeginFrame fence wait).
        /// </summary>
        public IGlobalBuffer<T> CreateGlobalBuffer<T>(uint binding, int initialCapacity = 64) where T : unmanaged
        {
            if (Backend is not VulkanRenderBackend vk)
                throw new NotSupportedException("Global buffers require the Vulkan backend.");
            return vk.CreateGlobalBuffer<T>(binding, initialCapacity);
        }

        /// <summary>
        /// Registers a STATIC global storage buffer at the given set-3 binding (must match the
        /// shader's <c>set = 3, binding = N</c>). A single persistent backing handed out in
        /// fixed-size slots, written only when contents change (e.g. terrain tile load/unload)
        /// rather than per frame. See <see cref="IStaticGlobalBuffer{T}"/>.
        /// </summary>
        public IStaticGlobalBuffer<T> CreateStaticGlobalBuffer<T>(uint binding, int slotElementCount, int initialSlots = 8) where T : unmanaged
        {
            if (Backend is not VulkanRenderBackend vk)
                throw new NotSupportedException("Global buffers require the Vulkan backend.");
            return vk.CreateStaticGlobalBuffer<T>(binding, slotElementCount, initialSlots);
        }

        public void Dispose()
        {
            uiManager.Dispose();
            fontManager.Dispose();
            lightManager.Dispose();
            decalManager.Dispose();
            cameraManger.Dispose();
            materialManager.Dispose();
            renderManager.Dispose();
            meshManager.Dispose();
            textureManager.Dispose();
            shaderManager.Dispose();
            entityManager.Dispose();
            physicsManager.Dispose();
            Backend.Dispose();
        }

        private List<Action>[] nextFrameActions = [new List<Action>(), new List<Action>()];
        private int currentNextFrameActionsIndex = 0;
        private ConcurrentQueue<Action> multithreadedActions = new ConcurrentQueue<Action>();

        internal void ExecuteNextFrameActions()
        {
            var previousActions = nextFrameActions[currentNextFrameActionsIndex];
            currentNextFrameActionsIndex = 1 - currentNextFrameActionsIndex;
            if (previousActions.Count > 0)
            {
                for (var index = 0; index < previousActions.Count; index++)
                {
                    var action = previousActions[index];
                    action();
                }

                previousActions.Clear();
            }

            while (multithreadedActions.TryDequeue(out var action))
            {
                action();
            }
        }

        internal void PostNextFrame(Action continuation)
        {
            if (Environment.CurrentManagedThreadId == GameThreadId)
            {
                nextFrameActions[currentNextFrameActionsIndex].Add(continuation);
            }
            else
            {
                multithreadedActions.Enqueue(continuation);
            }
        }

        public void BeginFrame()
        {
            InFrame = true;
            Backend.BeginFrame();
        }

        public void EndFrame()
        {
            Backend.EndFrame();
            InFrame = false;
        }

        public bool InFrame { get; private set; }
    }

    public class EnterGameLoopAwaitable
    {
        private readonly Engine engine;

        public Awaiter GetAwaiter() => new Awaiter(engine);

        public EnterGameLoopAwaitable(Engine engine)
        {
            this.engine = engine;
        }

        public struct Awaiter(Engine engine) : System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                if (Environment.CurrentManagedThreadId == engine.GameThreadId)
                {
                    if (!engine.Backend.InFrame)
                    {
                        throw new Exception("will crash!");
                    }
                    continuation();
                }
                else
                    engine.PostNextFrame(continuation);
            }

            public void GetResult() { }
        }
    }
    public class EnterThreadPoolAwaitable
    {
        private readonly Engine engine;

        public EnterThreadPoolAwaitable(Engine engine)
        {
            this.engine = engine;
        }

        public Awaiter GetAwaiter() => new Awaiter(engine);

        public struct Awaiter(Engine engine) : System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                if (Environment.CurrentManagedThreadId != engine.GameThreadId)
                    continuation();
                else
                    ThreadPool.QueueUserWorkItem(_ => continuation());
            }

            public void GetResult() { }
        }
    }

    public class NextFrameAwaitable
    {
        private readonly Engine engine;

        public NextFrameAwaitable(Engine engine)
        {
            this.engine = engine;
        }

        public Awaiter GetAwaiter() => new Awaiter(engine);

        public struct Awaiter(Engine engine) : System.Runtime.CompilerServices.INotifyCompletion
        {
            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                engine.PostNextFrame(continuation);
            }

            public void GetResult() { }
        }
    }

}
