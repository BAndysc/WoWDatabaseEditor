using System.Collections;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.MPQ;
using WDE.MpqReader;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public interface IGameFiles
    {
        Task<PooledArray<byte>?> ReadFile(FileId fileId, bool silent = false, int? maxReadBytes = null);
        PooledArray<byte>? ReadFileSyncPool(FileId fileId);
        byte[]? ReadFileSync(FileId fileId);
        byte[]? ReadFileSyncLocked(FileId fileId, bool silent = false);
        string Adt(string mapName, int x, int y);
        string AdtTex0(string mapName, int x, int y);
        string AdtObj0(string mapName, int x, int y);
        string AdtLod0(string mapName, int x, int y);
        string Wdt(string mapName);
        string Wdl(string mapName);
        bool Initialize();
        GameFilesVersion WoWVersion { get; }
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