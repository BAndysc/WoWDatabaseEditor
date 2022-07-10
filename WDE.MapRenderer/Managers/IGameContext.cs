using System.Collections;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MpqReader;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public interface IGameFiles
    {
        Task<PooledArray<byte>?> ReadFile(string fileName);
        PooledArray<byte>? ReadFileSyncPool(string fileName);
        byte[]? ReadFileSync(string fileName);
        byte[]? ReadFileSyncLocked(string fileName, bool silent = false);
        string Adt(string mapName, int x, int y);
        string Wdt(string mapName);
        bool Initialize();
    }
    
    public interface IGameContext
    {
        float Delta { get; }
        event Action<int>? ChangedMap;
        Map CurrentMap { get; }
        void SetMap(int id, Vector3? position = null);
        void StartCoroutine(IEnumerator coroutine);
        
        CoroutineManager CoroutineManager { get; }
        NotificationsCenter NotificationsCenter { get; }
        TimeManager TimeManager { get; }
        ScreenSpaceSelector ScreenSpaceSelector { get; }
        WoWMeshManager MeshManager { get; }
        WoWTextureManager TextureManager { get; }
        ChunkManager ChunkManager { get; }
        ModuleManager ModuleManager { get; }
        MdxManager MdxManager { get; }
        WmoManager WmoManager { get; }
        CameraManager CameraManager { get; }
        RaycastSystem RaycastSystem { get; }
        DbcManager DbcManager { get; }
        LightingManager LightingManager { get; }
        AreaTriggerManager AreaTriggerManager { get; }
        UpdateManager UpdateLoop { get; }
        WorldManager WorldManager { get; }
        LoadingManager LoadingManager { get; }
        ZoneAreaManager ZoneAreaManager { get; }
        AnimationSystem AnimationSystem { get; }
        IEntityManager EntityManager { get; }
        ITextureManager EngineTextureManager { get; }
        IMeshManager EngineMeshManager { get; }
        IMaterialManager MaterialManager { get; }
        Archetypes Archetypes { get; }
        Engine Engine { get; }
        IUIManager UiManager { get; }
    }
}