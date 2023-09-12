using System.Buffers;
using System.Collections;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class ChunkInstance
    {
        public int X { get; }
        public int Z { get; }
        public TextureHandle splatMapTex;
        public TextureHandle holesMapTex;
        public NativeBuffer<VectorByte4>? chunkToSplatBuffer;
        public NativeBuffer<Vector4>? heightsNormalBuffer;
        public float[,] heights;
        public Material? material;
        public bool terrainLoaded;
        public CancellationTokenSource? loading = new CancellationTokenSource();
        public uint[,] areaIds = new uint[16,16];
        public Task? chunkLoading;

        public Entity terrainEntity;
        public List<StaticRenderHandle> renderHandles = new();
        public List<Entity> entities = new();
        public List<IMesh> meshes = new();
        public List<NativeBuffer<Matrix>> animationBuffers = new();
        
        public ChunkInstance(int x, int z)
        {
            X = x;
            Z = z;
        }

        public Vector3 MiddlePoint => ((X, Z).ChunkToWoWPosition() - new Vector3(Constants.BlockSize / 2, Constants.BlockSize / 2, 0));

        public void Dispose(ITextureManager textureManager)
        {
            chunkToSplatBuffer?.Dispose();
            textureManager.DisposeTexture(splatMapTex);
            textureManager.DisposeTexture(holesMapTex);
            heightsNormalBuffer?.Dispose();
            foreach (var buffer in animationBuffers)
                buffer.Dispose();
            animationBuffers.Clear();
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
                    chunk.material?.SetUniformInt("showGrid", value ? 1 : 0);
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
        }

        public IEnumerator LoadChunk(int y, int x, bool now)
        {
            if (y < 0 || y >= 64 || x < 0 || x >= 64)
                yield break;
            
            if (!loadedChunks.Add((y, x)))
                yield break;
            
            ChunkInstance chunk = new(y, x);
            var cancelationToken = chunk.loading.Token;
            var tasksource = new TaskCompletionSource();
            chunk.chunkLoading = tasksource.Task;
            chunks.Add(chunk);
            chunksXY[(y, x)] = chunk;

            var WDTflag = worldManager.CurrentWdt?.Header.flags;

            var fullName = gameFiles.Adt(gameContext.CurrentMap.Directory, x, y);
            var fullNameTex0 = gameFiles.AdtTex0(gameContext.CurrentMap.Directory, x, y);
            var fullNameObj0 = gameFiles.AdtObj0(gameContext.CurrentMap.Directory, x, y);
            var fullNameLod = gameFiles.AdtLod0(gameContext.CurrentMap.Directory, x, y);
            var file = gameFiles.ReadFile(fullName);
            var fileTex0 = gameFiles.ReadFile(fullNameTex0, true);
            var fileObj0 = gameFiles.ReadFile(fullNameObj0, true);
            var fileLod = gameFiles.ReadFile(fullNameLod, true);
            yield return file;
            yield return fileTex0;
            yield return fileObj0;
            yield return fileLod;
            if (file.Result == null)
            {
                tasksource.SetResult();
                yield return LoadModules(chunk, cancelationToken);
                chunk.loading = null;
                yield break;
            }

            chunk.heights = new float[Constants.ChunksInBlockX * 9, Constants.ChunksInBlockY * 9];

            using var splatMapArrayGenerator = new GroupedArrayPooler<Rgba32>(Constants.ChunksInBlock);
            Rgba32[][][] splatMaps = new Rgba32[Constants.ChunksInBlock][][];
            Rgba32[][][] holesMaps = new Rgba32[Constants.ChunksInBlock][][];
            Vector4[] heightsNormal = null!;
            VectorByte4[] chunkToSplatIdx = null!;
            Dictionary<string, int> textureToSlot = null!;
            ADT adt = null!;

            yield return Task.Run(() =>
            {
                heightsNormal = new Vector4[1 * Constants.VerticesInChunk * Constants.ChunksInBlock];
                chunkToSplatIdx = new VectorByte4[Constants.ChunksInBlock];
                textureToSlot = new();

                try
                {
                    adt = new ADT( gameFiles.WoWVersion, new MemoryBinaryReader(file.Result), 
                        fileTex0.Result == null ? null : new MemoryBinaryReader(fileTex0.Result),
                        fileObj0.Result == null ? null : new MemoryBinaryReader(fileObj0.Result),
                        fileLod.Result == null ? null : new MemoryBinaryReader(fileLod.Result), WDTflag,
                        dbcManager.LiquidObjectStore, dbcManager.LiquidTypeStore, dbcManager.LiquidMaterialStore);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception while loading ADT " + fullName);
                    Console.WriteLine(e);
                    adt = null;
                    throw;
                }
            });
            file.Result.Dispose();
            fileTex0.Result?.Dispose();
            fileObj0.Result?.Dispose();
            fileLod.Result?.Dispose();

            if (adt == null)
                yield break;
            
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            int k = 0;
            int k2 = 0;
            using var chunksEnumerator2 = ((IEnumerable<AdtChunk>)adt.Chunks).GetEnumerator();
            for (int i = 0; i < Constants.ChunksInBlockY; ++i)
            {
                for (int j = 0; j < Constants.ChunksInBlockX; ++j)
                {
                    var splatMap = splatMapArrayGenerator.Get(64 * 64);

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
                    var entity = entityManager.CreateEntity(archetypes.CollisionOnlyArchetype);
                    entityManager.GetComponent<Collider>(entity).CollisionMask = Collisions.COLLISION_MASK_TERRAIN;
                    entityManager.GetComponent<LocalToWorld>(entity).Matrix = Matrix.Identity;
                    entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = 0;
                    entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = subChunkMesh.Handle;
                    entityManager.GetComponent<WorldMeshBounds>(entity) = (WorldMeshBounds)subChunkMesh.Bounds;
                    chunk.entities.Add(entity);

                    Rgba32[] holeMap = new Rgba32[4 * 4];
                    if (chunksEnumerator2.Current.Holes != null)
                    {
                        for (int hx = 0; hx < 4; hx++)
                        {
                            for (int hy = 0; hy < 4; hy++)
                            {
                                holeMap[hx + hy * 4] =
                                    new Rgba32(chunksEnumerator2.Current.Holes[hx, hy] ? 255 : 0, 0, 0);
                            }
                        }
                    }
                
                    int _i = 0;
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
                            splatMap[(_x) + (_y) * 64] = col;
                        }
                    }

                    splatMaps[i * 16 + j] = new Rgba32[1][] { splatMap };
                    holesMaps[i * 16 + j] = new Rgba32[1][] { holeMap };
                }
            }

            if (cancelationToken.IsCancellationRequested)
            {
                tasksource.SetResult();
                chunk.loading = null;
                yield break;
            }
        
            int chnk = 0;
            var material = materialManager.CreateMaterial("data/lit.json");
            chunk.material = material;
            material.SetUniformInt("showGrid", gameProperties.ShowGrid ? 1 : 0);

            using var chunksEnumerator = ((IEnumerable<AdtChunk>)adt.Chunks).GetEnumerator();
            chunksEnumerator.MoveNext();
            var chunkMesh = woWMeshManager.MeshOfChunk;
            var t = new Transform();
            t.Position = new Vector3(chunksEnumerator.Current.BasePosition.X, chunksEnumerator.Current.BasePosition.Y, 0);
            t.Scale = new Vector3(1);
            chunkMesh.Activate();
        
            chunk.splatMapTex = textureManager.CreateTextureArray(splatMaps, 64, 64);
            textureManager.SetFiltering(chunk.splatMapTex, FilteringMode.Linear);
            textureManager.SetWrapping(chunk.splatMapTex, WrapMode.ClampToEdge);
            chunk.holesMapTex = textureManager.CreateTextureArray(holesMaps, 4, 4);
            textureManager.SetFiltering(chunk.holesMapTex, FilteringMode.Nearest);
            material.SetTexture("texture1", chunk.splatMapTex);
            material.SetTexture("holes", chunk.holesMapTex);
        
            for (int i = 0; i < Constants.ChunksInBlockX; ++i)
            {
                for (int j = 0; j < Constants.ChunksInBlockY; ++j)
                {
                    int? r = null;
                    int? g = null;
                    int? b = null;
                    int? a = null;
                    foreach (var splat in chunksEnumerator.Current.Splats)
                    {
                        if (splat.TextureId >= adt.Textures.Length)
                        {
                            if (r == null)
                                r = 0;
                            else if (g == null)
                                g = 0;
                            else if (b == null)
                                b = 0;
                            else if (a == null)
                                a = 0;
                            continue;
                        }
                        var texturePath = adt.Textures[(int)splat.TextureId];
                        if (!textureToSlot.ContainsKey(texturePath))
                        {
                            var tcs = new TaskCompletionSource<TextureHandle>();
                            yield return woWTextureManager.GetTexture(texturePath, tcs);
                            var splatTex = tcs.Task.Result;
                            if (textureToSlot.Count <= 13)
                                material.SetTexture("_tex" + (textureToSlot.Count), splatTex);
                            textureToSlot[texturePath] = textureToSlot.Count;
                        }

                        if (r == null)
                            r = textureToSlot[texturePath];
                        else if (g == null)
                            g = textureToSlot[texturePath];
                        else if (b == null)
                            b = textureToSlot[texturePath];
                        else if (a == null)
                            a = textureToSlot[texturePath];
                    }

                    chunkToSplatIdx[chnk] = new VectorByte4((byte)(r ?? 0), (byte)(g ?? 0), (byte)(b ?? 0),
                        (byte)(a ?? 0));

                    chnk++;
                    chunksEnumerator.MoveNext();
                }
            }

            for (int j = textureToSlot.Count; j <= 13; ++j)
            {
                material.SetTexture("_tex" + j, woWTextureManager.EmptyTexture);
            }
        

            chunk.chunkToSplatBuffer = engine.CreateBuffer<VectorByte4>(BufferTypeEnum.StructuredBufferVertexOnly, chunkToSplatIdx, BufferInternalFormat.Byte4);
            chunk.heightsNormalBuffer = engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBufferVertexOnly, heightsNormal, BufferInternalFormat.Float4);// new NativeBuffer<Vector4>(device.device, BufferTypeEnum.StructuredBufferVertexOnly, heightsNormal);
        
            material.SetBuffer("chunkToSplat", chunk.chunkToSplatBuffer);
            material.SetBuffer("heightsNormalBuffer", chunk.heightsNormalBuffer);

            //chunk.terrainHandle = renderManager.RegisterDynamicRenderer(chunkMesh.Handle, material, 0, t);
            
            var terrainEntity = entityManager.CreateEntity(archetypes.TerrainEntityArchetype);
            entityManager.GetComponent<LocalToWorld>(terrainEntity).Matrix = t.LocalToWorldMatrix;
            entityManager.GetComponent<MeshRenderer>(terrainEntity).SubMeshId = 0;
            entityManager.GetComponent<MeshRenderer>(terrainEntity).MaterialHandle = material.Handle;
            entityManager.GetComponent<MeshRenderer>(terrainEntity).MeshHandle = chunkMesh.Handle; 
            entityManager.GetComponent<MeshRenderer>(terrainEntity).Opaque = !material.BlendingEnabled; 
            var localBounds = new BoundingBox(
                new Vector3(chunkMesh.Bounds.Minimum.X, chunkMesh.Bounds.Minimum.Y, minHeight),
                new Vector3(chunkMesh.Bounds.Maximum.X, chunkMesh.Bounds.Maximum.Y, maxHeight));
            entityManager.GetComponent<WorldMeshBounds>(terrainEntity) = RenderManager.LocalToWorld((MeshBounds)localBounds, new LocalToWorld() { Matrix = t.LocalToWorldMatrix });
            terrainEntity.SetForceDisabledRendering(entityManager, !RenderTerrain);
            chunk.terrainEntity = terrainEntity;
            chunk.entities.Add(terrainEntity);
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
                var waterEntity = entityManager.CreateEntity(archetypes.TerrainEntityArchetype);
                entityManager.GetComponent<LocalToWorld>(waterEntity).Matrix = trsMatrix;
                            
                entityManager.GetComponent<MeshRenderer>(waterEntity).SubMeshId = 0;
                entityManager.GetComponent<MeshRenderer>(waterEntity).MaterialHandle = woWMeshManager.WaterMaterial.Handle;
                entityManager.GetComponent<MeshRenderer>(waterEntity).MeshHandle = waterMesh.Handle;
                entityManager.GetComponent<MeshRenderer>(waterEntity).Opaque = false;
                localBounds = waterMesh.Bounds;
                localBounds = localBounds.WithSize(localBounds.Size with { Z = Math.Max(localBounds.Size.Z, 10) });
                var worldBounds = RenderManager.LocalToWorld((MeshBounds)localBounds, new LocalToWorld() { Matrix = trsMatrix });
                entityManager.GetComponent<WorldMeshBounds>(waterEntity) = worldBounds;

                chunk.entities.Add(waterEntity);
                chunk.meshes.Add(waterMesh);
            }


            /////////////

            if (cancelationToken.IsCancellationRequested)
            {
                tasksource.SetResult();
                chunk.loading = null;
                yield break;
            }
            
            yield return LoadObjects(adt, chunk, cancelationToken);

            yield return LoadModules(chunk, cancelationToken);
            
            tasksource.SetResult();
            chunk.loading = null; 
        }

        private IEnumerator LoadModules(ChunkInstance chunk, CancellationToken cancellationToken)
        {
            IEnumerator LoadModuleChunk(IGameModule arg)
            {
                yield return arg.LoadChunk(gameContext.CurrentMap.Id, chunk.X, chunk.Z, cancellationToken);
            }
            
            yield return moduleManager.ForEach(LoadModuleChunk);
        }

        private IEnumerator LoadObjects(ADT adt, ChunkInstance chunk, CancellationToken cancellationToken)
        {
            yield return LoadWorldMapObjects(adt, chunk, cancellationToken);

            yield return LoadM2(adt, chunk, cancellationToken);
        }

        private IEnumerator LoadM2(ADT adt, ChunkInstance chunk, CancellationToken cancellationToken)
        {
            int index = 0;
            foreach (var m2 in adt.M2Objects)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                TaskCompletionSource<MdxManager.MdxInstance?> result = new();
                
                yield return mdxManager.LoadM2Mesh(m2.M2Path, result);
                if (result.Task.Result == null)
                {
                    Console.WriteLine(m2.M2Path + " is null");
                    continue;
                }
                var m = result.Task.Result;

                var t = new Transform();
                t.Position = new Vector3(32 * Constants.BlockSize - m2.AbsolutePosition.Z, (32 * Constants.BlockSize - m2.AbsolutePosition.X), m2.AbsolutePosition.Y);
                t.Scale = Vector3.One * m2.Scale;
                t.Rotation = Utilities.FromEuler(m2.Rotation.X, m2.Rotation.Y + 180,  m2.Rotation.Z);

                Entity entity;
                NativeBuffer<Matrix>? bones = null;
                if (m.HasAnimations)
                {
                    bones = engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Float4);
                    bones.UpdateBuffer(AnimationSystem.IdentityBones(m.model.bones.Length).Span);
                    chunk.animationBuffers.Add(bones);
                }

                bool first = true;

                foreach (var material in m.materials)
                {
                    if (m.HasAnimations)
                    {
                        entity = entityManager.CreateEntity(first ? archetypes.StaticM2WorldObjectAnimatedMasterArchetype : archetypes.StaticM2WorldObjectAnimatedArchetype);
                        // only one renderer has to update the animation, because the animation is the same among all renderers
                        if (first)
                        {
                            entityManager.SetManagedComponent(entity, new M2AnimationComponentData(m.model)
                            {
                                SetNewAnimation = 0,
                                _buffer = bones!
                            });
                        }
                        var instanceRenderer = new MaterialInstanceRenderData();
                        instanceRenderer.SetBuffer(material.material, "boneMatrices", bones!);
                        entityManager.SetManagedComponent(entity, instanceRenderer);
                    }
                    else
                        entity = entityManager.CreateEntity(archetypes.StaticM2WorldObjectArchetype);
                    
                    renderManager.SetupRendererEntity(entity, m.mesh.Handle, material.material, material.submesh, t.LocalToWorldMatrix);
                    
                    chunk.entities.Add(entity);
                    first = false;
                }

                index++;
                
                if (index % 10 == 0)
                    yield return null;
            }
        }

        private IEnumerator LoadWorldMapObjects(ADT adt, ChunkInstance chunk,
            CancellationToken cancellationToken)
        {
            int index = 0;
            foreach (var wmoReference in adt.WorldMapObjects)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                var wmoTransform = new Transform();
                wmoTransform.Position = new Vector3((32 * Constants.BlockSize - wmoReference.AbsolutePosition.Z), (32 * Constants.BlockSize - wmoReference.AbsolutePosition.X), wmoReference.AbsolutePosition.Y);
                wmoTransform.Rotation = Utilities.FromEuler(wmoReference.Rotation.X,  wmoReference.Rotation.Y + 180, wmoReference.Rotation.Z);

                var tcs = new TaskCompletionSource<WmoManager.WmoInstance?>();
                yield return wmoManager.LoadWorldMapObject(wmoReference.WmoPath, tcs);
                if (tcs.Task.Result == null)
                    continue;

                foreach (var mesh in tcs.Task.Result.Meshes)
                {
                    int i = 0;
                    foreach (var material in mesh.Item2)
                    {
                        chunk.renderHandles.Add(renderManager.RegisterStaticRenderer(mesh.Item1.Handle, material, i++, wmoTransform));

                        if (!material.BlendingEnabled)
                        {
                            var entity = entityManager.CreateEntity(archetypes.CollisionOnlyArchetype);
                            entityManager.GetComponent<LocalToWorld>(entity).Matrix = wmoTransform.LocalToWorldMatrix;
                            entityManager.GetComponent<Collider>(entity).CollisionMask = Collisions.COLLISION_MASK_WMO;
                            entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = i - 1;
                            entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = mesh.Item1.Handle;
                            entityManager.GetComponent<WorldMeshBounds>(entity) = RenderManager.LocalToWorld((MeshBounds)mesh.Item1.Bounds, new LocalToWorld() { Matrix = wmoTransform.LocalToWorldMatrix });   
                            chunk.entities.Add(entity);
                        }
                    }
                }
                
                index++;
                
                if (index % 10 == 0)
                    yield return null;
            }
        }

        public void Dispose()
        {
            foreach (var c in chunks)
                c.Dispose(textureManager);
        }

        public void Update(float delta)
        {
            if (loadingManager.Value.EssentialLoadingInProgress)
                return;
            
            RenderGrid = gameProperties.ShowGrid;
            
            int D = 1;
            (int x, int y) chunk = cameraManager.CurrentChunk;
            for (int i = -D; i <= D; ++i)
            {
                for (int j = -D; j <= D; ++j)
                    gameContext.StartCoroutine(LoadChunk(chunk.x + i, chunk.y + j, false));
            }
            
            UnloadChunks();
        }

        private void UnloadChunks()
        {
            var camera = new Vector2(cameraManager.Position.X, cameraManager.Position.Z);
            for (var index = chunks.Count - 1; index >= 0; index--)
            {
                var c = chunks[index];
                var midPoint = new Vector2(c.MiddlePoint.X, c.MiddlePoint.Z);
                if ((midPoint - camera).LengthSquared() > 5500 * 5500)
                {
                    chunksXY.Remove((c.X, c.Z));
                    loadedChunks.Remove((c.X, c.Z));
                    chunks.Remove(c);
                    gameContext.StartCoroutine(UnloadChunk(c));
                }
            }
        }

        private IEnumerator UnloadChunk(ChunkInstance chunk)
        {
            if (chunk.loading != null)
            {
                chunk.loading.Cancel();
                yield return chunk.chunkLoading;
            }
            
            IEnumerator UnloadModuleChunk(IGameModule arg)
            {
                yield return arg.UnloadChunk(chunk.X, chunk.Z);
            }
            
            yield return moduleManager.ForEach(UnloadModuleChunk);
            
            chunk.terrainEntity = Entity.Empty;
            foreach (var obj in chunk.renderHandles)
                renderManager.UnregisterStaticRenderer(obj);
            foreach (var entity in chunk.entities)
                entityManager.DestroyEntity(entity);
            foreach (var buffer in chunk.animationBuffers)
                buffer.Dispose();
            
            foreach (var obj in chunk.meshes)
                meshManager.DisposeMesh(obj);
                
            chunk.animationBuffers.Clear();

            chunk.Dispose(textureManager);
        }

        public IEnumerator UnloadAllChunks()
        {
            var chunksCopy = chunks.ToList();
            chunks.Clear();
            loadedChunks.Clear();
            chunksXY.Clear();
            foreach (var chunk in chunksCopy)
                yield return UnloadChunk(chunk);
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