using System.Collections;
using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader;
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
        public CancellationTokenSource? loading = new CancellationTokenSource();
        public uint[,] areaIds = new uint[16,16];
        public Task? chunkLoading;

        public List<StaticRenderHandle> renderHandles = new();
        public List<Entity> entities = new();

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
        
        public float? HeightAtPosition(float x, float y)
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
            this.engine = engine;
        }

        public IEnumerator LoadChunk(int y, int x, bool now)
        {
            if (!loadedChunks.Add((y, x)))
                yield break;
            
            ChunkInstance chunk = new(y, x);
            var cancelationToken = chunk.loading.Token;
            var tasksource = new TaskCompletionSource();
            chunk.chunkLoading = tasksource.Task;
            chunks.Add(chunk);
            chunksXY[(y, x)] = chunk;

            var fullName = gameFiles.Adt(gameContext.CurrentMap.Directory, x, y);
            var file = gameFiles.ReadFile(fullName);
            yield return file;
            if (file.Result == null)
            {
                tasksource.SetResult();
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
                    adt = new ADT(new MemoryBinaryReader(file.Result));
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

            if (adt == null)
                yield break;
            
            float minHeight = float.MaxValue;
            float maxHeight = float.MaxValue;
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
                    Vector3[] subVertices = new Vector3[145];
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

                    int[] indices = new int[4 * 8 * 8 * 3];
                    int k__ = 0;
                    for (int cx = 0; cx < 8; cx++)
                    {
                        for (int cy = 0; cy < 8; cy++)
                        {
                            int tl = cy * 17 + cx;
                            int tr = tl + 1;
                            int middle = tl + 9;
                            int bl = middle + 8;
                            int br = bl + 1;

                            indices[k__++] = tl;
                            indices[k__++] = middle;
                            indices[k__++] = tr;
                            //
                            indices[k__++] = tl;
                            indices[k__++] = bl;
                            indices[k__++] = middle;
                            //
                            indices[k__++] = tr;
                            indices[k__++] = middle;
                            indices[k__++] = br;
                            //
                            indices[k__++] = middle;
                            indices[k__++] = bl;
                            indices[k__++] = br;
                        }
                    }
                    var subChunkMesh = meshManager.CreateManagedOnlyMesh(subVertices, indices);
                    var entity = entityManager.CreateEntity(archetypes.CollisionOnlyArchetype);
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
                    var len = sm.GetLength(2);
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
            chunk.entities.Add(terrainEntity);
            
            if (cancelationToken.IsCancellationRequested)
            {
                tasksource.SetResult();
                chunk.loading = null;
                yield break;
            }
            
            yield return LoadObjects(adt, chunk, cancelationToken);
            
            tasksource.SetResult();
            chunk.loading = null; 
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
                t.Rotation = Quaternion.FromEuler(m2.Rotation.X, m2.Rotation.Y + 180,  m2.Rotation.Z);

                int j = 0;
                foreach (var material in m.materials)
                {
                    //
                    // var entity = entityManager.CreateEntity(renderEntityArchetype);
                    // entityManager.GetComponent<LocalToWorld>(entity).Matrix = t.LocalToWorldMatrix;
                    // entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = j++;
                    // entityManager.GetComponent<MeshRenderer>(entity).MaterialHandle = material.Handle;
                    // entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = m.mesh.Handle;
                    // entityManager.GetComponent<MeshBounds>(entity) = (MeshBounds)m.mesh.Bounds;
                    // entityManager.GetComponent<DirtyPosition>(entity).Enable();
                    // chunk.objectHandles2.Add(entity);
                    //
                    chunk.renderHandles.Add(renderManager.RegisterStaticRenderer(m.mesh.Handle, material, j++, t));
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
                wmoTransform.Rotation = Quaternion.FromEuler(wmoReference.Rotation.X,  wmoReference.Rotation.Y + 180, wmoReference.Rotation.Z);

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
                    if (worldManager.IsChunkPresent(chunk.x + i, chunk.y + j))
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
                if ((midPoint - camera).LengthSquared() > 7500 * 7500)
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
            
            foreach (var obj in chunk.renderHandles)
                renderManager.UnregisterStaticRenderer(obj);
            foreach (var entity in chunk.entities)
                entityManager.DestroyEntity(entity);
            
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
    }
}