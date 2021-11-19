using System;
using System.Collections;
using System.Diagnostics;
using Nito.AsyncEx;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.Entities;
using TheEngine.PhysicsSystem;
using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer
{
    [AutoRegister]
    public class GameManager : IGame, IGameContext
    {
        private readonly IMpqService mpqService;
        private readonly IGameView gameView;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseClientFileOpener databaseClientFileOpener;
        private AsyncMonitor monitor = new AsyncMonitor();
        private Engine engine;
        public event Action? OnInitialized;
        public event Action? OnFailedInitialize;

        public GameManager(IMpqService mpqService, 
            IGameView gameView,
            IMessageBoxService messageBoxService,
            IDatabaseClientFileOpener databaseClientFileOpener)
        {
            this.mpqService = mpqService;
            this.gameView = gameView;
            this.messageBoxService = messageBoxService;
            this.databaseClientFileOpener = databaseClientFileOpener;
            UpdateLoop = new UpdateManager(this);
        }
        
        private bool TryOpenMpq(out IMpqArchive m)
        {
            try
            {
                m = mpqService.Open();
                return true;
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Invalid MPQ")
                    .SetMainInstruction("Couldn't parse game MPQ.")
                    .SetContent(e.Message + "\n\nAre you using modified game files?")
                    .WithButton("Ok", false)
                    .Build());
                m = null;
                return false;
            }
        }
        
        public bool Initialize(Engine engine)
        {
            this.engine = engine;
            if (!TryOpenMpq(out mpq))
            {
                OnFailedInitialize?.Invoke();
                waitForInitialized.SetResult(false);
                waitForInitialized = new();
                return false;
            }
            coroutineManager = new();
            TimeManager = new TimeManager(this);
            ScreenSpaceSelector = new ScreenSpaceSelector(this);
            DbcManager = new DbcManager(this, databaseClientFileOpener);
            CurrentMap = DbcManager.MapStore.FirstOrDefault(m => m.Id == 1) ?? Map.Empty;
            TextureManager = new WoWTextureManager(this);
            MeshManager = new WoWMeshManager(this);
            MdxManager = new MdxManager(this);
            WmoManager = new WmoManager(this);
            ChunkManager = new ChunkManager(this);
            CameraManager = new CameraManager(this);
            LightingManager = new LightingManager(this);
            RaycastSystem = new RaycastSystem(engine);
            ModuleManager = new ModuleManager(this, gameView); // must be last
            
            OnInitialized?.Invoke();
            IsInitialized = true;
            waitForInitialized.SetResult(true);
            return true;
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            coroutineManager.Start(coroutine);
        }

        private TaskCompletionSource<bool> waitForInitialized = new();
        public Task<bool> WaitForInitialized => waitForInitialized.Task;

        private Material? prevMaterial;
        public void Update(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }
            coroutineManager.Step();

            TimeManager.Update(delta);
            
            CameraManager.Update(delta);
            LightingManager.Update(delta);
            
            ScreenSpaceSelector.Update(delta);
            UpdateLoop.Update(delta);
            ChunkManager.Update(delta);
            ModuleManager.Update(delta);
        }

        public void Render(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }
            ModuleManager.Render();
            LightingManager.Render();
            ScreenSpaceSelector.Render();
        }

        public event Action? RequestDispose;

        public void SetMap(int mapId)
        {
            if (DbcManager.MapStore.Contains(mapId) && CurrentMap.Id != mapId)
            {
                CurrentMap = DbcManager.MapStore[mapId];
                ChunkManager?.UnloadAllNow();
            }
        }

        public void DoDispose()
        {
            if (IsInitialized)
                RequestDispose?.Invoke();
            Debug.Assert(!IsInitialized);
        }

        public void DisposeGame()
        {
            if (!IsInitialized)
                return;
            waitForInitialized = new();
            IsInitialized = false;
            ModuleManager.Dispose();
            LightingManager.Dispose();
            ChunkManager.Dispose();
            WmoManager.Dispose();
            MdxManager.Dispose();
            TextureManager.Dispose();
            MeshManager.Dispose();
            mpq.Dispose();
            mpq = null!;
            coroutineManager = null!;
            TimeManager = null!;
            ScreenSpaceSelector = null!;
            DbcManager = null!;
            CurrentMap = null!;
            TextureManager = null!;
            MeshManager = null!;
            MdxManager = null!;
            WmoManager = null!;
            ChunkManager = null!;
            CameraManager = null!;
            LightingManager = null!;
            RaycastSystem = null!;
            ModuleManager = null!;
        }

        public Engine Engine => engine;

        private IMpqArchive mpq;
        private CoroutineManager coroutineManager;
        public TimeManager TimeManager { get; private set; }
        public ScreenSpaceSelector ScreenSpaceSelector { get; private set; }
        public WoWMeshManager MeshManager { get; private set; }
        public WoWTextureManager TextureManager { get; private set; }
        public ChunkManager ChunkManager { get; private set; }
        public ModuleManager ModuleManager { get; private set; }
        public MdxManager MdxManager { get; private set; }
        public WmoManager WmoManager { get; private set; }
        public CameraManager CameraManager { get; private set; }
        public RaycastSystem RaycastSystem { get; private set; }
        public DbcManager DbcManager { get; private set; }
        public LightingManager LightingManager { get; private set; }
        public UpdateManager UpdateLoop { get; private set; }
        public Map CurrentMap { get; private set; }
        public bool IsInitialized { get; private set; }

        public async Task<PooledArray<byte>?> ReadFile(string fileName)
        {
            using var _ = await monitor.EnterAsync();
            var bytes = await Task.Run(() => mpq.ReadFilePool(fileName));
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }

        public byte[]? ReadFileSync(string fileName)
        {
            using var _ = monitor.Enter();
            var bytes = mpq.ReadFile(fileName);
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }
    }
}