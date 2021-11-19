using System.Collections;
using System.Threading.Tasks;
using TheEngine;
using TheEngine.PhysicsSystem;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public interface IGameContext
    {
        Engine Engine { get; }
        Task<PooledArray<byte>?> ReadFile(string fileName);
        byte[]? ReadFileSync(string fileName);
        
        NotificationsCenter NotificationsCenter { get; }
        WoWMeshManager MeshManager { get; }
        WoWTextureManager TextureManager { get; }
        ChunkManager ChunkManager { get; }
        MdxManager MdxManager { get; }
        WmoManager WmoManager { get; }
        CameraManager CameraManager { get; }
        UpdateManager UpdateLoop { get; }
        RaycastSystem RaycastSystem { get; }
        DbcManager DbcManager { get; }
        LightingManager LightingManager { get; }
        TimeManager TimeManager { get; }
        ScreenSpaceSelector ScreenSpaceSelector { get; }
        Map CurrentMap { get; }
        void SetMap(int id);
        void StartCoroutine(IEnumerator coroutine);
        Task<bool> WaitForInitialized { get; }
    }
}