using System.Collections;
using TheEngine;
using WDE.MpqReader;

namespace WDE.MapRenderer.Managers
{
    public interface IGameContext
    {
        Engine Engine { get; }
        Task<PooledArray<byte>?> ReadFile(string fileName);
        
        WoWMeshManager MeshManager { get; }
        WoWTextureManager TextureManager { get; }
        ChunkManager ChunkManager { get; }
        MdxManager MdxManager { get; }
        WmoManager WmoManager { get; }
        CameraManager CameraManager { get; }
        UpdateManager UpdateLoop { get; }
        string CurrentMap { get; }
        void StartCoroutine(IEnumerator coroutine);
    }
}