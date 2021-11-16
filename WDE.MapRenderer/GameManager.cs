using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Nito.AsyncEx;
using TheEngine;
using TheEngine.Components;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.MapRenderer.Managers;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer
{
    public class GameManager : IGame, IGameContext
    {
        private IMpqArchive mpq;
        private readonly IGameView gameView;
        private readonly IDatabaseClientFileOpener databaseClientFileOpener;
        private AsyncMonitor monitor = new AsyncMonitor();
        private Engine engine;

        public GameManager(IMpqArchive mpq, IGameView gameView, IDatabaseClientFileOpener databaseClientFileOpener)
        {
            this.mpq = mpq;
            this.gameView = gameView;
            this.databaseClientFileOpener = databaseClientFileOpener;
            UpdateLoop = new UpdateManager(this);
        }
        
        public void Initialize(Engine engine)
        {
            this.engine = engine;
            TimeManager = new TimeManager(this);
            ModuleManager = new ModuleManager(this, gameView);
            DbcManager = new DbcManager(this, databaseClientFileOpener);
            CurrentMap = DbcManager.MapStore.FirstOrDefault() ?? Map.Empty;
            TextureManager = new WoWTextureManager(this);
            MeshManager = new WoWMeshManager(this);
            MdxManager = new MdxManager(this);
            WmoManager = new WmoManager(this);
            ChunkManager = new ChunkManager(this);
            CameraManager = new CameraManager(this);
            LightingManager = new LightingManager(this);
            RaycastSystem = new RaycastSystem(engine);
        }
        
        private CoroutineManager coroutineManager = new();
        
        public void StartCoroutine(IEnumerator coroutine)
        {
            coroutineManager.Start(coroutine);
        }

        private Material? prevMaterial;
        public void Update(float delta)
        {
            coroutineManager.Step();

            TimeManager.Update(delta);
            
            CameraManager.Update(delta);
            LightingManager.Update(delta);
            
            UpdateLoop.Update(delta);
            ChunkManager.Update(delta);
            ModuleManager.Update(delta);
        }

        public void Render(float delta)
        {
            ModuleManager.Render();
            LightingManager.Render();
        }

        public void SetMap(int mapId)
        {
            if (DbcManager.MapStore.Contains(mapId) && CurrentMap.Id != mapId)
            {
                CurrentMap = DbcManager.MapStore[mapId];
                ChunkManager?.UnloadAllNow();
            }
        }

        public void Dispose()
        {
            ModuleManager.Dispose();
            ChunkManager.Dispose();
            WmoManager.Dispose();
            MdxManager.Dispose();
            TextureManager.Dispose();
            MeshManager.Dispose();
        }

        public Engine Engine => engine;

        public TimeManager TimeManager { get; private set; }
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
        public Map CurrentMap { get; set; }

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