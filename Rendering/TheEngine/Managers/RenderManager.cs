using System.Diagnostics;
using Avalonia.Input;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Primitives;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Managers
{
    public class RenderManager : IRenderManager, IDisposable
    {
        private readonly Engine engine;

        private NativeBuffer<SceneBuffer> sceneBuffer;
        private NativeBuffer<ObjectBuffer> objectBuffer;
        //private NativeBuffer<PixelShaderSceneBuffer> pixelShaderSceneBuffer;
        private NativeBuffer<Matrix> instancesBuffer;
        private NativeBuffer<Matrix> instancesInverseBuffer;
        private MaterialInstanceRenderData instancingRenderData = new();

        private ObjectBuffer objectData;
        private SceneBuffer sceneData;
        //private PixelShaderSceneBuffer scenePixelData;
        private Matrix[] instancesArray;
        private Matrix[] inverseInstancesArray;

        private ICameraManager cameraManager;
        
        private Sampler defaultSampler;

        private DepthStencil depthStencilZWrite;
        private DepthStencil depthStencilNoZWrite;

        private DepthCompare? currentDepthTest;
        private bool? currentZwrite;
        private bool? currentDepthTestEnabled;
        private CullingMode? currentCulling;
        private (bool enabled, Blending? source, Blending? dest)? currentBlending;

        private int currentBackBufferWidth = -1;
        private int currentBackBufferHeight = -1;
        private TextureHandle[] backBuffers = new TextureHandle[2];
        private int currentBackBufferIndex = 0;
        private TextureHandle CurrentBackBuffer => backBuffers[currentBackBufferIndex];
        private TextureHandle OtherBackBuffer => backBuffers[1 - currentBackBufferIndex];
        private void SwapBackBuffers() => currentBackBufferIndex = (currentBackBufferIndex + 1) % 2;
        
        private IMesh planeMesh;

        private ShaderHandle blitShader;

        private Material blitMaterial;

        private Material unlitMaterial;
        
        // utils
        private IMesh sphereMesh = null!;
        private Material wireframe = null!;
        // end utils

        private Mesh currentMesh = null;

        private Mesh lineMesh = null;
        
        private Shader currentShader = null;

        private Archetype toCullArchetype;
        private Archetype entitiesSharingRenderingArchetype;
        private Archetype toRenderArchetype;
        private Archetype updateWorldBoundsArchetype;
        private Archetype dirtEntities;
        
        private Archetype staticRendererArchetype;
        private Archetype dynamicRendererArchetype;

        private Archetype dynamicParentedEntitiesArchetype;

        internal RenderManager(Engine engine, bool flipY)
        {
            this.engine = engine;

            cameraManager = engine.CameraManager;

            dirtEntities = engine.entityManager.NewArchetype()
                .WithComponentData<DirtyPosition>();

            entitiesSharingRenderingArchetype = engine.entityManager.NewArchetype()
                .WithComponentData<ShareRenderEnabledBit>()
                .WithComponentData<RenderEnabledBit>();

            dynamicParentedEntitiesArchetype = engine.entityManager.NewArchetype()
                .WithComponentData<CopyParentTransform>()
                .WithComponentData<DirtyPosition>()
                .WithComponentData<LocalToWorld>();
            
            staticRendererArchetype = engine.EntityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<PerformCullingBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshRenderer>();

            dynamicRendererArchetype = engine.EntityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<PerformCullingBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<MeshBounds>()
                .WithComponentData<DirtyPosition>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshRenderer>();
            
            updateWorldBoundsArchetype = engine.entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<DirtyPosition>()
                .WithComponentData<MeshBounds>();

            toCullArchetype = engine.entityManager.NewArchetype()
                    .WithComponentData<RenderEnabledBit>()
                    .WithComponentData<LocalToWorld>()
                    .WithComponentData<PerformCullingBit>()
                    .WithComponentData<WorldMeshBounds>();

            toRenderArchetype = engine.entityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshRenderer>();

            sceneBuffer = engine.Device.CreateBuffer<SceneBuffer>(BufferTypeEnum.ConstVertex, 1);
            objectBuffer = engine.Device.CreateBuffer<ObjectBuffer>(BufferTypeEnum.ConstVertex, 1);
            //pixelShaderSceneBuffer = engine.Device.CreateBuffer<PixelShaderSceneBuffer>(BufferTypeEnum.ConstPixel, 1);
            instancesBuffer = engine.Device.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            instancesInverseBuffer = engine.Device.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            engine.Device.device.CheckError("created buffers");

            instancesArray = new Matrix[1];
            inverseInstancesArray = new Matrix[1];

            sceneData = new SceneBuffer();

            defaultSampler = engine.Device.CreateSampler();
            engine.Device.device.CheckError("create sampler");

            depthStencilZWrite = engine.Device.CreateDepthStencilState(true);
            depthStencilNoZWrite = engine.Device.CreateDepthStencilState(false);
            engine.Device.device.CheckError("create depth stencil");

            for (int i = 0; i < backBuffers.Length; ++i)
                backBuffers[i] = engine.textureManager.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            engine.Device.device.CheckError("create render tex");

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            engine.Device.device.CheckError("create mesh");

            lineMesh = (Mesh)engine.MeshManager.CreateMesh(new Vector3[2]{Vector3.Zero, Vector3.Zero}, new ushort[]{});

            blitShader = engine.ShaderManager.LoadShader("internalShaders/blit.json", false);
            engine.Device.device.CheckError("load shader");
            blitMaterial = engine.MaterialManager.CreateMaterial(blitShader, null);
            blitMaterial.SourceBlending = Blending.One;
            blitMaterial.DestinationBlending = Blending.Zero;
            blitMaterial.ZWrite = true;
            blitMaterial.DepthTesting = DepthCompare.Always;
            blitMaterial.SetUniformInt("flipY", flipY ? 1 : 0);

            unlitMaterial = engine.MaterialManager.CreateMaterial("internalShaders/unlit.json");
            unlitMaterial.ZWrite = false;
            unlitMaterial.DepthTesting = DepthCompare.Always;
            
            // utils
            sphereMesh = engine.meshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);
        
            wireframe = engine.MaterialManager.CreateMaterial("data/wireframe.json");
            wireframe.SetUniform("Width", 1);
            wireframe.SetUniform("Color", new Vector4(1, 1, 1, 1));
            wireframe.ZWrite = false;
            wireframe.DepthTesting = DepthCompare.Always;
        }

        public void Dispose()
        {
            engine.meshManager.DisposeMesh(sphereMesh);
            //outlineTexture.Dispose();
            engine.meshManager.DisposeMesh(lineMesh);
            engine.meshManager.DisposeMesh(planeMesh);
            foreach (var t in backBuffers)
                engine.textureManager.DisposeTexture(t);

            depthStencilZWrite.Dispose();
            depthStencilNoZWrite.Dispose();
            defaultSampler.Dispose();
            instancesInverseBuffer.Dispose();
            instancesBuffer.Dispose();
            //pixelShaderSceneBuffer.Dispose();
            objectBuffer.Dispose();
            sceneBuffer.Dispose();
        }
        
        private void SetBlending(bool enabled, Blending source, Blending dest)
        {
            if (currentBlending.HasValue && currentBlending.Value.enabled == enabled &&
                currentBlending.Value.source == source && currentBlending.Value.dest == dest) 
                return;

            if (currentBlending.HasValue && currentBlending.Value.enabled == enabled)
            {
                engine.Device.device.BlendFunc((BlendingFactorSrc)source, ((BlendingFactorDest)dest));
                currentBlending = (enabled, source, dest);
            }
            else if (enabled)
            {
                engine.Device.device.Enable(EnableCap.Blend);
                engine.Device.device.BlendFunc((BlendingFactorSrc)source, ((BlendingFactorDest)dest));
                currentBlending = (enabled, source, dest);
            }
            else
            {
                engine.Device.device.Disable(EnableCap.Blend);
                currentBlending = (enabled, null, null);
            }
        }

        private void SetCulling(CullingMode culling)
        {
            if (!currentCulling.HasValue || currentCulling.Value != culling)
            {
                if (culling == CullingMode.Off)
                {
                    engine.Device.device.Disable(EnableCap.CullFace);
                }
                else
                {
                    if (!currentCulling.HasValue || currentCulling == CullingMode.Off)
                        engine.Device.device.Enable(EnableCap.CullFace);
                    engine.Device.device.CullFace(currentCulling == CullingMode.Front ? CullFaceMode.Front : CullFaceMode.Back);
                }
                currentCulling = culling;
            }
        }
        
        private void SetDepth(bool zwrite, DepthCompare depthCompare)
        {
            if (zwrite == false && depthCompare == DepthCompare.Always)
            {
                if (!currentDepthTestEnabled.HasValue || currentDepthTestEnabled.Value)
                {
                    engine.Device.device.Disable(EnableCap.DepthTest);
                    currentDepthTestEnabled = false;
                    currentZwrite = null;
                    currentDepthTest = null;
                }
            }
            else
            {
                if (!currentDepthTestEnabled.HasValue || !currentDepthTestEnabled.Value)
                {
                    engine.Device.device.Enable(EnableCap.DepthTest);
                    currentDepthTestEnabled = true;
                }
                
                if (!currentZwrite.HasValue || currentZwrite.Value != zwrite)
                {
                    if (zwrite)
                        engine.Device.device.DepthMask(true);
                    else
                        engine.Device.device.DepthMask(false);
                    currentZwrite = zwrite;
                }
                
                if (!currentDepthTest.HasValue || currentDepthTest.Value != depthCompare)
                {
                    engine.Device.device.DepthFunction((DepthFunction)depthCompare);
                    currentDepthTest = depthCompare;
                }
            }
        }

        public void BeginFrame()
        {
            // better not assume state was saved from the previous frame...
            currentMesh = null;
            currentShader = null;
            currentCulling = null;
            currentZwrite = null;
            currentDepthTest = null;
            currentDepthTest = null;
            currentBlending = null;
            cameraManager.MainCamera.Aspect = engine.WindowHost.Aspect;
        }

        private bool inRenderingLoop = false;
        private bool inCoreRenderingLoop = false;
        private int currentFrameBuffer;
        public void PrepareRendering(int dstFrameBuffer)
        {
            // dynamic - auto scale
            // var drawTime = engine.statsManager.Counters.FrameTime.Average;
            // if (drawTime > 22)
            //     dynamicScale /= 1.02f;
            // else if (drawTime <= 18)
            //     dynamicScale *= 1.02f;
            // dynamicScale = Math.Clamp(dynamicScale, 0.25, 1);
        
            currentFrameBuffer = dstFrameBuffer;
            engine.shaderManager.Update();

            inRenderingLoop = true;
            engine.Device.device.CheckError("pre UpdateSceneBuffer");
            UpdateSceneBuffer();
            
            Stats = default;
            engine.Device.device.CheckError("Render begin");
            
            engine.Device.RenderClearBuffer();

            if (currentBackBufferWidth != (int)engine.WindowHost.WindowWidth ||
                currentBackBufferHeight != (int)engine.WindowHost.WindowHeight)
            {
                currentBackBufferWidth = (int)engine.WindowHost.WindowWidth;
                currentBackBufferHeight = (int)engine.WindowHost.WindowHeight;
                for (var index = 0; index < backBuffers.Length; index++)
                {
                    engine.textureManager.DisposeTexture(backBuffers[index]);
                    backBuffers[index] = engine.textureManager.CreateRenderTexture(currentBackBufferWidth, currentBackBufferHeight);
                }
            }

            if (pendingDynamicScale.HasValue)
            {
                if (Math.Abs(pendingDynamicScale.Value - 1) < 0.01f)
                {
                    useDynamicScale = false;
                }
                else
                {
                    dynamicScale = pendingDynamicScale.Value;
                    useDynamicScale = true;
                }
                pendingDynamicScale = null;
            }

            inCoreRenderingLoop = true;
            currentBackBufferIndex = 0;
            
            ActivateRenderTexture(CurrentBackBuffer, Color4.White);

            sceneBuffer.UpdateBuffer(ref sceneData);
            sceneBuffer.Activate(Constants.SCENE_BUFFER_INDEX);

            //pixelShaderSceneBuffer.UpdateBuffer(ref scenePixelData);
            //pixelShaderSceneBuffer.Activate(Constants.PIXEL_SCENE_BUFFER_INDEX);

            objectBuffer.Activate(Constants.OBJECT_BUFFER_INDEX);
            engine.Device.device.CheckError("Before render all");
        }

        private bool useDynamicScale = false;
        private float dynamicScale = 1;
        private float? pendingDynamicScale;
        private int DynamicWidth => useDynamicScale ? Math.Max(1, (int)(currentBackBufferWidth * dynamicScale)) : currentBackBufferWidth;
        private int DynamicHeight => useDynamicScale ? Math.Max(1, (int)(currentBackBufferHeight * dynamicScale)) : currentBackBufferHeight;

        public void ActivateRenderTexture(TextureHandle rt, Color4? color = null)
        {
            if (inRenderingLoop)
            {
                var tex = engine.textureManager.GetTextureByHandle(rt) as RenderTexture;
                tex!.ActivateFrameBuffer(inCoreRenderingLoop && rt == CurrentBackBuffer && useDynamicScale ? dynamicScale : 1);
                if (color.HasValue)
                    tex!.Clear(color.Value.Red, color.Value.Green, color.Value.Blue, color.Value.Alpha);
            }
        }
        
        public void ActivateDefaultRenderTexture()
        {
            ActivateRenderTexture(CurrentBackBuffer);
        }

        private readonly List<IPostProcess> postProcesses = new();
        
        public void AddPostprocess(IPostProcess postProcess) => postProcesses.Add(postProcess);

        public void RemovePostprocess(IPostProcess postProcess) => postProcesses.Remove(postProcess);
        
        public void SetDynamicResolutionScale(float scale)
        {
            pendingDynamicScale = scale;
        }

        public void DrawSphere(Vector3 center, float radius, Vector4 color)
        {
            wireframe.SetUniform("Color", color);
            Render(sphereMesh, wireframe, 0, Matrix.TRS(center, Quaternion.Identity, Vector3.One * radius));
        }

        public void RenderFullscreenPlane(Material material)
        {
            material.Shader.Activate();
            EnableMaterial(material, false, null);
            planeMesh.Activate();
            engine.Device.DrawIndexed(engine.meshManager.GetMeshByHandle(planeMesh.Handle).IndexCount(0), 0, 0);
        }
        
        public void FinalizeRendering(int dstFrameBuffer)
        {
            ClearDirtyEntityBit();

            engine.Device.SetRenderTexture(null, dstFrameBuffer);
            engine.Device.device.Viewport(0, 0, (int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            
            engine.Device.device.CheckError("Blitz");
            blitMaterial.SetTexture("texture1", CurrentBackBuffer);
            RenderFullscreenPlane(blitMaterial);
            
            inRenderingLoop = false;
            engine.statsManager.RenderStats = Stats;
        }

        private RenderStats Stats;
        
        internal void RenderOpaque(int dstFrameBuffer)
        {
            //defaultSampler.Activate(Constants.DEFAULT_SAMPLER);
            RenderEntities();

            //engine.Device.RenderClearBuffer();
            //engine.Device.SetRenderTexture(outlineTexture);
            //outlineTexture.Clear(0, 0, 0, 0);

            //RenderAll(unlitMaterial);
            //engine.Device.RenderBlitBuffer();
        }
        
        internal void RenderTransparent(int dstFrameBuffer)
        {
            RenderTransparent();
        }

        internal void RenderPostProcess()
        {
            inCoreRenderingLoop = false;
            if (useDynamicScale)
            {
                engine.textureManager.BlitFramebuffers(CurrentBackBuffer, OtherBackBuffer, 0, 0, DynamicWidth, DynamicHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
                engine.textureManager.BlitFramebuffers(CurrentBackBuffer, OtherBackBuffer, 0, 0, DynamicWidth, DynamicHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);

                SwapBackBuffers();
                ActivateDefaultRenderTexture(); // to make sure we set the correct viewport
            }
            
            foreach (var post in postProcesses)
            {
                ActivateRenderTexture(OtherBackBuffer, Color4.White);
                post.RenderPostprocess(this, CurrentBackBuffer);
                SwapBackBuffers();
            }
        }

        public void UpdateTransforms()
        {
            var entityManager = engine.entityManager;
            dynamicParentedEntitiesArchetype.ParallelForEach<CopyParentTransform, LocalToWorld, DirtyPosition>((itr, start, end, parents, localToWorld, dirtyPosition) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (!dirtyPosition[i] && !entityManager.GetComponent<DirtyPosition>(parents[i].Parent))
                        continue;
                    ref var parentLocalToWorld = ref entityManager.GetComponent<LocalToWorld>(parents[i].Parent);
                    localToWorld[i] = parentLocalToWorld;
                    dirtyPosition[i].Enable();
                }
            });
        }
        
        private void ClearDirtyEntityBit()
        {
            dirtEntities.ParallelForEach<DirtyPosition>((itr, start, end, dirty) =>
            {
                for (int i = start; i < end; ++i)
                    dirty[i].Disable();
            });
        }

        private void EnableMaterial(Material material, bool instancing, MaterialInstanceRenderData? instanceData = null)
        {
            SetDepth(material.ZWrite, material.DepthTesting);
            SetCulling(material.Culling);
            SetBlending(material.BlendingEnabled, material.SourceBlending, material.DestinationBlending);
            material.ActivateUniforms(instancing, instanceData);
            Stats.MaterialActivations++;
        }

        public static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld)
        {
            Span<Vector3> corners = stackalloc Vector3[8];
            return LocalToWorld(in local, in localToWorld, ref corners);
        }
        
        internal static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld, ref Span<Vector3> corners)
        {
            var matrix = localToWorld.Matrix;
            var min = local.box.Minimum;
            var max = local.box.Maximum;
            corners[0] = new Vector3(min.X, max.Y, max.Z);
            corners[1] = new Vector3(max.X, max.Y, max.Z);
            corners[2] = new Vector3(max.X, min.Y, max.Z);
            corners[3] = new Vector3(min.X, min.Y, max.Z);
            corners[4] = new Vector3(min.X, max.Y, min.Z);
            corners[5] = new Vector3(max.X, max.Y, min.Z);
            corners[6] = new Vector3(max.X, min.Y, min.Z);
            corners[7] = new Vector3(min.X, min.Y, min.Z);
            for (int j = 0; j < 8; ++j)
            {
                var vec4 = new Vector4(corners[j], 1);
                Vector4.Transform(ref vec4, ref matrix, out var worldspace);
                corners[j] = worldspace.XYZ;
            }

            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int j = 0; j < 8; ++j)
            {
                min.X = Math.Min(min.X, corners[j].X);
                min.Y = Math.Min(min.Y, corners[j].Y);
                min.Z = Math.Min(min.Z, corners[j].Z);
                            
                max.X = Math.Max(max.X, corners[j].X);
                max.Y = Math.Max(max.Y, corners[j].Y);
                max.Z = Math.Max(max.Z, corners[j].Z);
            }

            return (WorldMeshBounds)new BoundingBox(min, max);
        }
        
        Stopwatch culler = new Stopwatch();
        Stopwatch boundsUpdate = new Stopwatch();
        Stopwatch sw = new Stopwatch();
        Stopwatch shadertimer = new Stopwatch();
        Stopwatch meshtimer = new Stopwatch();
        Stopwatch materialtimer = new Stopwatch();
        Stopwatch buffertimer = new Stopwatch();
        Stopwatch draw = new Stopwatch();
        private float viewDistanceModifier = 8;

        private (LocalToWorld, MaterialInstanceRenderData?)[] localToWorlds = new (LocalToWorld, MaterialInstanceRenderData?)[10000];
        //private MaterialInstanceRenderData?[] materialInstanceData = new MaterialInstanceRenderData?[10000];
        private MeshRenderer[] renderers = new MeshRenderer[10000];
        private int opaque;
        private int transparent;
        private int totalToDraw;

        private void RenderEntities()
        {
            var cameraPosition = cameraManager.MainCamera.Transform.Position;
            var frustum = new BoundingFrustum(cameraManager.MainCamera.ViewMatrix * cameraManager.MainCamera.ProjectionMatrix);
            var entityManager = engine.EntityManager;

            boundsUpdate.Restart();
            updateWorldBoundsArchetype.ParallelForEach<LocalToWorld, MeshBounds, WorldMeshBounds, DirtyPosition>((itr, start, end, l2w, meshBounds, worldMeshBounds, dirtyBit) =>
            {
                Span<Vector3> corners = stackalloc Vector3[8];
                for (int i = start; i < end; ++i)
                {
                    if (!dirtyBit[i])
                        continue;

                    worldMeshBounds[i] = LocalToWorld(in meshBounds[i], in l2w[i], ref corners);
                }
            });
            boundsUpdate.Stop();
            engine.statsManager.Counters.BoundsCalc.Add(boundsUpdate.Elapsed.TotalMilliseconds);
            
            culler.Restart();
            float mod = viewDistanceModifier * viewDistanceModifier;
            toCullArchetype.ParallelForEach<LocalToWorld, PerformCullingBit, RenderEnabledBit, WorldMeshBounds>((itr, start, end, l2w, _ , bits, worldMeshBounds) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (bits[i].IsForceDisabled())
                        continue;
                    
                    var boundingBox = worldMeshBounds[i].box;
                    var pos = boundingBox.Center;
                    var boundingBoxSize = boundingBox.Size;
                    var size = boundingBoxSize.X + boundingBoxSize.Y + boundingBoxSize.Z;
                    size = MathF.Sqrt(Math.Max(size, 10));
                    bool doRender = (pos - cameraPosition).LengthSquared() < (size) * (size) * (size) * mod;
                    if (doRender)
                    {
                        bits[i] = (RenderEnabledBit)(frustum.Contains(ref boundingBox) != ContainmentType.Disjoint);
                    }
                    else
                        bits[i] = (RenderEnabledBit)false;
                }
            });
            dynamicParentedEntitiesArchetype.WithComponentData<RenderEnabledBit>().ParallelForEach<RenderEnabledBit, CopyParentTransform>((itr, start, end, renderBit, cpt) =>
            {
                for (int i = start; i < end; ++i)
                {
                    var parent = cpt[i];
                    if (parent.Parent != Entity.Empty)
                        renderBit[i] = engine.entityManager.GetComponent<RenderEnabledBit>(parent.Parent);
                }
            });
            entitiesSharingRenderingArchetype.ParallelForEach<RenderEnabledBit, ShareRenderEnabledBit>(
                (itr, start, end, renderBits, shareRenderBits) =>
                {
                    for (int i = start; i < end; ++i)
                    {
                        if (!renderBits[i])
                            renderBits[i] = entityManager.GetComponent<RenderEnabledBit>(shareRenderBits[i].OtherEntity);
                    }
                });
            culler.Stop();
            engine.statsManager.Counters.Culling.Add(culler.Elapsed.TotalMilliseconds);

            ThreadLocal<(int opaque, int transparent)> count = new(true);
            toRenderArchetype.ParallelForEach<RenderEnabledBit, MeshRenderer>((itr, start, end, renderBit, renderers) =>
            {
                int opaque = 0;
                int transparent = 0;
                for (int i = start; i < end; ++i)
                {
                    if (!renderBit[i])
                        continue;
                    
                    if (renderers[i].Opaque)
                        opaque++;
                    else
                        transparent++;
                }
                if (count.IsValueCreated)
                    count.Value = (opaque + count.Value.opaque, transparent + count.Value.transparent);
                else
                    count.Value = (opaque, transparent);
            });
            opaque = count.Values.Sum(i => i.opaque);
            transparent = count.Values.Sum(i => i.transparent);
            totalToDraw = opaque + transparent;
            if (localToWorlds.Length < totalToDraw)
            {
                localToWorlds = new (LocalToWorld, MaterialInstanceRenderData?)[totalToDraw];
                renderers = new MeshRenderer[totalToDraw];
                //materialInstanceData = new MaterialInstanceRenderData[totalToDraw];
            }
            int opaqueIndex = 0;
            int transparentIndex = opaque;
            toRenderArchetype.ForEachRRRO<LocalToWorld, RenderEnabledBit, MeshRenderer, MaterialInstanceRenderData>((itr, start, end, l2w, render, meshRenderer, materialData) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (!render[i])
                        continue;
                    
                    var renderer = meshRenderer[i];

                    if (renderer.Opaque)
                    {
                        renderers[opaqueIndex] = renderer;
                        //materialInstanceData[opaqueIndex] = materialData?[i];
                        localToWorlds[opaqueIndex++] = (l2w[i], materialData?[i]);
                    }
                    else
                    {
                        renderers[transparentIndex] = renderer;
                        //materialInstanceData[transparentIndex] = materialData?[i];
                        localToWorlds[transparentIndex++] = (l2w[i], materialData?[i]);
                    }
                }
            });

            SortRenderersByMesh(0, opaque);
            SortRenderersByMesh(opaque + 1, totalToDraw);

            Render(0, opaque);
        }

        private void SortRenderersByMesh(int start, int end)
        {
            if (end <= start)
                return;
            Array.Sort(renderers, localToWorlds, start, end - start, Comparer<MeshRenderer>.Create((a, b) =>
            {
                if (a.MeshHandle == b.MeshHandle)
                {
                    if (a.SubMeshId == b.SubMeshId)
                        return a.MaterialHandle.Handle.CompareTo(b.MaterialHandle.Handle);
                    return a.SubMeshId.CompareTo(b.SubMeshId);
                }
                return a.MeshHandle.Handle.CompareTo(b.MeshHandle.Handle);
            }));
        }

        private void RenderTransparent()
        {
            Render(opaque, totalToDraw);
        }

        private bool enableInstancing = true;

        private void Render(int start, int end)
        {
            sw.Restart();
            int savedByInstancing = 0;
            for (int i = start; i < end; ++i)
            {
                var mr = renderers[i];
                var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
                var shader = material.Shader;
                var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);
                var meshId = mr.SubMeshId;

                var toBatch = 0;
                if (material.InstancedShader != null && // shader supports instancing
                    enableInstancing &&                 // instancing is enabled
                    localToWorlds[i].Item2 == null)     // no material per instance data
                {
                    int j = i + 1;
                    while (j < end && renderers[j].MeshHandle == mr.MeshHandle &&
                           renderers[j].SubMeshId == mr.SubMeshId &&
                           renderers[j].MaterialHandle == mr.MaterialHandle &&
                           localToWorlds[j].Item2 == null)
                    {
                        toBatch++;
                        j++;
                    }   
                }

                if (toBatch <= 1)
                {
                    if (currentShader != shader)
                    {
                        Stats.ShaderSwitches++;
                        currentShader = shader;
                        //shadertimer.Start();
                        shader.Activate();
                        //shadertimer.Stop();
                    }

                    if (currentMesh != mesh)
                    {
                        Stats.MeshSwitches++;
                        currentMesh = mesh;
                        //meshtimer.Start();
                        mesh.Activate();
                        //meshtimer.Stop();
                    }
                
                    //materialtimer.Start();
                    EnableMaterial(material, false, localToWorlds[i].Item2);
                    //materialtimer.Stop();
                    
                    //buffertimer.Start();
                    objectData.WorldMatrix = localToWorlds[i].Item1;
                    objectData.InverseWorldMatrix = localToWorlds[i].Item1.Inverse;
                    objectBuffer.UpdateBuffer(ref objectData);
                    //buffertimer.Stop();
#if DEBUG
                currentShader.Validate();
#endif
                    //draw.Start();
                    var indicesCount = mesh.IndexCount(meshId);
                    Stats.IndicesDrawn += indicesCount;
                    Stats.TrianglesDrawn += indicesCount / 3;
                    Stats.NonInstancedDraws++;
                    engine.Device.DrawIndexed(indicesCount, mesh.IndexStart(meshId), 0);
                    //draw.Stop();
                }
                else
                {
                    savedByInstancing += toBatch;
                    if (renderers[i + toBatch].MaterialHandle != renderers[i].MaterialHandle)
                    {
                        Console.WriteLine(" no zjebane xd");
                    }

                    if (instancesArray.Length < toBatch + 1)
                    {
                        instancesArray = new Matrix[toBatch + 1];
                        inverseInstancesArray = new Matrix[toBatch + 1];
                    }
                    
                    for (int k = 0; k < toBatch + 1; ++k)
                    {
                        instancesArray[k] = localToWorlds[i + k].Item1.Matrix;
                        inverseInstancesArray[k] = localToWorlds[i + k].Item1.Inverse;
                    }

                    //buffertimer.Start();
                    //var instancesBuffer =
                    //    engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, worldMatrices, BufferInternalFormat.Float4);
                    //var instancesInverseBuffer =
                    //    engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, inerseWorldMatrices, BufferInternalFormat.Float4);
                    instancesBuffer.UpdateBuffer(instancesArray.AsSpan(0, toBatch + 1));
                    instancesInverseBuffer.UpdateBuffer(inverseInstancesArray.AsSpan(0, toBatch + 1));
                    //buffertimer.Stop();

                    shader = material.InstancedShader!;
                    
                    if (currentShader != shader)
                    {
                        Stats.ShaderSwitches++;
                        currentShader = shader;
                        //shadertimer.Start();
                        shader.Activate();
                        //shadertimer.Stop();
                    }

                    if (currentMesh != mesh)
                    {
                        Stats.MeshSwitches++;
                        currentMesh = mesh;
                        //meshtimer.Start();
                        mesh.Activate();
                        //meshtimer.Stop();
                    }
                
                    //materialtimer.Start();
                    instancingRenderData.Clear();
                    instancingRenderData.SetInstancedBuffer(material, "InstancingModels", instancesBuffer);
                    instancingRenderData.SetInstancedBuffer(material, "InstancingInverseModels", instancesInverseBuffer);
                    EnableMaterial(material, true, instancingRenderData);
                    //materialtimer.Stop();
                    
#if DEBUG
                currentShader.Validate();
#endif
                    //draw.Start();
                    var indicesCount = mesh.IndexCount(meshId);
                    Stats.IndicesDrawn += indicesCount * (toBatch + 1);
                    Stats.TrianglesDrawn += (indicesCount / 3) * (toBatch + 1);
                    Stats.InstancedDraws++;
                    engine.Device.DrawIndexedInstanced(indicesCount,  toBatch + 1, mesh.IndexStart(meshId), 0, 0);
                    
                    i += toBatch;
                }
            }
            sw.Stop();
            engine.statsManager.Counters.Drawing.Add(sw.Elapsed.TotalMilliseconds);
            Stats.InstancedDrawSaved += savedByInstancing;
        }

        public void Render(MeshHandle meshHandle, MaterialHandle materialHandle, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var material = engine.materialManager.GetMaterialByHandle(materialHandle);
            Render(mesh, material, submesh, localToWorld, worldToLocal, instanceData);
        }
        
        public void Render(IMesh mesh, Material material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null)
        {
            if (worldToLocal == null)
                worldToLocal = Matrix.Invert(localToWorld);
            
            Debug.Assert(inRenderingLoop);
            if (currentShader != material.Shader)
            {
                currentShader = material.Shader;
                currentShader.Activate();
            }
            EnableMaterial(material, false, instanceData);
            currentMesh = (Mesh)mesh;
            mesh.Activate();
            objectData.WorldMatrix = localToWorld;
            objectData.InverseWorldMatrix = worldToLocal.Value;
            objectBuffer.UpdateBuffer(ref objectData);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexed(count, start, 0);
        }

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color)
        {
            lineMesh.SetVertices(start, end);
            lineMesh.Rebuild();
            if (currentShader != unlitMaterial.Shader)
            {
                currentShader = unlitMaterial.Shader;
                currentShader.Activate();
            }
            unlitMaterial.SetUniform("color", color);
            EnableMaterial(unlitMaterial, false);
            currentMesh = (Mesh)lineMesh;
            lineMesh.Activate();
            objectData.WorldMatrix = Matrix.Identity;
            objectData.InverseWorldMatrix = Matrix.Identity;
            objectBuffer.UpdateBuffer(ref objectData);
            engine.Device.DrawLineMesh(2, 0);
        }

        public void Render(IMesh mesh, Material material, int submesh, Transform transform)
        {
            Render(mesh, material, submesh, transform.LocalToWorldMatrix, transform.WorldToLocalMatrix);
        }

        public void Render(IMesh mesh, Material material, int submesh, Vector3 position)
        {
            var matrix = Matrix.Translation(position);
            Render(mesh, material, submesh, matrix);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int instancesCount, Matrix localToWorld, Matrix? worldToLocal = null)
        {
            if (!worldToLocal.HasValue)
                worldToLocal = Matrix.Invert(localToWorld);
            material.Shader.Activate();
            EnableMaterial(material, false);
            objectData.WorldMatrix = localToWorld;
            objectData.InverseWorldMatrix = worldToLocal.Value;
            objectBuffer.UpdateBuffer(ref objectData);
            
            mesh.Activate();
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexedInstanced(count, instancesCount, start, 0, 0);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int instancesCount)
        {
            material.Shader.Activate();
            EnableMaterial(material, false);
            mesh.Activate();
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexedInstanced(count, instancesCount, start, 0, 0);
        }

        public float ViewDistanceModifier
        {
            get => viewDistanceModifier;
            set
            {
                if (value > 0)
                    viewDistanceModifier = value;
            }
        }

        private void UpdateSceneBuffer()
        {
            var camera = cameraManager.MainCamera;
            var proj = camera.ProjectionMatrix;
            var vm = camera.Transform.WorldToLocalMatrix;

            sceneData.ViewMatrix = vm;
            sceneData.ProjectionMatrix = proj;
            sceneData.LightPosition = engine.lightManager.MainLight.LightPosition;
            sceneData.CameraPosition = new Vector4(engine.CameraManager.MainCamera.Transform.Position, 1);
            sceneData.LightDirection = new Vector4((Vector3.Forward * engine.lightManager.MainLight.LightRotation).Normalized, 0);
            sceneData.LightColor = engine.lightManager.MainLight.LightColor.XYZ;
            sceneData.LightIntensity = engine.lightManager.MainLight.LightIntensity;
            sceneData.SecondaryLightDirection = new Vector4(Vector3.Forward * engine.lightManager.SecondaryLight.LightRotation, 0);
            sceneData.SecondaryLightColor = engine.lightManager.SecondaryLight.LightColor.XYZ;
            sceneData.SecondaryLightIntensity = engine.lightManager.SecondaryLight.LightIntensity;
            sceneData.AmbientColor = engine.lightManager.MainLight.AmbientColor;
            sceneData.Time = (float)engine.TotalTime;
            sceneData.ScreenWidth = engine.WindowHost.WindowWidth;
            sceneData.ScreenHeight = engine.WindowHost.WindowHeight;
        }

        public StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Transform t)
        {
            return RegisterStaticRenderer(mesh, material, subMesh, t.LocalToWorldMatrix);
        }

        public void SetupRendererEntity(Entity entity, MeshHandle meshHandle, Material material, int subMesh, Matrix localToWorld)
        {
            var l2w = new LocalToWorld() { Matrix = localToWorld };
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            engine.EntityManager.GetComponent<LocalToWorld>(entity) = l2w;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MaterialHandle = material.Handle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MeshHandle = meshHandle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Opaque = !material.BlendingEnabled;
            engine.EntityManager.GetComponent<WorldMeshBounds>(entity) = LocalToWorld((MeshBounds)mesh.Bounds, l2w);
            if (engine.EntityManager.HasComponent<MeshBounds>(entity))
                engine.EntityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
        }
        
        public StaticRenderHandle RegisterStaticRenderer(MeshHandle meshHandle, Material material, int subMesh, Matrix localToWorld)
        {
            var entity = engine.EntityManager.CreateEntity(staticRendererArchetype);
            SetupRendererEntity(entity, meshHandle, material, subMesh, localToWorld);
            return new StaticRenderHandle(entity);
        }

        public void UnregisterStaticRenderer(StaticRenderHandle handle)
        {
            engine.EntityManager.DestroyEntity(handle.Handle);
        }
        
        public DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Transform t)
        {
            return RegisterDynamicRenderer(mesh, material, subMesh, t.LocalToWorldMatrix);
        }

        public DynamicRenderHandle RegisterDynamicRenderer(MeshHandle meshHandle, Material material, int subMesh, Matrix localToWorld)
        {
            var l2w = new LocalToWorld() { Matrix = localToWorld };
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var entity = engine.EntityManager.CreateEntity(dynamicRendererArchetype);
            engine.EntityManager.GetComponent<LocalToWorld>(entity) = l2w;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MaterialHandle = material.Handle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MeshHandle = meshHandle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Opaque = !material.BlendingEnabled;
            engine.EntityManager.GetComponent<DirtyPosition>(entity).Enable();
            engine.EntityManager.GetComponent<MeshBounds>(entity) = (MeshBounds)mesh.Bounds;
            return new DynamicRenderHandle(entity);
        }

        public void UnregisterDynamicRenderer(DynamicRenderHandle handle)
        {
            engine.EntityManager.DestroyEntity(handle.Handle);
        }
    }

    public static class Extensions
    {
        public static void DrawRay(this IRenderManager renderManager, Ray ray)
        {
            renderManager.DrawLine(ray.Position, ray.Position + ray.Direction, Color4.White);
            renderManager.DrawLine(ray.Position + ray.Direction - Vector3.Left * 0.5f, ray.Position + ray.Direction, Color4.White);
            renderManager.DrawLine(ray.Position + ray.Direction - Vector3.Forward * 0.5f, ray.Position + ray.Direction, Color4.White);
        }
    }
}
