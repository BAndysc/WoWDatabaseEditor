using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.ECS;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Utils;

[assembly: InternalsVisibleTo("TheEngine.Test")]
namespace TheEngine
{
    public class Engine : IDisposable
    {
        internal int GameThreadId = Environment.CurrentManagedThreadId;

        internal TheDevice Device { get; }

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
        
        internal TextureManager textureManager { get; }
        public ITextureManager TextureManager => textureManager;

        internal MaterialManager materialManager { get; }
        public IMaterialManager MaterialManager => materialManager;
        public IWindowHost WindowHost { get; }

        internal EntityManager entityManager { get; }
        public IEntityManager EntityManager => entityManager;
        
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

        public Engine(IDevice device, IConfiguration configuration, IWindowHost host, bool flipY)
        {
            WindowHost = host;
            //windowHost.Bind(this);

            EnterThreadPool = new EnterThreadPoolAwaitable(this);
            EnterGameLoop = new EnterGameLoopAwaitable(this);
            NextFrame = new NextFrameAwaitable(this);

            Configuration = configuration;
            Device = new TheDevice(host, device, false);

            Device.Initialize();

            statsManager = new StatsManager();
            entityManager = new EntityManager(this);
            
            lightManager = new LightManager(this);
            inputManager = new InputManager(this);
            cameraManger = new CameraManager(this);

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
            Device.device.DisposeBuffers();
            statsManager.BufferBytes = Device.device.TotalBufferBytes;

            uiManager.UpdateGui(delta);
            EntityInspector.UpdateGui(delta);
        }

        internal void Render3DGUI()
        {
            Device.device.Debug("  Rendering 3D GUI");
            uiManager.Render3D();
        }

        internal void RenderGUI()
        {
            Device.device.Debug("  Rendering GUI");
            uiManager.Render();
        }
        
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged => Device.CreateBuffer<T>(bufferType, data, format);
        public NativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged => Device.CreateBuffer<T>(bufferType, size, format);
        
        public void Dispose()
        {
            uiManager.Dispose();
            fontManager.Dispose();
            lightManager.Dispose();
            cameraManger.Dispose();
            materialManager.Dispose();
            renderManager.Dispose();
            meshManager.Dispose();
            textureManager.Dispose();
            shaderManager.Dispose();
            entityManager.Dispose();
            Device.Dispose();
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
                    continuation();
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
