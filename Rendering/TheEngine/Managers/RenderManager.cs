using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
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

        private RenderTexture renderTexture;

        private RenderTexture outlineTexture;

        private IMesh planeMesh;

        private ShaderHandle blitShader;

        private Material blitMaterial;

        private Material unlitMaterial;

        private Mesh currentMesh = null;

        private Mesh lineMesh = null;
        
        private Shader currentShader = null;

        private Archetype toRenderArchetype;
        private Archetype updateWorldBoundsArchetype;
        private Archetype dirtEntities;
        
        private Archetype staticRendererArchetype;
        private Archetype dynamicRendererArchetype;

        internal RenderManager(Engine engine, bool flipY)
        {
            this.engine = engine;

            cameraManager = engine.CameraManager;

            dirtEntities = engine.entityManager.NewArchetype()
                .WithComponentData<DirtyPosition>();
            
            staticRendererArchetype = engine.EntityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshRenderer>();

            dynamicRendererArchetype = engine.EntityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
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

            renderTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            engine.Device.device.CheckError("create render tex");

            //outlineTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            engine.Device.device.CheckError("create mesh");

            lineMesh = (Mesh)engine.MeshManager.CreateMesh(new Vector3[2]{Vector3.Zero, Vector3.Zero}, new int[]{});

            blitShader = engine.ShaderManager.LoadShader("internalShaders/blit.json");
            engine.Device.device.CheckError("load shader");
            blitMaterial = engine.MaterialManager.CreateMaterial(blitShader);
            blitMaterial.SetUniformInt("flipY", flipY ? 1 : 0);

            unlitMaterial = engine.MaterialManager.CreateMaterial("internalShaders/unlit.json");
            unlitMaterial.ZWrite = false;
            unlitMaterial.DepthTesting = DepthCompare.Always;
        }

        public void Dispose()
        {
            //outlineTexture.Dispose();
            engine.meshManager.DisposeMesh(lineMesh);
            engine.meshManager.DisposeMesh(planeMesh);
            renderTexture.Dispose();

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
        public void PrepareRendering(int dstFrameBuffer)
        {
            inRenderingLoop = true;
            engine.Device.device.CheckError("pre UpdateSceneBuffer");
            UpdateSceneBuffer();
            
            Stats = default;
            engine.Device.device.CheckError("Render begin");
            
            engine.Device.RenderClearBuffer();

            if (renderTexture.Width != (int)engine.WindowHost.WindowWidth ||
                renderTexture.Height != (int)engine.WindowHost.WindowHeight)
            {
                renderTexture.Dispose();
                renderTexture = engine.Device.CreateRenderTexture((int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            }
            
            engine.Device.SetRenderTexture(renderTexture, dstFrameBuffer);
            renderTexture.Clear(1, 1, 1, 1);

            sceneBuffer.UpdateBuffer(ref sceneData);
            sceneBuffer.Activate(Constants.SCENE_BUFFER_INDEX);

            //pixelShaderSceneBuffer.UpdateBuffer(ref scenePixelData);
            //pixelShaderSceneBuffer.Activate(Constants.PIXEL_SCENE_BUFFER_INDEX);

            objectBuffer.Activate(Constants.OBJECT_BUFFER_INDEX);
        }

        public void FinalizeRendering(int dstFrameBuffer)
        {
            engine.Device.SetRenderTexture(null, dstFrameBuffer);

            engine.Device.device.CheckError("Blitz");
            blitMaterial.Shader.Activate();
            blitMaterial.ActivateUniforms();
            SetDepth(true, DepthCompare.Always);
            renderTexture.Activate(0);
            //outlineTexture.Activate(1);
            planeMesh.Activate();
            SetBlending(false, Blending.One, Blending.Zero);
            engine.Device.DrawIndexed(engine.meshManager.GetMeshByHandle(planeMesh.Handle).IndexCount(0), 0, 0);

            inRenderingLoop = false;
            engine.statsManager.RenderStats = Stats;
        }

        private RenderStats Stats;
        
        internal void RenderWorld(int dstFrameBuffer)
        {
            //defaultSampler.Activate(Constants.DEFAULT_SAMPLER);

            engine.Device.device.CheckError("Before render all");
            RenderEntities();
            ClearDirtyEntityBit();

            //engine.Device.RenderClearBuffer();
            //engine.Device.SetRenderTexture(outlineTexture);
            //outlineTexture.Clear(0, 0, 0, 0);

            //RenderAll(unlitMaterial);
            //engine.Device.RenderBlitBuffer();
        }

        private void ClearDirtyEntityBit()
        {
            dirtEntities.ParallelForEach<DirtyPosition>((itr, start, end, dirty) =>
            {
                for (int i = start; i < end; ++i)
                    dirty[i].Disable();
            });
        }

        private void EnableMaterial(Material material)
        {
            SetDepth(material.ZWrite, material.DepthTesting);
            SetCulling(material.Culling);
            SetBlending(material.BlendingEnabled, material.SourceBlending, material.DestinationBlending);
            material.ActivateUniforms();
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

        private LocalToWorld[] localToWorlds = new LocalToWorld[10000];
        private MeshRenderer[] renderers = new MeshRenderer[10000];
        private void RenderEntities()
        {
            var cameraPosition = cameraManager.MainCamera.Transform.Position;
            var frustum = new BoundingFrustum(cameraManager.MainCamera.ViewMatrix * cameraManager.MainCamera.ProjectionMatrix);

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
            toRenderArchetype.ParallelForEach<LocalToWorld, RenderEnabledBit, WorldMeshBounds>((itr, start, end, l2w, bits, worldMeshBounds) =>
            {
                for (int i = start; i < end; ++i)
                {
                    var boundingBox = worldMeshBounds[i].box;
                    var pos = l2w[i].Position;
                    var boundingBoxSize = boundingBox.Size;
                    var size = boundingBoxSize.X + boundingBoxSize.Y + boundingBoxSize.Z;
                    size = Math.Max(size, 60);
                    bool doRender = (pos - cameraPosition).LengthSquared() < (size) * (size) * mod;
                    if (doRender)
                    {
                        bits[i] = (RenderEnabledBit)(frustum.Contains(ref boundingBox) != ContainmentType.Disjoint);
                    }
                    else
                        bits[i] = (RenderEnabledBit)false;
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
                count.Value = (opaque, transparent);
            });
            var opaque = count.Values.Sum(i => i.opaque);
            var transparent = count.Values.Sum(i => i.transparent);
            int totalToDraw = opaque + transparent;
            if (localToWorlds.Length < totalToDraw)
            {
                localToWorlds = new LocalToWorld[totalToDraw];
                renderers = new MeshRenderer[totalToDraw];
            }
            int opaqueIndex = 0;
            int transparentIndex = opaque;
            toRenderArchetype.ForEach<LocalToWorld, RenderEnabledBit, MeshRenderer>((itr, start, end, l2w, render, meshRenderer) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (!render[i])
                        continue;
                    
                    var renderer = meshRenderer[i];

                    if (renderer.Opaque)
                    {
                        renderers[opaqueIndex] = renderer;
                        localToWorlds[opaqueIndex++] = l2w[i];
                    }
                    else
                    {
                        renderers[transparentIndex] = renderer;
                        localToWorlds[transparentIndex++] = l2w[i];
                    }
                }
            });

            sw.Restart();
            for (int i = 0; i < totalToDraw; ++i)
            {
                var mr = renderers[i];
                var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
                var shader = material.Shader;
                var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);
                var meshId = mr.SubMeshId;

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
                EnableMaterial(material);
                //materialtimer.Stop();

                //buffertimer.Start();
                objectData.WorldMatrix = localToWorlds[i];
                objectData.InverseWorldMatrix = localToWorlds[i].Inverse;
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
            sw.Stop();
            engine.statsManager.Counters.Drawing.Add(sw.Elapsed.TotalMilliseconds);
        }

        public void Render(IMesh mesh, Material material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null)
        {
            if (worldToLocal == null)
                worldToLocal = Matrix.Invert(localToWorld);
            
            Debug.Assert(inRenderingLoop);
            if (currentShader != material.Shader)
            {
                currentShader = material.Shader;
                currentShader.Activate();
            }
            EnableMaterial(material);
            currentMesh = (Mesh)mesh;
            mesh.Activate();
            objectData.WorldMatrix = localToWorld;
            objectData.InverseWorldMatrix = worldToLocal.Value;
            objectBuffer.UpdateBuffer(ref objectData);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexed(count, start, 0);
        }

        public void DrawLine(Vector3 start, Vector3 end)
        {
            lineMesh.SetVertices(new Vector3[2]{start, end});
            lineMesh.Rebuild();
            if (currentShader != unlitMaterial.Shader)
            {
                currentShader = unlitMaterial.Shader;
                currentShader.Activate();
            }
            EnableMaterial(unlitMaterial);
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
            EnableMaterial(material);
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
            EnableMaterial(material);
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

        public StaticRenderHandle RegisterStaticRenderer(MeshHandle meshHandle, Material material, int subMesh, Matrix localToWorld)
        {
            var l2w = new LocalToWorld() { Matrix = localToWorld };
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var entity = engine.EntityManager.CreateEntity(staticRendererArchetype);
            engine.EntityManager.GetComponent<LocalToWorld>(entity) = l2w;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MaterialHandle = material.Handle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).MeshHandle = meshHandle;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Opaque = !material.BlendingEnabled;
            engine.EntityManager.GetComponent<WorldMeshBounds>(entity) = LocalToWorld((MeshBounds)mesh.Bounds, l2w);
            return new StaticRenderHandle(entity);
        }

        public void UnregisterStaticRenderer(StaticRenderHandle handle)
        {
            engine.EntityManager.DestroyEntity(handle.Handle);
        }
    }

    public static class Extensions
    {
        public static void DrawRay(this IRenderManager renderManager, Ray ray)
        {
            renderManager.DrawLine(ray.Position, ray.Position + ray.Direction);
            renderManager.DrawLine(ray.Position + ray.Direction - Vector3.Left * 0.5f, ray.Position + ray.Direction);
            renderManager.DrawLine(ray.Position + ray.Direction - Vector3.Forward * 0.5f, ray.Position + ray.Direction);
        }
    }
}
