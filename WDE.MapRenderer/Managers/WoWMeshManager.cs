using TheEngine.Interfaces;
using WDE.MapRenderer.StaticData;

namespace WDE.MapRenderer.Managers
{
    public class WoWMeshManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private IMesh chunkMesh;

        public WoWMeshManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            chunkMesh = gameContext.Engine.MeshManager.CreateMesh(ChunkMesh.Create());
        }

        public IMesh MeshOfChunk => chunkMesh;
        
        public void Dispose()
        {
            gameContext.Engine.MeshManager.DisposeMesh(chunkMesh);
            chunkMesh = null!;
        }
    }
}