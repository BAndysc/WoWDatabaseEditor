using System.Buffers;
using System.Collections;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class LowDetailHeightMapManager : IDisposable
{
    private readonly IMeshManager meshManager;
    private readonly IMaterialManager materialManager;
    private readonly IRenderManager renderManager;
    private readonly WorldManager worldManager;
    private readonly ChunkManager chunkManager;
    private readonly CameraManager cameraManager;
    private IMesh? lowLevelMesh;
    private Material material;
    private WDL? currentWdl;
    
    public LowDetailHeightMapManager(IMeshManager meshManager,
        IMaterialManager materialManager,
        IRenderManager renderManager,
        WorldManager worldManager,
        ChunkManager chunkManager,
        CameraManager cameraManager)
    {
        this.meshManager = meshManager;
        this.materialManager = materialManager;
        this.renderManager = renderManager;
        this.worldManager = worldManager;
        this.chunkManager = chunkManager;
        this.cameraManager = cameraManager;

        material = materialManager.CreateMaterial("data/wdl.json");
    }
        
    public void Dispose()
    {
        Unload();
    }

    public void Render()
    {
        if (currentWdl == null || lowLevelMesh == null)
            return;

        int subMesh = 0;

        var currentChunk = cameraManager.Position.WoWPositionToChunk();
        var currentPosChunk = currentChunk.ChunkToWoWPosition();
        
        for (int y = 0; y < 64; ++y)
        {
            for (int x = 0; x < 64; ++x)
            {
                if (!currentWdl.HasChunk(y, x))
                    continue;

                subMesh++;

                if (chunkManager.IsTerrainLoaded(y, x))
                    continue;

                renderManager.Render(lowLevelMesh, material, subMesh - 1, Matrix4x4.Identity);
            }
        }   
    }

    public void Unload()
    {
        if (lowLevelMesh != null)
        {
            meshManager.DisposeMesh(lowLevelMesh);
            lowLevelMesh = null;
        }
    }

    public unsafe void Load()
    {
        currentWdl = worldManager.CurrentWdl;
        
        if (currentWdl == null)
            return;
        
        const int BigGrid = 17;
        const int SmallGrid = 16;
        var subVertices = ArrayPool<Vector3>.Shared.Rent((17 * 17 + 16 * 16) * currentWdl.NonEmptyChunks);
        uint[] indices = ArrayPool<uint>.Shared.Rent(3 * (BigGrid - 1) * 4 * (BigGrid - 1) * currentWdl.NonEmptyChunks);

        int globalIndex = 0;
        int globalIndexIndex = 0;
        
        List<(int indexStart, int length)> subMeshRanges = new List<(int, int)>();
        
        for (int y = 0; y < Constants.Blocks; ++y)
        {
            for (int x = 0; x < Constants.Blocks; ++x)
            {
                if (!currentWdl.HasChunk(y, x))
                    continue;

                var basePos = (y, x).ChunkToWoWPosition();
                
                ref var chunk = ref currentWdl.GetChunk(y, x);
                int k = 0;
                int baseVertexIndex = globalIndex;
                
                for (int cy = 0; cy < (BigGrid + SmallGrid); ++cy)
                {
                    for (int cx = 0; cx < (cy % 2 == 0 ? BigGrid : SmallGrid); cx++)
                    {
                        float VERTX = 0;
                        float height = 0;
                        if (cy % 2 == 0) // outer
                        {
                            VERTX = cx * 1.0f / (BigGrid - 1) * Constants.BlockSize;
                            height = chunk.OuterHeights[(cy / 2) * BigGrid + cx];
                        }
                        else // inner
                        {
                            VERTX = (Constants.BlockSize / (BigGrid - 1)) * (SmallGrid - 1) * (cx * 1.0f / (SmallGrid - 1)) + Constants.BlockSize / (BigGrid - 1) / 2;
                            height = chunk.InnerHeights[((cy - 1) / 2) * SmallGrid + cx];
                        }
                        float VERTY = cy * 1.0f / (BigGrid + SmallGrid - 1) * Constants.BlockSize;
                        var vert = new Vector3(-VERTY, -VERTX, height) + basePos;
                        subVertices[globalIndex++] = vert;
                    }
                }
                
                var globalIndexIndexStart = globalIndexIndex;
                
                for (uint cx = 0; cx < BigGrid - 1; cx++)
                {
                    for (uint cy = 0; cy < BigGrid - 1; cy++)
                    {
                        uint tl = (uint)baseVertexIndex + cy * (BigGrid + SmallGrid) + cx;
                        uint tr = tl + 1;
                        uint middle = tl + BigGrid;
                        uint bl = middle + SmallGrid;
                        uint br = bl + 1;

                        indices[globalIndexIndex++] = tl;
                        indices[globalIndexIndex++] = middle;
                        indices[globalIndexIndex++] = tr;
                        //
                        indices[globalIndexIndex++] = tl;
                        indices[globalIndexIndex++] = bl;
                        indices[globalIndexIndex++] = middle;
                        //
                        indices[globalIndexIndex++] = tr;
                        indices[globalIndexIndex++] = middle;
                        indices[globalIndexIndex++] = br;
                        //
                        indices[globalIndexIndex++] = middle;
                        indices[globalIndexIndex++] = bl;
                        indices[globalIndexIndex++] = br;
                    }
                }
                
                subMeshRanges.Add((globalIndexIndexStart, globalIndexIndex - globalIndexIndexStart));
            }
        }
        
        lowLevelMesh = meshManager.CreateMesh(subVertices, indices);
        lowLevelMesh.SetSubmeshCount(subMeshRanges.Count);
        for (int i = 0; i < subMeshRanges.Count; ++i)
        {
            lowLevelMesh.SetSubmeshIndicesRange(i, subMeshRanges[i].indexStart, subMeshRanges[i].length);
        }
    }
}