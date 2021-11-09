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
using WDE.Common.MPQ;
using WDE.MapRenderer.Managers;
using WDE.MpqReader;

namespace WDE.MapRenderer
{
    public class GameManager : IGame, IGameContext
    {
        private IMpqArchive mpq;
        private readonly IGameView gameView;
        private AsyncMonitor monitor = new AsyncMonitor();
        private Engine engine;

        public GameManager(IMpqArchive mpq, IGameView gameView)
        {
            this.mpq = mpq;
            this.gameView = gameView;
            CurrentMap = "Kalimdor";
            UpdateLoop = new UpdateManager(this);
        }
        
        public void Initialize(Engine engine)
        {
            this.engine = engine;
            ModuleManager = new ModuleManager(this, gameView);
            TextureManager = new WoWTextureManager(this);
            MeshManager = new WoWMeshManager(this);
            MdxManager = new MdxManager(this);
            WmoManager = new WmoManager(this);
            ChunkManager = new ChunkManager(this);
            CameraManager = new CameraManager(this);
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

            CameraManager.Update(delta);
            
            UpdateLoop.Update(delta);
            ChunkManager.Update(delta);
            ModuleManager.Update(delta);
        }

        public void Render(float delta)
        {
            ModuleManager.Render();
        }

        public void SetMap(string mapPath)
        {
            CurrentMap = mapPath;
            ChunkManager?.UnloadAllNow();
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

        public WoWMeshManager MeshManager { get; private set; }
        public WoWTextureManager TextureManager { get; private set; }
        public ChunkManager ChunkManager { get; private set; }
        public ModuleManager ModuleManager { get; private set; }
        public MdxManager MdxManager { get; private set; }
        public WmoManager WmoManager { get; private set; }
        public CameraManager CameraManager { get; private set; }
        public RaycastSystem RaycastSystem { get; private set; }
        public UpdateManager UpdateLoop { get; private set; }
        public string CurrentMap { get; set; }

        public async Task<PooledArray<byte>?> ReadFile(string fileName)
        {
            using var _ = await monitor.EnterAsync();
            var bytes = await Task.Run(() => mpq.ReadFilePool(fileName));
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }
    }
}