using System.Buffers;
using System.Collections;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;
using Veldrid;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public struct LowDetailData : IComponentData
{
    public byte y;
    public byte x;
}

public class LowDetailHeightMapManager : IDisposable
{
    private readonly IMeshManager meshManager;
    private readonly IMaterialManager materialManager;
    private readonly IRenderManager renderManager;
    private readonly WorldManager worldManager;
    private readonly ChunkManager chunkManager;
    private readonly CameraManager cameraManager;
    private readonly IGameContext gameContext;
    private IMesh? lowLevelMesh;
    private Material material;
    private WDL? currentWdl;
    private Entity[] wdlEntities = new Entity[64 * 64];
    private RenderLayer renderLayer;

    public LowDetailHeightMapManager(IMeshManager meshManager,
        IMaterialManager materialManager,
        IRenderManager renderManager,
        WorldManager worldManager,
        ChunkManager chunkManager,
        CameraManager cameraManager,
        IGameContext gameContext,
        IShaderManager shaderManager,
        IPipelineManager pipelineManager)
    {
        this.meshManager = meshManager;
        this.materialManager = materialManager;
        this.renderManager = renderManager;
        this.worldManager = worldManager;
        this.chunkManager = chunkManager;
        this.cameraManager = cameraManager;
        this.gameContext = gameContext;

        var shaderHandle = shaderManager.LoadShader("data/wdl.json");
        var pipeline = pipelineManager.CreatePipeline(shaderHandle, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
            RasterizerState = new RasterizerStateDescription
            {
                CullMode = FaceCullMode.Front,
                FillMode = PolygonFillMode.Solid,
                FrontFace = FrontFace.Clockwise,
                DepthClipEnabled = true,
                ScissorTestEnabled = false,
            }
        }, false);
        material = materialManager.CreateMaterial(pipeline);

        renderLayer = gameContext.Engine.RenderManager.RegisterRenderLayer("Low Level Detail Terrain");

        var groupNode = gameContext.EntityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, "Low Detail Terrain"u8);

        for (int y = 0; y < Constants.Blocks; ++y)
        {
            for (int x = 0; x < Constants.Blocks; ++x)
            {
                var entity = wdlEntities[x * Constants.Blocks + y] = gameContext.EntityManager.CreateEntity(gameContext.Archetypes.LowLevelDetailArchetype);
                gameContext.EntityManager.GetComponent<EntityName>(entity)
                    = $"WDL[{y}, {x}]";
                gameContext.EntityManager.SetParent(entity, groupNode);
                ref var renderEnableBit = ref gameContext.EntityManager.GetComponent<RenderEnabledBit>(entity);
                renderEnableBit.Layer = renderLayer.Layer;
                renderEnableBit.ForceDisableCulling();
                gameContext.EntityManager.GetComponent<LocalToWorld>(entity) = LocalToWorld.Identity;
                gameContext.Engine.EntityManager.AddArrayComponent(entity, new MeshRenderer());
                gameContext.EntityManager.GetComponent<LowDetailData>(entity) = new LowDetailData()
                {
                    y=(byte)y,
                    x=(byte)x
                };
            }
        }
    }
        
    public void Dispose()
    {
        Unload();
    }

    // Scheduled from Update (not Render): each visible WDL chunk is queued via RenderOnce so the
    // engine batches them into one shared instancing buffer (same mesh + material, only the submesh
    // differs), keeping set=1 bound once instead of rebinding per chunk.
    public void Update(float delta)
    {
        gameContext.Archetypes.LowLevelDetailArchetype.ParallelForEachState<LowDetailHeightMapManager, LowDetailData, RenderEnabledBit>(this,
            static (state, itr, thread, start, end, lowDetails, renderEnableBits) =>
            {
                for (int i = start; i < end; ++i)
                {
                    ref var lowDetail = ref lowDetails[i];
                    renderEnableBits[i]
                        .SetDisabled(state.currentWdl == null || !state.currentWdl.HasChunk(lowDetail.y, lowDetail.x) || state.chunkManager.IsTerrainLoaded(lowDetail.y, lowDetail.x));
                }
            });
    }

    public void Unload()
    {
        if (lowLevelMesh != null)
        {
            foreach (var entity in wdlEntities)
            {
                if (entity.IsEmpty())
                    continue;

                gameContext.Engine.EntityManager.GetComponent<RenderEnabledBit>(entity)
                    .SetDisabled(true);
                gameContext.Engine.EntityManager.GetArrayComponents<MeshRenderer>(entity)
                    [0].Mesh = null;
                gameContext.Engine.EntityManager.GetArrayComponents<MeshRenderer>(entity)
                    [0].SubMeshId = 0;
            }
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

        var subMesh = 0;
        for (int y = 0; y < Constants.Blocks; ++y)
        {
            for (int x = 0; x < Constants.Blocks; ++x)
            {
                var entity = wdlEntities[x * Constants.Blocks + y];

                if (!currentWdl.HasChunk(y, x))
                    continue;

                subMesh++;

                var meshRenderers = gameContext.Engine.EntityManager.GetArrayComponents<MeshRenderer>(entity);
                meshRenderers[0].Mesh = lowLevelMesh;
                meshRenderers[0].Material = material;
                meshRenderers[0].SkipDraw = false;
                meshRenderers[0].SubMeshId = subMesh - 1;
            }
        }
    }
}