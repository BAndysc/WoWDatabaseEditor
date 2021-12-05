using TheEngine.Interfaces;
using WDE.MapRenderer.StaticData;

namespace WDE.MapRenderer.Managers
{
    public class WoWMeshManager : System.IDisposable
    {
        private readonly IMeshManager meshManager;
        private IMesh chunkMesh;

        public WoWMeshManager(IMeshManager meshManager)
        {
            this.meshManager = meshManager;
            chunkMesh = meshManager.CreateMesh(ChunkMesh.Create());
        }

        public IMesh MeshOfChunk => chunkMesh;
        
        public void Dispose()
        {
            meshManager.DisposeMesh(chunkMesh);
            chunkMesh = null!;
        }
    }
}