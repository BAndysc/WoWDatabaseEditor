using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;
using TheEngine.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.PhysicsSystem;
using TheEngine.Utils;
using TheMaths;
using Veldrid;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using Constants = WDE.MapRenderer.StaticData.Constants;
using WDE.MpqReader;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;
using Pipeline = TheEngine.Resources.Pipeline;

namespace WDE.MapRenderer.Managers
{
    public class ChunkInstance
    {
        public int X { get; }
        public int Z { get; }
        public ITexture splatMapTex;
        public ITexture holesMapTex;
        // Bindless terrain path only: GetBindlessIndex() stores a plain int in chunkToSplatIdx,
        // which doesn't keep the underlying texture alive - hold a strong reference here for as
        // long as the chunk is loaded, or the texture can be GC'd while still in the bindless
        // descriptor array and sampling it faults the GPU.
        public List<ITexture> bindlessSplatTextures = new();
        // Heights + chunkToSplat now live in shared STATIC set-3 global buffers (ChunkManager owns
        // them); a chunk holds only its slot start offsets and frees them on unload. -1 = unallocated.
        public int heightsOffset = -1;
        public int splatOffset = -1;
        public float[,] heights;
        public Material<ChunkManager.LitMaterialData_t>? material;
        public bool terrainLoaded;
        public CancellationTokenSource? loading = new CancellationTokenSource();
        public uint[,] areaIds = new uint[16,16];
        public Task? chunkLoading;

        // Objects (M2/WMO) load only in the inner ring; the placements are retained so a chunk can be
        // upgraded/downgraded as it crosses the ring without re-reading the ADT.
        public M2PlacementData[]? m2Placements;
        public WorldMapObjectPlacementData[]? wmoPlacements;
        public bool objectsLoaded;
        public bool modulesLoaded;
        public bool objectsBusy;            // an object load is in flight
        public bool objectsUnloading;       // a downgrade is in flight
        public Task? objectsTask;           // latest in-flight object op (load or downgrade), awaited before the next
        public CancellationTokenSource? objectsCts;

        public Entity terrainEntity;
        public Entity groupNode;
        public List<IMesh> meshes = new();
        public List<M2Id> mdx = new();
        public List<WmoId> wmos = new();

        public ChunkInstance(int x, int z)
        {
            X = x;
            Z = z;
        }

        public Vector3 MiddlePoint => ((X, Z).ChunkToWoWPosition() - new Vector3(Constants.BlockSize / 2, Constants.BlockSize / 2, 0));

        public void Dispose(ITextureManager textureManager,
            IStaticGlobalBuffer<Vector4> heightsGlobalBuffer,
            IStaticGlobalBuffer<Int4> chunkToSplatGlobalBuffer)
        {
            textureManager.DisposeTexture(splatMapTex);
            textureManager.DisposeTexture(holesMapTex);
            if (heightsOffset >= 0)
            {
                heightsGlobalBuffer.Free(heightsOffset);
                heightsOffset = -1;
            }
            if (splatOffset >= 0)
            {
                chunkToSplatGlobalBuffer.Free(splatOffset);
                splatOffset = -1;
            }
        }

        public uint GetAreaId(Vector3 wowPosition)
        {
            var chunkPosition = (X, Z).ChunkToWoWPosition();
            var relativePosition = chunkPosition - wowPosition;
            var x = Math.Clamp((int)(relativePosition.X / Constants.ChunkSize), 0, 63);
            var y = Math.Clamp((int)(relativePosition.Y / Constants.ChunkSize), 0, 63);
            return areaIds[x, y];
        }
    }

    public class ChunkManager : System.IDisposable
    {
        private readonly IEntityManager entityManager;
        private readonly IGameProperties gameProperties;
        // snapshot of IGameProperties.DontLoadDoodads (skips WMO interior doodads only; ADT M2
        // placements always load) for this view session: chunks loaded before and after a
        // mid-session toggle must agree, so the change applies when a 3D view reopens
        private readonly bool dontLoadDoodads;
        private readonly ITextureManager textureManager;
        private readonly IMeshManager meshManager;
        private readonly WoWTextureManager woWTextureManager;
        private readonly WoWMeshManager woWMeshManager;
        private readonly IMaterialManager materialManager;
        private readonly IGameFiles gameFiles;
        private readonly CameraManager cameraManager;
        private readonly IRenderManager renderManager;
        private readonly MdxManager mdxManager;
        private readonly WmoManager wmoManager;
        private readonly WorldManager worldManager;
        private readonly Lazy<LoadingManager> loadingManager;
        private readonly ModuleManager moduleManager;
        private readonly RaycastSystem raycastSystem;
        private readonly DbcManager dbcManager;
        private readonly Engine engine;
        private readonly IGameContext gameContext;
        private readonly Archetypes archetypes;
        private HashSet<(int, int)> loadedChunks = new();
        private List<ChunkInstance> chunks = new();
        private Dictionary<(int, int), ChunkInstance> chunksXY = new();

        // WMO/M2 placements are refcounted across chunks (loadedM2s/loadedWmos), so these stay flat/global
        // rather than nested per Chunk[x,z]
        private Entity chunksRoot;
        private Entity wmoGroupNode;
        private Entity m2GroupNode;

        // Per-frame render-thread budget for finalizing object spawns, shared across all concurrent chunk
        // loaders (replaces the old fixed "yield every 10 objects" pacing). Frame-keyed off FrameCount so
        // the window resets once per frame regardless of when continuations resume.
        private const double LoadBudgetMs = 4.0;
        private long loadBudgetFrame = -1;
        private long loadBudgetStart;

        // Chebyshev chunk radius from the camera's chunk: terrain loads wide, object spawns only near.
        private const int TerrainLoadRadius = 3;
        private const int ObjectsLoadRadius = 1;

        // Caps concurrent heavy loads. Loading is latency-bound (each model crosses several frame-synced
        // EnterGameLoop hops), so more in-flight overlaps that latency; the ceiling is the render thread.
        private const int MaxConcurrentPrepares = 48;
        private readonly SemaphoreSlim prepareGate = new(MaxConcurrentPrepares);

        private async ValueTask<MdxManager.MdxInstance?> PrepareM2(FileId path)
        {
            await prepareGate.WaitAsync();
            try { return await mdxManager.LoadM2Mesh(path); }
            finally { prepareGate.Release(); }
        }

        private async ValueTask<WmoManager.WmoInstance?> PrepareWmo(FileId path)
        {
            await prepareGate.WaitAsync();
            try { return await wmoManager.LoadWorldMapObject(path); }
            finally { prepareGate.Release(); }
        }

        private bool LoadBudgetExceeded()
        {
            long frame = engine.FrameCount;
            if (frame != loadBudgetFrame)
            {
                loadBudgetFrame = frame;
                loadBudgetStart = Stopwatch.GetTimestamp();
                return false;
            }
            double ms = (Stopwatch.GetTimestamp() - loadBudgetStart) * 1000.0 / Stopwatch.Frequency;
            return ms >= LoadBudgetMs;
        }

        private Entity EnsureChunksRoot()
        {
            if (chunksRoot == Entity.Empty)
                chunksRoot = entityManager.CreateEntity(archetypes.GroupArchetype, "Chunks"u8);
            return chunksRoot;
        }

        private Entity EnsureWmoGroupNode()
        {
            if (wmoGroupNode == Entity.Empty)
            {
                wmoGroupNode = entityManager.CreateEntity(archetypes.GroupArchetype, "WMO"u8);
                entityManager.SetParent(wmoGroupNode, EnsureChunksRoot());
            }
            return wmoGroupNode;
        }

        private Entity EnsureM2GroupNode()
        {
            if (m2GroupNode == Entity.Empty)
            {
                m2GroupNode = entityManager.CreateEntity(archetypes.GroupArchetype, "M2"u8);
                entityManager.SetParent(m2GroupNode, EnsureChunksRoot());
            }
            return m2GroupNode;
        }

        private bool renderGrid;
        private bool RenderGrid
        {
            get => renderGrid;
            set
            {
                if (renderGrid == value)
                    return;
                
                renderGrid = value;
                foreach (var chunk in chunks)
                {
                    if (chunk.material == null)
                        continue;
                    // preserve the per-tile heights/splat offsets; only toggle showGrid
                    LitMaterialData_t data = chunk.material.MaterialData;
                    data.showGrid = value ? 1 : 0;
                    chunk.material.SetMaterialData(ref data);
                }
            }
        }

        private bool renderTerrain = true;
        public bool RenderTerrain
        {
            get => renderTerrain;
            set
            {
                if (renderTerrain == value)
                    return;
                renderTerrain = value;
                foreach (var chunk in chunks)
                    if (chunk.terrainEntity != Entity.Empty)
                        chunk.terrainEntity.SetForceDisabledRendering(entityManager, !value);
            }
        }

        private void PosToChunkHeightCoords(Vector3 wowPosition, out (int, int) chunk, out int xIndex, out int yIndex)
        {
            chunk = wowPosition.WoWPositionToChunk();
            var chunkInitPos = chunk.ChunkToWoWPosition();
            var posWithInChunk = chunkInitPos - wowPosition;
            int xInt = 533 - (int)Math.Clamp(posWithInChunk.X, 0, 533);
            int yInt = 533 - (int)Math.Clamp(posWithInChunk.Y, 0, 533);
            int maxIndex = Constants.ChunksInBlockX * 9 - 1;
            xIndex = Math.Clamp((int)(xInt / 533.0 * maxIndex), 0, maxIndex);
            yIndex = Math.Clamp((int)(yInt / 533.0 * maxIndex), 0, maxIndex);
        }
        
        private List<(Entity, Vector3)> outRaycast = new();
        public float? HeightAtPosition(float x, float y, float? closestToZ)
        {
            var origin = new Vector3(x, y, closestToZ ?? 4000);
            raycastSystem.RaycastAll(new Ray(origin.WithZ(4000), Vectors.Down), origin, outRaycast, Collisions.COLLISION_MASK_STATIC);
            if (outRaycast.Count > 0)
            {
                float minDiff = float.MaxValue;
                float bestHeight = 0;
                for (int i = 0; i < outRaycast.Count; ++i)
                {
                    var diff = Math.Abs(outRaycast[i].Item2.Z - origin.Z);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        bestHeight = outRaycast[i].Item2.Z;
                    }
                }
                outRaycast.Clear();
                return bestHeight;
            }

            return null;

            // var ret = raycastSystem.Raycast(new Ray(new Vector3(x, y, 4000), Vector3.Down), null, false, Collisions.COLLISION_MASK_STATIC);
            // if (ret.HasValue)
            //     return ret.Value.Item2.Z;
            // return null;
        }
        
        private float? FastHeightAtPosition(float x, float y)
        {
            Vector3 wowPos = new Vector3(x, y, 0);
            PosToChunkHeightCoords(wowPos, out var chunk, out int xIndex, out int yIndex);

            if (!chunksXY.TryGetValue(chunk, out var c))
                return null;

            return c.heights[xIndex, yIndex];
        }

        private ShaderHandle shader;
        private Pipeline pipeline;

        // Static set-3 global buffers shared by every loaded terrain tile (one slot per tile). Bindings
        // must match lit.vert: 3 = heightsNormal, 4 = chunkToSplat. Each tile's vertex data is a fixed
        // size, so the slab allocator hands out uniform slots; the slot start offset is baked into the
        // tile's LitMaterialData_t (heightsOffset / splatOffset).
        private const uint HeightsGlobalBinding = 3;
        private const uint ChunkToSplatGlobalBinding = 4;
        private const int HeightsSlotElements = Constants.VerticesInChunk * Constants.ChunksInBlock;
        private const int ChunkToSplatSlotElements = Constants.ChunksInBlock;
        private readonly IStaticGlobalBuffer<Vector4> heightsGlobalBuffer;
        private readonly IStaticGlobalBuffer<Int4> chunkToSplatGlobalBuffer;

        public ChunkManager(IEntityManager entityManager,
            IGameProperties gameProperties,
            ITextureManager textureManager,
            IMeshManager meshManager,
            WoWTextureManager woWTextureManager,
            WoWMeshManager woWMeshManager,
            IMaterialManager materialManager,
            IGameFiles gameFiles,
            CameraManager cameraManager,
            IRenderManager renderManager,
            MdxManager mdxManager,
            WmoManager wmoManager,
            WorldManager worldManager,
            IGameContext gameContext,
            Archetypes archetypes,
            Lazy<LoadingManager> loadingManager,
            ModuleManager moduleManager,
            RaycastSystem raycastSystem,
            DbcManager dbcManager,
            Engine engine)
        {
            this.entityManager = entityManager;
            this.gameProperties = gameProperties;
            dontLoadDoodads = gameProperties.DontLoadDoodads;
            this.textureManager = textureManager;
            this.meshManager = meshManager;
            this.woWTextureManager = woWTextureManager;
            this.woWMeshManager = woWMeshManager;
            this.materialManager = materialManager;
            this.gameFiles = gameFiles;
            this.cameraManager = cameraManager;
            this.renderManager = renderManager;
            this.mdxManager = mdxManager;
            this.wmoManager = wmoManager;
            this.worldManager = worldManager;
            this.gameContext = gameContext;
            this.archetypes = archetypes;
            this.loadingManager = loadingManager;
            this.moduleManager = moduleManager;
            this.raycastSystem = raycastSystem;
            this.dbcManager = dbcManager;
            this.engine = engine;

            shader = engine.ShaderManager.LoadShader("data/lit.json");
            pipeline = engine.PipelineManager.CreatePipeline(shader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleDisabled,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = RasterizerStateDescription.Front
            }, false);

            // TerrainLoadRadius gives a (2r+1)^2 = 49-tile window; size the slab for that plus a little
            // slack so the common case never reallocs (it still grows on demand if exceeded).
            const int initialTileSlots = 64;
            heightsGlobalBuffer = engine.CreateStaticGlobalBuffer<Vector4>(HeightsGlobalBinding, HeightsSlotElements, initialTileSlots);
            chunkToSplatGlobalBuffer = engine.CreateStaticGlobalBuffer<Int4>(ChunkToSplatGlobalBinding, ChunkToSplatSlotElements, initialTileSlots);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct LitMaterialData_t
        {
            public int showGrid;
            public int heightsOffset;  // start offset of this tile's slot in heightsGlobalBuffer (set 3, binding 3)
            public int splatOffset;    // start offset of this tile's slot in chunkToSplatGlobalBuffer (set 3, binding 4)
            public BindlessTextureId splatTexIndex;  // bindless index of the per-tile splat/alpha atlas
            public BindlessTextureId holesTexIndex;  // bindless index of the per-tile holes atlas
            public int padding0;
            public int padding1;
            public int padding2;
        };

        public ValueTask LoadChunk(int y, int x, bool now)
        {
            if (y < 0 || y >= 64 || x < 0 || x >= 64)
                return ValueTask.CompletedTask;

            if (!loadedChunks.Add((y, x)))
                return ValueTask.CompletedTask;

            return LoadChunkImpl(y, x, now);
        }

        public async ValueTask LoadChunkImpl(int y, int x, bool now)
        {
            ChunkInstance chunk = new(y, x);
            var tasksource = new TaskCompletionSource();
            chunk.chunkLoading = tasksource.Task;
            chunks.Add(chunk);
            chunksXY[(y, x)] = chunk;

            try
            {
                await LoadChunkBody(chunk, y, x, tasksource);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while loading chunk [{y}, {x}] - tile abandoned");
                Console.WriteLine(e);
            }
            finally
            {
                // chunkLoading MUST complete no matter what: UnloadChunk awaits it unconditionally,
                // so a load that dies (corrupt ADT/BLP, ...) with the task left incomplete wedges
                // unloading and with it the whole world load. Completion happens on the game loop
                // because awaiters resume inline on the completing thread (as on the normal path).
                if (!tasksource.Task.IsCompleted)
                {
                    await engine.EnterGameLoop;
                    tasksource.TrySetResult();
                }
            }
        }

        private async ValueTask LoadChunkBody(ChunkInstance chunk, int y, int x, TaskCompletionSource tasksource)
        {
            var cancelationToken = chunk.loading.Token;

            chunk.groupNode = entityManager.CreateEntity(archetypes.GroupArchetype, $"Chunk[{chunk.X}, {chunk.Z}]");
            entityManager.SetParent(chunk.groupNode, EnsureChunksRoot());

            var WDTflag = worldManager.CurrentWdt?.Header.flags;


            string fullName, fullNameTex0, fullNameObj0, fullNameLod;
            unsafe
            {
                if (gameContext.CurrentMap == null)
                {
                    throw new Exception();
                }
                fullName = gameFiles.Adt(gameContext.CurrentMap->Directory, x, y);
                fullNameTex0 = gameFiles.AdtTex0(gameContext.CurrentMap->Directory, x, y);
                fullNameObj0 = gameFiles.AdtObj0(gameContext.CurrentMap->Directory, x, y);
                fullNameLod = gameFiles.AdtLod0(gameContext.CurrentMap->Directory, x, y);
            }
            var file = await gameFiles.ReadFile(fullName);
            var fileTex0 = await gameFiles.ReadFile(fullNameTex0, true);
            var fileObj0 = await gameFiles.ReadFile(fullNameObj0, true);
            var fileLod = await gameFiles.ReadFile(fullNameLod, true);
            if (file == null)
            {
                tasksource.SetResult();
                return;
            }

            await engine.EnterThreadPool;

            chunk.heights = new float[Constants.ChunksInBlockX * 9, Constants.ChunksInBlockY * 9];

            // All ChunksInBlock sub-chunk splat/holes maps are merged into ONE 2D texture per tile
            // (a 16x16 grid of the per-chunk cells), sampled bindlessly with a sub-UV computed from
            // ChunkId - so terrain has just two ordinary bindless 2D textures per tile instead of a
            // set-1 sampler2DArray. See lit.frag atlasUV(): the chunk-local UV is clamped + half-texel
            // inset so linear filtering never crosses a cell seam (no bleed; matches the old per-layer
            // ClampToEdge exactly).
            const int splatCell = 64, holesCell = 4;
            int splatAtlasW = Constants.ChunksInBlockX * splatCell; // 1024
            int holesAtlasW = Constants.ChunksInBlockX * holesCell; // 64
            Rgba32[] splatAtlas = new Rgba32[splatAtlasW * splatAtlasW];
            Rgba32[] holesAtlas = new Rgba32[holesAtlasW * holesAtlasW];
            Vector4[] heightsNormal = null!;
            Int4[] chunkToSplatIdxBindless = null!;
            ADT adt = null!;

            heightsNormal = new Vector4[1 * Constants.VerticesInChunk * Constants.ChunksInBlock];
            chunkToSplatIdxBindless = new Int4[Constants.ChunksInBlock];

            try
            {
                adt = new ADT(gameFiles.WoWVersion, new MemoryBinaryReader(file),
                    fileTex0 == null ? null : new MemoryBinaryReader(fileTex0),
                    fileObj0 == null ? null : new MemoryBinaryReader(fileObj0),
                    fileLod == null ? null : new MemoryBinaryReader(fileLod), WDTflag,
                    dbcManager.LiquidObjectStore, dbcManager.LiquidTypeStore, dbcManager.LiquidMaterialStore);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while loading ADT " + fullName);
                Console.WriteLine(e);
                adt = null;
                throw;
            }
            finally
            {
                file.Dispose();
                fileTex0?.Dispose();
                fileObj0?.Dispose();
                fileLod?.Dispose();
            }

            if (adt == null)
                return;

            await engine.EnterGameLoop;

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            int k = 0;
            int k2 = 0;
            using var chunksEnumerator2 = ((IEnumerable<AdtChunk>)adt.Chunks).GetEnumerator();
            for (int i = 0; i < Constants.ChunksInBlockY; ++i)
            {
                for (int j = 0; j < Constants.ChunksInBlockX; ++j)
                {
                    // this chunk's cell in the per-tile atlas. ChunkId = i*16+j (enumeration order),
                    // so the shader's cell = (ChunkId%16, ChunkId/16) = (j, i) - keep these in sync.
                    int splatCellX = j * splatCell, splatCellY = i * splatCell;
                    int holesCellX = j * holesCell, holesCellY = i * holesCell;

                    if (!chunksEnumerator2.MoveNext())
                    {
                        throw new Exception("Unexpected end of chunks");
                    }

                    chunk.areaIds[i, j] = chunksEnumerator2.Current.AreaId;
                    var basePos = chunksEnumerator2.Current.BasePosition;
                    int k_ = 0;
                    var subVertices = ArrayPool<Vector3>.Shared.Rent(145);
                    for (int cy = 0; cy < 17; ++cy)
                    {
                        for (int cx = 0; cx < (cy % 2 == 0 ? 9 : 8); cx++)
                        {
                            float VERTX = 0;
                            if (cy % 2 == 0)
                            {
                                VERTX = cx / 8.0f * Constants.ChunkSize;
                            }
                            else
                            {
                                VERTX = (Constants.ChunkSize / 8) * 7 * (cx / 7.0f) + Constants.ChunkSize / 8 / 2;
                            }
                            float VERTY = cy / 16.0f * Constants.ChunkSize;
                            var vert = new Vector3(-VERTY, -VERTX, chunksEnumerator2.Current.Heights[k_]) + basePos;
                            subVertices[k_] = vert;

                            if (cy % 2 == 0) // inner row, 8 verts
                            {
                                int yIndex2 = (15 - j) * 9 + 8 - cx;
                                int xIndex2 =  (15 - i) * 9 + 8 - cy/2;
                                chunk.heights[xIndex2, yIndex2] = vert.Z;
                            }
                            vert.Z.MinMax(ref minHeight, ref maxHeight);
                            
                            var norm = chunksEnumerator2.Current.Normals[k_];
                            heightsNormal[k++] = new Vector4(
                                norm.X,
                                norm.Y,
                                norm.Z,
                                chunksEnumerator2.Current.Heights[k_] + basePos.Z
                            );
                            k_++;
                        }
                    }

                    ushort[] indices = ArrayPool<ushort>.Shared.Rent(4 * 8 * 8 * 4);
                    int k__ = 0;
                    for (uint cx = 0; cx < 8; cx++)
                    {
                        for (uint cy = 0; cy < 8; cy++)
                        {
                            uint tl = cy * 17 + cx;
                            uint tr = tl + 1;
                            uint middle = tl + 9;
                            uint bl = middle + 8;
                            uint br = bl + 1;

                            if (br > ushort.MaxValue)
                                throw new Exception("Too many vertices");

                            indices[k__++] = (ushort)tl;
                            indices[k__++] = (ushort)middle;
                            indices[k__++] = (ushort)tr;
                            //
                            indices[k__++] = (ushort)tl;
                            indices[k__++] = (ushort)bl;
                            indices[k__++] = (ushort)middle;
                            //
                            indices[k__++] = (ushort)tr;
                            indices[k__++] = (ushort)middle;
                            indices[k__++] = (ushort)br;
                            //
                            indices[k__++] = (ushort)middle;
                            indices[k__++] = (ushort)bl;
                            indices[k__++] = (ushort)br;
                        }
                    }
                    var subChunkMesh = meshManager.CreateManagedOnlyMesh(subVertices.AsSpan(0, 145), indices.AsSpan(0, 4 * 8 * 8 * 4));
                    ArrayPool<Vector3>.Shared.Return(subVertices);
                    ArrayPool<ushort>.Shared.Return(indices);
                    var entity = entityManager.CreateEntity(archetypes.CollisionOnlyArchetype, "Terrain collider"u8);
                    entityManager.GetComponent<LegacyCollider>(entity).CollisionMask = Collisions.COLLISION_MASK_TERRAIN;
                    entityManager.GetComponent<LocalToWorld>(entity).Matrix = Matrix.Identity;
                    var meshRenderer = new MeshRenderer() { Mesh = subChunkMesh, SubMeshId = 0 };
                    entityManager.AddArrayComponent(entity, meshRenderer);
                    entityManager.GetComponent<WorldMeshBounds>(entity) = (WorldMeshBounds)subChunkMesh.Bounds;
                    entityManager.SetParent(entity, chunk.groupNode);

                    // holes cells default to 0 (no hole) since holesAtlas is zero-initialized
                    if (chunksEnumerator2.Current.Holes != null)
                    {
                        for (int hx = 0; hx < 4; hx++)
                        {
                            for (int hy = 0; hy < 4; hy++)
                            {
                                holesAtlas[(holesCellX + hx) + (holesCellY + hy) * holesAtlasW] =
                                    new Rgba32(chunksEnumerator2.Current.Holes[hx, hy] ? 255 : 0, 0, 0);
                            }
                        }
                    }

                    var sm = chunksEnumerator2.Current.SplatMap;
                    var len = sm?.GetLength(2) ?? 0;
                    for (int _x = 0; _x < 64; ++_x)
                    {
                        for (int _y = 0; _y < 64; ++_y)
                        {
                            var col = new Rgba32(len >= 1 ? sm[_x, _y, 0] : (byte)255,
                                len >= 2 ? sm[_x, _y, 1] : (byte)0,
                                len >= 3 ? sm[_x, _y, 2] : (byte)0,
                                chunksEnumerator2.Current.ShadowMap != null && chunksEnumerator2.Current.ShadowMap[_x, _y] ? (byte)255 : (byte)0);
                            var left = 255 - col.B;
                            col.G = (byte)Math.Min(col.G, left);
                            left -= col.G;
                            col.R = (byte)Math.Min(col.R, left);
                            splatAtlas[(splatCellX + _x) + (splatCellY + _y) * splatAtlasW] = col;
                        }
                    }
                }
            }

            if (cancelationToken.IsCancellationRequested)
            {
                tasksource.SetResult();
                return;
            }

            int chnk = 0;
            var material = materialManager.CreateMaterial<LitMaterialData_t>(pipeline);
            chunk.material = material;
            // material data (incl. the global-buffer slot offsets) is written below, once the slots
            // are allocated and the per-tile heights/splat data has been uploaded.

            using var chunksEnumerator = ((IEnumerable<AdtChunk>)adt.Chunks).GetEnumerator();
            chunksEnumerator.MoveNext();
            var chunkMesh = woWMeshManager.MeshOfChunk;
            var t = new Transform();
            t.Position = new Vector3(chunksEnumerator.Current.BasePosition.X, chunksEnumerator.Current.BasePosition.Y, 0);
            t.Scale = new Vector3(1);
        
            // one merged 2D atlas texture each, sampled bindlessly (indices baked into the material
            // data below). SetFiltering/SetWrapping must precede GetBindlessIndex (the sampler slot
            // is resolved from the texture's current state).
            chunk.splatMapTex = textureManager.CreateTexture(new Rgba32[][] { splatAtlas }, splatAtlasW, splatAtlasW, false);
            textureManager.SetFiltering(chunk.splatMapTex, FilteringMode.Linear);
            textureManager.SetWrapping(chunk.splatMapTex, WrapMode.ClampToEdge);
            chunk.holesMapTex = textureManager.CreateTexture(new Rgba32[][] { holesAtlas }, holesAtlasW, holesAtlasW, false);
            textureManager.SetFiltering(chunk.holesMapTex, FilteringMode.Nearest);
            textureManager.SetWrapping(chunk.holesMapTex, WrapMode.ClampToEdge);

            int emptyBindlessIndex = textureManager.GetBindlessIndex(woWTextureManager.EmptyTexture);
            Dictionary<string, int> textureToBindlessIndex = new();

            for (int i = 0; i < Constants.ChunksInBlockX; ++i)
            {
                for (int j = 0; j < Constants.ChunksInBlockY; ++j)
                {
                    int? r = null;
                    int? g = null;
                    int? b = null;
                    int? a = null;
                    int defaultSlot = emptyBindlessIndex;
                    foreach (var splat in chunksEnumerator.Current.Splats)
                    {
                        if (splat.TextureId >= adt.Textures.Length)
                        {
                            if (r == null)
                                r = defaultSlot;
                            else if (g == null)
                                g = defaultSlot;
                            else if (b == null)
                                b = defaultSlot;
                            else if (a == null)
                                a = defaultSlot;
                            continue;
                        }
                        var texturePath = adt.Textures[(int)splat.TextureId];
                        int slot;
                        if (!textureToBindlessIndex.TryGetValue(texturePath, out slot))
                        {
                            var splatTex = await woWTextureManager.GetTexture(texturePath);
                            slot = textureManager.GetBindlessIndex(splatTex);
                            textureToBindlessIndex[texturePath] = slot;
                            chunk.bindlessSplatTextures.Add(splatTex);
                        }

                        if (r == null)
                            r = slot;
                        else if (g == null)
                            g = slot;
                        else if (b == null)
                            b = slot;
                        else if (a == null)
                            a = slot;
                    }

                    chunkToSplatIdxBindless[chnk] = new Int4(r ?? defaultSlot, g ?? defaultSlot, b ?? defaultSlot, a ?? defaultSlot);

                    chnk++;
                    chunksEnumerator.MoveNext();
                }
            }


            // Upload this tile's vertex data into shared static set-3 global buffers and bake the slot
            // start offsets into the material. Allocate + GetSpan + copy run as one synchronous block:
            // the game loop is single-threaded, so no concurrent loader can trigger a Grow (which would
            // invalidate the mapped span) between Allocate and the copy.
            chunk.heightsOffset = heightsGlobalBuffer.Allocate();
            heightsNormal.AsSpan().CopyTo(heightsGlobalBuffer.GetSpan(chunk.heightsOffset));
            chunk.splatOffset = chunkToSplatGlobalBuffer.Allocate();
            chunkToSplatIdxBindless.AsSpan().CopyTo(chunkToSplatGlobalBuffer.GetSpan(chunk.splatOffset));

            LitMaterialData_t data = new()
            {
                showGrid = gameProperties.ShowGrid ? 1 : 0,
                heightsOffset = chunk.heightsOffset,
                splatOffset = chunk.splatOffset,
                splatTexIndex = textureManager.GetBindlessIndex(chunk.splatMapTex),
                holesTexIndex = textureManager.GetBindlessIndex(chunk.holesMapTex),
            };
            material.SetMaterialData(ref data);

            //chunk.terrainHandle = renderManager.RegisterDynamicRenderer(chunkMesh.Handle, material, 0, t);
            
            var terrainEntity = entityManager.CreateEntity(archetypes.TerrainEntityArchetype, "Terrain renderer"u8);
            entityManager.GetComponent<LocalToWorld>(terrainEntity).Matrix = t.LocalToWorldMatrix;
            entityManager.AddArrayComponent(terrainEntity, new MeshRenderer() { Mesh = chunkMesh, SubMeshId = 0, Material = material, Opaque = !material.BlendingEnabled });
            var localBounds = new BoundingBox(
                chunkMesh.Bounds.Minimum with { Z = minHeight },
                chunkMesh.Bounds.Maximum with { Z = maxHeight });
            entityManager.GetComponent<WorldMeshBounds>(terrainEntity) = RenderManager.LocalToWorld((MeshBounds)localBounds, new LocalToWorld() { Matrix = t.LocalToWorldMatrix });
            terrainEntity.SetForceDisabledRendering(entityManager, !RenderTerrain);
            chunk.terrainEntity = terrainEntity;
            entityManager.SetParent(terrainEntity, chunk.groupNode);
            chunk.terrainLoaded = true;

            // water here
            if (adt.HasLiquid)
            {
                float adtposx = t.Position.X;
                float adtposy = t.Position.Y;

                bool allWaterLayersHaveEqualSize = true;
                float? waterHeight = null;

                for (int chunkY = 0; chunkY < Constants.ChunksInBlockY; ++chunkY) // for each subchunk
                {
                    for (int chunkX = 0; chunkX < Constants.ChunksInBlockX; ++chunkX)
                    {
                        var liquidChunk = adt.MH2OLiquidChunks[chunkY * Constants.ChunksInBlockX + chunkX];
                        if (liquidChunk.LayerCount != 1 ||
                            !liquidChunk.LiquidInstances![0].IsSingleHeight ||
                            (waterHeight.HasValue &&
                            Math.Abs(liquidChunk.LiquidInstances[0].MinHeightLevel - waterHeight.Value) > float.Epsilon))
                        {
                            allWaterLayersHaveEqualSize = false;
                            break;
                        }

                        waterHeight = liquidChunk.LiquidInstances[0].MinHeightLevel;
                    }
                }
                
                IMesh waterMesh;
                Vector3 meshPosition = Vector3.Zero;
                if (allWaterLayersHaveEqualSize)
                {
                    var merged = woWMeshManager.GenerateGiganticWaterMesh(waterHeight!.Value);
                    waterMesh = meshManager.CreateMesh(merged.Item1, merged.Item2);
                    meshPosition = new Vector3(adtposx, adtposy, 0);
                }
                else
                {
                    List<Vector3[]> verticesSet = new List<Vector3[]>();
                    List<ushort[]> indicesSet = new List<ushort[]>();
                    List<Vector3> offsets = new List<Vector3>();
                    
                    for (int chunkY = 0; chunkY < Constants.ChunksInBlockY; ++chunkY) // for each subchunk
                    {
                        for (int chunkX = 0; chunkX < Constants.ChunksInBlockX; ++chunkX)
                        {
                            var liquidChunk = adt.MH2OLiquidChunks[chunkY * Constants.ChunksInBlockX + chunkX];
                            var chunkposx = adtposx - ( (chunkY) * Constants.ChunkSize);
                            var chunkposy = adtposy - ( (chunkX) * Constants.ChunkSize);
                        
                            if (liquidChunk.LiquidInstances == null)
                                continue;

                            for (var index = 0; index < liquidChunk.LiquidInstances.Length; index++)
                            {
                                var (vertices, indices) = woWMeshManager.GenerateWaterMesh(ref liquidChunk.LiquidInstances[index]);
                                verticesSet.Add(vertices);
                                indicesSet.Add(indices);
                                offsets.Add(new Vector3(chunkposx, chunkposy, 0));
                            }
                        }
                    }
                    var merged = MeshBatcher.MergeMeshes(verticesSet, indicesSet, offsets);
                    waterMesh = meshManager.CreateMesh(merged.Item1, merged.Item2);
                }

                var trsMatrix = Utilities.TRS(meshPosition, Quaternion.Identity, Vectors.One);
                var waterEntity = entityManager.CreateEntity(archetypes.TerrainEntityArchetype, "Water renderer"u8);
                entityManager.GetComponent<LocalToWorld>(waterEntity).Matrix = trsMatrix;
                var meshRenderer = new MeshRenderer() { Mesh = waterMesh, SubMeshId = 0, Material = woWMeshManager.WaterMaterial, Opaque = false };
                entityManager.AddArrayComponent(waterEntity, meshRenderer);
                localBounds = waterMesh.Bounds;
                localBounds = localBounds.WithSize(localBounds.Size with { Z = Math.Max(localBounds.Size.Z, 10) });
                var worldBounds = RenderManager.LocalToWorld((MeshBounds)localBounds, new LocalToWorld() { Matrix = trsMatrix });
                entityManager.GetComponent<WorldMeshBounds>(waterEntity) = worldBounds;

                entityManager.SetParent(waterEntity, chunk.groupNode);
                chunk.meshes.Add(waterMesh);
            }


            /////////////

            if (cancelationToken.IsCancellationRequested)
            {
                tasksource.SetResult();
                return;
            }

            // Retain placements so objects can be spawned later (only in the inner ring; not here).
            chunk.m2Placements = adt.M2Objects;
            chunk.wmoPlacements = adt.WorldMapObjects;

            tasksource.SetResult();
        }

        private async ValueTask LoadModules(ChunkInstance chunk, CancellationToken cancellationToken)
        {
            ValueTask LoadModuleChunk(IGameModule arg)
            {
                return arg.LoadChunk(gameContext.CurrentMapId, chunk.X, chunk.Z, cancellationToken);
            }

            await moduleManager.ForEach(LoadModuleChunk);
        }

        private async ValueTask LoadObjects(ChunkInstance chunk, CancellationToken cancellationToken)
        {
            // modules belong to the inner ring too; load them first so they're up before the M2/WMO spawns
            if (!chunk.modulesLoaded)
            {
                await LoadModules(chunk, cancellationToken);
                chunk.modulesLoaded = true;
            }

            if (chunk.wmoPlacements != null)
                await LoadWorldMapObjects(chunk.wmoPlacements, chunk, cancellationToken);

            if (chunk.m2Placements != null)
                await LoadM2(chunk.m2Placements, chunk, cancellationToken);
        }

        // Safe to call every frame; no-ops until terrain is ready and while an object op is in flight.
        private void EnsureChunkObjects(ChunkInstance chunk)
        {
            if (chunk.objectsLoaded || chunk.objectsBusy || chunk.objectsUnloading || !chunk.terrainLoaded || chunk.loading == null)
                return;
            chunk.objectsBusy = true;
            // linked to chunk.loading so a full unload cancels objects too; cancellable alone for downgrade
            chunk.objectsCts = CancellationTokenSource.CreateLinkedTokenSource(chunk.loading.Token);
            chunk.objectsTask = LoadChunkObjects(chunk).AsTask();
        }

        private async ValueTask LoadChunkObjects(ChunkInstance chunk)
        {
            var token = chunk.objectsCts!.Token;
            try
            {
                if (!token.IsCancellationRequested)
                    await LoadObjects(chunk, token);
                if (!token.IsCancellationRequested)
                    chunk.objectsLoaded = true;
            }
            finally
            {
                chunk.objectsBusy = false;
            }
        }

        // Downgrade: drop this chunk's object spawns but keep its terrain (it left the inner ring).
        private void BeginUnloadChunkObjects(ChunkInstance chunk)
        {
            if (chunk.objectsUnloading)
                return;
            chunk.objectsUnloading = true;
            var inFlightLoad = chunk.objectsBusy ? chunk.objectsTask : null;
            chunk.objectsTask = DowngradeChunkObjects(chunk, inFlightLoad).AsTask();
        }

        private async ValueTask DowngradeChunkObjects(ChunkInstance chunk, Task? inFlightLoad)
        {
            try
            {
                chunk.objectsCts?.Cancel();
                if (inFlightLoad != null)
                    await inFlightLoad;

                ReleaseChunkObjects(chunk);
                await UnloadChunkModules(chunk);
                chunk.objectsCts?.Dispose();
                chunk.objectsCts = null;
                chunk.objectsLoaded = false;
            }
            finally
            {
                chunk.objectsUnloading = false;
            }
        }

        private void ReleaseChunkObjects(ChunkInstance chunk)
        {
            foreach (var m2Id in chunk.mdx)
                ReleaseM2(m2Id);
            chunk.mdx.Clear();
            foreach (var wmoId in chunk.wmos)
                ReleaseWmo(wmoId);
            chunk.wmos.Clear();
        }

        // idempotent via modulesLoaded (called from both downgrade and full unload)
        private async ValueTask UnloadChunkModules(ChunkInstance chunk)
        {
            if (!chunk.modulesLoaded)
                return;
            chunk.modulesLoaded = false;
            await moduleManager.ForEach(arg => arg.UnloadChunk(chunk.X, chunk.Z));
        }

        private Dictionary<M2Id, LoadedM2> loadedM2s = new();

        private record struct LoadedM2
        {
            public M2Id Id;
            public int RefCount;
            public Entity Entity;

            public LoadedM2(M2Id id, int refCount)
            {
                Id = id;
                RefCount = refCount;
            }
        }

        // Decrements an M2's refcount, destroying it (and its child lights) at zero. Game-thread only.
        private void ReleaseM2(M2Id id)
        {
            if (!loadedM2s.TryGetValue(id, out var loadedM2))
                return;
            if (loadedM2.RefCount <= 1)
            {
                loadedM2s.Remove(id);
                if (loadedM2.Entity != Entity.Empty)
                    entityManager.DestroyEntity(loadedM2.Entity);
            }
            else
                loadedM2s[id] = loadedM2 with { RefCount = loadedM2.RefCount - 1 };
        }

        private async ValueTask LoadM2(M2PlacementData[] m2Objects, ChunkInstance chunk, CancellationToken cancellationToken)
        {
            // Stage 1: dedup and start every new M2's load concurrently (overlapping their await-chains);
            // the refcount is registered up front so a sibling chunk loading the same placement dedups.
            var pending = new List<(M2PlacementData m2, ValueTask<MdxManager.MdxInstance?> task)>(m2Objects.Length);
            foreach (var m2 in m2Objects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (loadedM2s.TryGetValue(m2.Id, out var refCount))
                {
                    loadedM2s[m2.Id] = refCount with { RefCount = refCount.RefCount + 1 };
                    continue;
                }
                loadedM2s[m2.Id] = new LoadedM2(m2.Id, 1);
                pending.Add((m2, PrepareM2(m2.M2Path)));
            }

            // Stage 2: finalize under the shared budget; roll back the refcount of any entry that never
            // finalizes (cancelled/null/failed) so an aborted load doesn't leak a model.
            for (int i = 0; i < pending.Count; i++)
            {
                var (m2, task) = pending[i];
                if (cancellationToken.IsCancellationRequested)
                {
                    for (int k = i; k < pending.Count; k++)
                        ReleaseM2(pending[k].m2.Id);
                    return;
                }

                var m = await task;
                if (m == null)
                {
                    Console.WriteLine(m2.M2Path + " is null");
                    ReleaseM2(m2.Id);
                    continue;
                }
                chunk.mdx.Add(m2.Id);

                var t = new Transform();
                t.Position = new Vector3(32 * Constants.BlockSize - m2.AbsolutePosition.Z, (32 * Constants.BlockSize - m2.AbsolutePosition.X), m2.AbsolutePosition.Y);
                t.Scale = Vector3.One * m2.Scale;
                t.Rotation = Utilities.FromEuler(m2.Rotation.X, m2.Rotation.Y + 180,  m2.Rotation.Z);

                var entity = SpawnM2Instance(m, t.LocalToWorldMatrix, m2.Id.ToString(), EnsureM2GroupNode());

                if (entity != Entity.Empty)
                {
                    var loadedM2 = loadedM2s[m2.Id];
                    loadedM2.Entity = entity;
                    loadedM2s[m2.Id] = loadedM2;
                    entityManager.GetComponent<Adt_M2Object>(entity) = new Adt_M2Object()
                    {
                        AbsolutePosition = m2.AbsolutePosition,
                        Rotation = m2.Rotation,
                        Scale = m2.Scale,
                        Flags = m2.Flags,
                        Id = m2.Id
                    };
                }

                if (LoadBudgetExceeded())
                    await engine.NextFrame;
            }
        }

        // Spawns a single static M2 instance at the given world matrix, parented to `parent`.
        // Handles animation slot allocation, per-material renderers and the model's own lights.
        // Returns Entity.Empty if the model has no materials.
        private Entity SpawnM2Instance(MdxManager.MdxInstance m, in Matrix worldMatrix, string name, Entity parent)
        {
            Entity entity = Entity.Empty;
            int animBoneBase = 0, animColorBase = 0, animTexBase = 0;
            if (m.HasAnimations)
            {
                (animBoneBase, animColorBase, animTexBase) = gameContext.AnimationSystem.AllocateAnimationSlots(m.model);
            }

            bool first = true;

            foreach (var material in m.materials)
            {
                if (first)
                {
                    entity = entityManager.CreateEntity(
                        m.HasAnimations ? archetypes.StaticM2WorldObjectAnimatedArchetype : archetypes.StaticM2WorldObjectArchetype,
                        name);

                    if (m.HasAnimations)
                    {
                        entityManager.SetManagedComponent(entity, new M2AnimationComponentData(m.model)
                        {
                            SetNewAnimation = 0,
                            BoneBase = animBoneBase,
                            ColorBase = animColorBase,
                            TexTransformBase = animTexBase,
                            _boneCache = AnimationSystem.IdentityMatrix(m.model.bones.Length).ToArray(),
                            _colorCache = AnimationSystem.IdentityColors(m.model.colors.Length).ToArray(),
                            _texTransformCache = AnimationSystem.IdentityMatrix(m.model.texture_transforms.Length + 1).ToArray(),
                        });
                    }
                }
                var instanceData = new Int4(
                    material.batch.colorIndex < 0 ? -1 : animColorBase + material.batch.colorIndex,
                    material.batch.textureTransformIndex < 0 ? -1 : animTexBase + material.batch.textureTransformIndex,
                    material.batch.textureTransformIndex2 < 0 ? -1 : animTexBase + material.batch.textureTransformIndex2,
                    animBoneBase);
                renderManager.SetupRendererEntity(entity, m.mesh.Handle, material.material, material.submesh, worldMatrix, instanceData);

                first = false;
            }

            if (entity != Entity.Empty)
            {
                entityManager.SetParent(entity, parent);
                entityManager.SetManagedComponent(entity, new MdxRenderer(m){ Owner = entity });

                if (m.model.lights.Length > 0)
                {
                    int i = 0;
                    entityManager.AddComponent(entity, new DirtyPosition()); // normally it was static M2, but light makes it a parent, so add DirtyPosition
                    foreach (var light in m.model.lights)
                    {
                        var lightEntity = entityManager.CreateEntity(archetypes.M2PointLightsArchetype);

                        entityManager.GetComponent<EntityName>(lightEntity)
                            = $"M2 Light[{name}, {i}]";

                        entityManager.SetParent(lightEntity, entity);

                        entityManager.GetComponent<CopyParentTransform>(lightEntity)
                            .Parent = entity;

                        entityManager.GetComponent<CopyParentTransform>(lightEntity)
                            .Local = Matrix.CreateTranslation(light.position);

                        entityManager.SetManagedComponent(lightEntity, new AnimatedM2Light()
                        {
                            M2 = m.model,
                            M2Light = light
                        });
                        i++;
                    }
                }
            }

            return entity;
        }

        private Dictionary<WmoId, LoadedWmo> loadedWmos = new();

        private record struct LoadedWmo
        {
            public WmoId Id;
            public int RefCount;
            public Entity Entity;
            public Entity ColliderEntity;

            public LoadedWmo(WmoId id, int refCount)
            {
                Id = id;
                RefCount = refCount;
            }
        }

        // Decrements a WMO's refcount, destroying it (collider + lights cascade) at zero. Game-thread only.
        private void ReleaseWmo(WmoId id)
        {
            if (!loadedWmos.TryGetValue(id, out var loadedWmo))
                return;
            if (loadedWmo.RefCount <= 1)
            {
                loadedWmos.Remove(id);
                if (loadedWmo.Entity != Entity.Empty)
                    entityManager.DestroyEntity(loadedWmo.Entity);
            }
            else
                loadedWmos[id] = loadedWmo with { RefCount = loadedWmo.RefCount - 1 };
        }

        private async ValueTask LoadWorldMapObjects(WorldMapObjectPlacementData[] wmoObjects, ChunkInstance chunk,
            CancellationToken cancellationToken)
        {
            // Stage 1: dedup and start every new WMO's load concurrently (see LoadM2 for the pattern).
            var pending = new List<(WorldMapObjectPlacementData wmoRef, Matrix transform, ValueTask<WmoManager.WmoInstance?> task)>(wmoObjects.Length);
            foreach (var wmoReference in wmoObjects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (loadedWmos.TryGetValue(wmoReference.Id, out var loadedWmo))
                {
                    loadedWmos[wmoReference.Id] = loadedWmo with { RefCount = loadedWmo.RefCount + 1 };
                    continue;
                }

                loadedWmos[wmoReference.Id] = new LoadedWmo(wmoReference.Id, 1);

                var wmoTransform = new Transform();
                wmoTransform.Position = new Vector3((32 * Constants.BlockSize - wmoReference.AbsolutePosition.Z), (32 * Constants.BlockSize - wmoReference.AbsolutePosition.X), wmoReference.AbsolutePosition.Y);
                wmoTransform.Rotation = Utilities.FromEuler(wmoReference.Rotation.X,  wmoReference.Rotation.Y + 180, wmoReference.Rotation.Z);
                pending.Add((wmoReference, wmoTransform.LocalToWorldMatrix, PrepareWmo(wmoReference.WmoPath)));
            }

            // Stage 2: finalize each prepared WMO on the render thread under the shared per-frame budget.
            for (int p = 0; p < pending.Count; p++)
            {
                var (wmoReference, wmoMatrix, task) = pending[p];
                if (cancellationToken.IsCancellationRequested)
                {
                    for (int k = p; k < pending.Count; k++)
                        ReleaseWmo(pending[k].wmoRef.Id);
                    return;
                }

                var wmoInstance = await task;
                if (wmoInstance == null)
                {
                    ReleaseWmo(wmoReference.Id);
                    continue;
                }

                chunk.wmos.Add(wmoReference.Id);
                Entity entityCollider = entityManager.CreateEntity(archetypes.CollisionOnlyArchetype, $"{wmoReference.Id} collider");
                var entity = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype, $"{wmoReference.Id}");

                engine.EntityManager.GetComponent<LocalToWorld>(entity) = new LocalToWorld() { Matrix = wmoMatrix };
                entityManager.GetComponent<LocalToWorld>(entityCollider).Matrix = wmoMatrix;
                entityManager.GetComponent<LegacyCollider>(entityCollider).CollisionMask = Collisions.COLLISION_MASK_WMO;

                // re-read: a sibling chunk may have incremented the refcount since stage 1
                loadedWmos[wmoReference.Id] = loadedWmos[wmoReference.Id] with { Entity = entity, ColliderEntity = entityCollider };

                entityManager.SetParent(entity, EnsureWmoGroupNode());
                entityManager.SetParent(entityCollider, entity);

                if (wmoInstance.wmoData.Lights != null)
                {
                    var lights = wmoInstance.wmoData.Lights;
                    int i = 0;
                    foreach (var light in lights)
                    {
                        var lightEntity = entityManager.CreateEntity(archetypes.PointLightsArchetype);

                        entityManager.GetComponent<EntityName>(lightEntity)
                            = $"WMO Light[{wmoReference.Id}, {i}]";

                        entityManager.SetParent(lightEntity, entity);

                        ref var lightData = ref entityManager.GetComponent<Light>(lightEntity);
                        lightData.Type = LightType.Point;
                        lightData.AttenuationStart = light.attenStart;
                        lightData.AttenuationEnd = light.attenEnd;
                        lightData.Intensity = light.intensity;
                        lightData.Color = new Vector4(light.color.r / 255.0f, light.color.g / 255.0f, light.color.b / 255.0f, light.color.a / 255.0f);

                        entityManager.GetComponent<LocalToWorld>(lightEntity)
                            .Matrix = Matrix.CreateTranslation(light.position) * wmoMatrix;
                        i++;
                    }
                }

                foreach (var mesh in wmoInstance.Meshes)
                {
                    int i = 0;
                    foreach (var material in mesh.Item2)
                    {
                        entity.SetRenderer(entityManager, mesh.Item1, i++, material);

                        if (!material.BlendingEnabled)
                        {
                            entityCollider.SetRenderer(entityManager, mesh.Item1, i - 1, null);
                        }
                    }
                }

                if (LoadBudgetExceeded())
                    await engine.NextFrame;

                // doodad set 0 (always-loaded), parented to the WMO entity so it cascade-destroys; no refcount
                if (!dontLoadDoodads)
                    await LoadWmoDoodads(wmoInstance, wmoReference, entity, wmoMatrix, cancellationToken);
            }
        }

        private async ValueTask LoadWmoDoodads(WmoManager.WmoInstance wmoInstance, WorldMapObjectPlacementData wmoReference,
            Entity wmoEntity, Matrix wmoMatrix, CancellationToken cancellationToken)
        {
            var doodads = wmoInstance.wmoData.DoodadsDefinition;
            if (doodads == null || doodads.Length == 0)
                return;

            var sets = wmoInstance.wmoData.DoodadSets;
            uint setStart = 0;
            uint setCount = (uint)doodads.Length;
            if (sets != null && sets.Length > 0)
            {
                setStart = sets[0].FirstInstanceIndex;
                setCount = sets[0].Count;
            }
            uint setEnd = Math.Min(setStart + setCount, (uint)doodads.Length);
            if (setEnd <= setStart)
                return;

            // Prepare all doodad meshes concurrently, then finalize under the shared frame budget.
            var pending = new List<(uint d, ValueTask<MdxManager.MdxInstance?> task)>((int)(setEnd - setStart));
            for (uint d = setStart; d < setEnd; ++d)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                pending.Add((d, PrepareM2(doodads[d].M2Path)));
            }

            foreach (var (d, task) in pending)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var doodadMesh = await task;
                if (doodadMesh == null)
                    continue;

                ref readonly var doodad = ref doodads[d];
                var doodadLocal = Matrix.CreateScale(doodad.Scale)
                    * Matrix.CreateFromQuaternion(doodad.Rotation)
                    * Matrix.CreateTranslation(doodad.Position);

                SpawnM2Instance(doodadMesh, doodadLocal * wmoMatrix,
                    $"{wmoReference.Id} doodad {d}", wmoEntity);

                if (LoadBudgetExceeded())
                    await engine.NextFrame;
            }
        }

        public void Dispose()
        {
            foreach (var c in chunks)
                c.Dispose(textureManager, heightsGlobalBuffer, chunkToSplatGlobalBuffer);
        }

        public void Update(float delta)
        {
            if (loadingManager.Value.EssentialLoadingInProgress)
                return;

            if (!gameProperties.LoadWorld)
                return;

            RenderGrid = gameProperties.ShowGrid;

            (int x, int y) cam = cameraManager.CurrentChunk;
            for (int i = -TerrainLoadRadius; i <= TerrainLoadRadius; ++i)
            {
                for (int j = -TerrainLoadRadius; j <= TerrainLoadRadius; ++j)
                {
                    int cy = cam.x + i;
                    int cx = cam.y + j;
                    LoadChunk(cy, cx, false).FireAndForget();

                    if (Math.Abs(i) <= ObjectsLoadRadius && Math.Abs(j) <= ObjectsLoadRadius
                        && chunksXY.TryGetValue((cy, cx), out var inner))
                        EnsureChunkObjects(inner);
                }
            }

            UnloadChunks();
        }

        private void UnloadChunks()
        {
            (int x, int y) cam = cameraManager.CurrentChunk;
            for (var index = chunks.Count - 1; index >= 0; index--)
            {
                var c = chunks[index];
                int cheb = Math.Max(Math.Abs(c.X - cam.x), Math.Abs(c.Z - cam.y));
                if (cheb > TerrainLoadRadius)
                {
                    chunksXY.Remove((c.X, c.Z));
                    loadedChunks.Remove((c.X, c.Z));
                    chunks.Remove(c);
                    UnloadChunk(c).FireAndForget();
                }
                else if (cheb > ObjectsLoadRadius && (c.objectsLoaded || c.objectsBusy) && !c.objectsUnloading)
                {
                    BeginUnloadChunkObjects(c); // left the inner ring: drop objects, keep terrain
                }
            }
        }

        private async ValueTask UnloadChunk(ChunkInstance chunk)
        {
            // also cancels the linked objectsCts; await both so nothing finalizes after teardown
            chunk.loading?.Cancel();
            if (chunk.chunkLoading != null)
                await chunk.chunkLoading;
            if (chunk.objectsTask != null)
                await chunk.objectsTask;

            await UnloadChunkModules(chunk);

            chunk.terrainEntity = Entity.Empty;
            // terrain/water renderers and terrain colliders are all parented to chunk.groupNode, so this cascades and destroys them too
            entityManager.DestroyEntity(chunk.groupNode);
            foreach (var obj in chunk.meshes)
                meshManager.DisposeMesh(obj);
            ReleaseChunkObjects(chunk);

            chunk.objectsCts?.Dispose();
            chunk.objectsCts = null;
            chunk.loading?.Dispose();
            chunk.loading = null;
            chunk.Dispose(textureManager, heightsGlobalBuffer, chunkToSplatGlobalBuffer);
        }

        public async ValueTask UnloadAllChunks()
        {
            var chunksCopy = chunks.ToList();
            chunks.Clear();
            loadedChunks.Clear();
            chunksXY.Clear();
            await Task.WhenAll(chunksCopy.Select(x => UnloadChunk(x).AsTask()).ToList());
        }

        public bool IsLoaded(int y, int x)
        {
            return loadedChunks.Contains((y, x));
        }

        public bool IsTerrainLoaded(int y, int x)
        {
            return chunksXY.TryGetValue((y, x), out var chunk) && chunk.terrainLoaded;
        }
    }
}
