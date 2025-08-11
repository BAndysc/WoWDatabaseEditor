using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Input;
using ImGuiNET;
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
using Veldrid;
using MouseButton = TheEngine.Input.MouseButton;
using Pipeline = TheEngine.Resources.Pipeline;
using PixelFormat = OpenGLBindings.PixelFormat;
using Sampler = TheAvaloniaOpenGL.Resources.Sampler;
using Shader = TheAvaloniaOpenGL.Resources.Shader;

namespace TheEngine.Managers
{
    public class RenderManager : IRenderManager, IDisposable
    {
        private readonly Engine engine;
        private readonly bool flipY;

        private NativeBuffer<SceneBuffer> sceneBuffer;
        private NativeBuffer<ObjectBuffer> objectBuffer;
        //private NativeBuffer<PixelShaderSceneBuffer> pixelShaderSceneBuffer;
        private NativeBuffer<Matrix> instancesBuffer;
        private NativeBuffer<Matrix> instancesInverseBuffer;
        private NativeBuffer<uint> instancesObjectIndicesBuffer;
        private NativeBuffer<Int4> instancesObjectDataBuffer;
        private MaterialInstanceRenderData instancingRenderData = new();

        private ObjectBuffer objectData;
        private SceneBuffer sceneData;
        //private PixelShaderSceneBuffer scenePixelData;
        private Matrix[] instancesArray;
        private Matrix[] inverseInstancesArray;
        private uint[] instancesObjectInddicesArray;
        private Int4[] instancesObjectDataArray;

        private ICameraManager cameraManager;
        
        private Sampler defaultSampler;

        private DepthStencil depthStencilZWrite;
        private DepthStencil depthStencilNoZWrite;

        private int currentBackBufferWidth = -1;
        private int currentBackBufferHeight = -1;
        private int currentSceneViewWidth = -1;
        private int currentSceneViewHeight = -1;
        private int currentGuiWidth = -1;
        private int currentGuiHeight = -1;
        private ITexture opaqueTexture2D;
        private ITexture depthTexture2D;
        internal ITexture sceneViewOpaqueTexture2D;
        private ITexture sceneViewDepthTexture2D;
        private ITexture opaqueRenderTexture;
        private ITexture mainObjectBuffer;
        private ITexture mainObjectDepthTexture;
        private ITexture mainObjectColorTexture;
        private ITexture mainObjectColor1Texture;
        private ITexture guiTexture;
        private ITexture sceneViewColor1Texture;
        private ITexture sceneViewTexture;
        private ITexture[] backBuffers = new ITexture[2];
        private int currentBackBufferIndex = -1;
        internal ITexture CurrentBackBuffer
        {
            get
            {
                if (currentBackBufferIndex == -1)
                    return mainObjectBuffer;
                return backBuffers[currentBackBufferIndex];
            }
        }

        private ITexture OtherBackBuffer
        {
            get
            {
                if (currentBackBufferIndex == -1)
                    return backBuffers[0];
                return backBuffers[1 - currentBackBufferIndex];
            }
        }

        private void SwapBackBuffers()
        {
            if (currentBackBufferIndex == -1)
            {
                currentBackBufferIndex = 0;
            }
            else
            {
                currentBackBufferIndex = (currentBackBufferIndex + 1) % 2;
            }
        }

        private IMesh planeMesh;

        private Material<BlitMaterialData_t> blitMaterial;

        private Material<UnlitMaterialData_t> unlitMaterial;
        
        // utils
        private IMesh sphereMesh = null!;
        private Material<WireframeMaterialData_t> wireframe = null!;
        // end utils

        private Mesh? currentMesh = null;

        private PipelineHandle currentPipeline;

        private Mesh? lineMesh = null;
        
        private ShaderPass? currentShader = null;
        private DepthCompare? currentDepthTest;
        private bool? currentZwrite;
        private bool? currentDepthTestEnabled;
        private CullingMode? currentCulling;
        private (bool enabled, Blending? source, Blending? dest)? currentBlending;

        private Archetype toCullArchetype;
        private Archetype entitiesSharingRenderingArchetype;
        private Archetype toRenderArchetype;
        private Archetype updateWorldBoundsArchetype;
        private Archetype dirtEntities;
        
        private Archetype staticRendererArchetype;
        private Archetype dynamicRendererArchetype;

        private Archetype dynamicParentedEntitiesArchetype;

        public ITexture DepthTexture => depthTexture2D;
        public ITexture OpaqueTexture => opaqueTexture2D;

        private RenderLayerData[] layers = Enumerable.Range(0, RenderLayer.MAX_LAYERS)
            .Select(layer => new RenderLayerData(){Layer = new RenderLayer((byte)layer, 0), Name = $"Unused {layer}"})
            .ToArray();
        private List<RenderLayer> freeLayers;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct BlitMaterialData_t
        {
            public int flipY;
            public int padding1;
            public int padding2;
            public int padding3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WireframeMaterialData_t
        {
            public Vector4 color;
            public float width;
            public int padding1;
            public int padding2;
            public int padding3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct UnlitMaterialData_t
        {
            public Vector4 color;
        }

        internal RenderManager(Engine engine, bool flipY)
        {
            this.engine = engine;
            this.flipY = flipY;

            layers[0].Name = "(default)";
            freeLayers = layers.Skip(1).Reverse().Select(x => x.Layer).ToList();

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
            instancesObjectIndicesBuffer = engine.Device.CreateBuffer<uint>(BufferTypeEnum.StructuredBufferPixelOnly, 1, BufferInternalFormat.UInt);
            instancesObjectDataBuffer = engine.Device.CreateBuffer<Int4>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Int4);
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

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            engine.Device.device.CheckError("create mesh");

            lineMesh = (Mesh)engine.MeshManager.CreateMesh(new Vector3[2]{Vector3.Zero, Vector3.Zero}, new ushort[]{});

            var blitShader = engine.ShaderManager.LoadShader("internalShaders/blit.json");
            // var blitDepthShader = engine.ShaderManager.LoadShader("internalShaders/blit_depth.json");
            var unlitShader = engine.ShaderManager.LoadShader("internalShaders/unlit.json");

            var blitPipeline = this.engine.pipelineManager.CreatePipeline(blitShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = new BlendStateDescription(
                    RgbaFloat.Clear,
                    BlendAttachmentDescription.OverrideBlend),
                DepthStencilState = new DepthStencilStateDescription(false, true, ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false)
            }, false);// todo veldrid, SwapChainOutput);

            blitMaterial = engine.MaterialManager.CreateMaterial<BlitMaterialData_t>(blitPipeline);
            // blitMaterial.SourceBlending = Blending.One;
            // blitMaterial.DestinationBlending = Blending.Zero;
            // blitMaterial.ZWrite = true;
            // blitMaterial.DepthTesting = DepthCompare.Always;
            BlitMaterialData_t data = new() { flipY = flipY ? 1 : 0 };
            blitMaterial.SetMaterialData(ref data);

            var unlitPipeline = engine.pipelineManager.CreatePipeline(unlitShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleDisabled,
                RasterizerState = RasterizerStateDescription.CullNone with {FillMode = PolygonFillMode.Wireframe},
                DepthStencilState = new DepthStencilStateDescription(true, false, ComparisonKind.LessEqual)
            }, false);

            unlitMaterial = engine.MaterialManager.CreateMaterial<UnlitMaterialData_t>(unlitPipeline);
            // unlitMaterial.ZWrite = false;
            // unlitMaterial.DepthTesting = DepthCompare.Lequal; // Always to render above the meshes
            
            // utils
            sphereMesh = engine.meshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);

            var wireframeShader = engine.shaderManager.LoadShader("data/wireframe.json");
            var wireframePipeline = engine.pipelineManager.CreatePipeline(wireframeShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleDisabled,
                RasterizerState = RasterizerStateDescription.CullNone with {FillMode = PolygonFillMode.Wireframe},
                DepthStencilState = new DepthStencilStateDescription(true, false, ComparisonKind.Always)
            }, false);

            wireframe = engine.MaterialManager.CreateMaterial<WireframeMaterialData_t>(wireframePipeline);
            WireframeMaterialData_t wireframeData = new() { width = 1, color = new Vector4(1, 1, 1, 1) };
            wireframe.SetMaterialData(ref wireframeData);
            // wireframe.ZWrite = false;
            // wireframe.DepthTesting = DepthCompare.Always;
        }

        public void Dispose()
        {
            engine.meshManager.DisposeMesh(sphereMesh);
            //outlineTexture.Dispose();
            engine.meshManager.DisposeMesh(lineMesh);
            engine.meshManager.DisposeMesh(planeMesh);
            engine.textureManager.DisposeTexture(mainObjectDepthTexture);
            engine.textureManager.DisposeTexture(mainObjectColorTexture);
            engine.textureManager.DisposeTexture(mainObjectColor1Texture);
            engine.textureManager.DisposeTexture(mainObjectBuffer);
            engine.textureManager.DisposeTexture(guiTexture);
            engine.textureManager.DisposeTexture(sceneViewOpaqueTexture2D);
            engine.textureManager.DisposeTexture(sceneViewDepthTexture2D);
            engine.textureManager.DisposeTexture(sceneViewColor1Texture);
            engine.textureManager.DisposeTexture(sceneViewTexture);
            foreach (var t in backBuffers)
                engine.textureManager.DisposeTexture(t);

            depthStencilZWrite.Dispose();
            depthStencilNoZWrite.Dispose();
            defaultSampler.Dispose();
            instancesInverseBuffer.Dispose();
            instancesBuffer.Dispose();
            instancesObjectIndicesBuffer.Dispose();
            instancesObjectDataBuffer.Dispose();
            //pixelShaderSceneBuffer.Dispose();
            objectBuffer.Dispose();
            sceneBuffer.Dispose();
        }

        public RenderLayer RegisterRenderLayer(string layerName)
        {
            if (freeLayers.Count == 0)
            {
                throw new Exception("All layers are taken!");
            }
            var freeLayer = freeLayers[^1];
            freeLayers.RemoveAt(freeLayers.Count - 1);
            var newLayer = new RenderLayer(freeLayer.Layer, freeLayer.Version + 1);
            ref var layerData = ref layers[newLayer.Layer];
            layerData.Name = layerName;
            layerData.IsDisabled = false;
            layerData.IsUsed = true;
            layerData.Layer = newLayer;

            return newLayer;
        }

        public void UnregisterRenderLayer(RenderLayer layer)
        {
            ref var layerData = ref layers[layer.Layer];
            if (layerData.Layer.Version != layer.Version)
            {
                throw new Exception("Trying to unregister an old layer");
            }

            layerData.IsUsed = false;
            freeLayers.Add(layer);
        }

        public void ToggleRenderLayer(RenderLayer layer, bool enable)
        {
            ref var layerData = ref layers[layer.Layer];
            if (layerData.Layer.Version != layer.Version)
            {
                throw new Exception("Trying to toggle an old layer");
            }

            if (enable)
            {
                layerData.IsDisabled = false;
            }
            else
            {
                layerData.IsDisabled = true;
            }
        }

        public IReadOnlyList<RenderLayerData> RenderLayers => layers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRenderLayerEnabled(byte layer)
        {
            return !layers[layer].IsDisabled;
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

        private void ActivatePipeline(Pipeline pipeline)
        {
            //engine.Device.device.Enable(EnableCap.DepthTest);
            // engine.Device.device.Enable(EnableCap.CullFace);
            // engine.Device.device.Enable(EnableCap.Blend);
            // engine.Device.device.Disable(EnableCap.Blend);
            // engine.Device.device.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            // engine.Device.device.DepthMask(true);
            //engine.Device.device.DepthFunction(DepthFunction.Lequal);

            if (currentPipeline == pipeline.Handle)
                return;
            currentPipeline = pipeline.Handle;
            var d = pipeline.Description;
            // depth testing

            if (d.DepthStencilState.DepthTestEnabled)
            {
                engine.Device.device.Enable(EnableCap.DepthTest);
            }
            else
            {
                engine.Device.device.Disable(EnableCap.DepthTest);
            }

            if (d.DepthStencilState.DepthWriteEnabled)
                engine.Device.device.DepthMask(true);
            else
                engine.Device.device.DepthMask(false);

            engine.Device.device.DepthFunction(d.DepthStencilState.DepthComparison switch
            {
                ComparisonKind.Never => DepthFunction.Never,
                ComparisonKind.Less => DepthFunction.Less,
                ComparisonKind.Equal => DepthFunction.Equal,
                ComparisonKind.LessEqual => DepthFunction.Lequal,
                ComparisonKind.Greater => DepthFunction.Greater,
                ComparisonKind.NotEqual => DepthFunction.Notequal,
                ComparisonKind.GreaterEqual => DepthFunction.Gequal,
                ComparisonKind.Always => DepthFunction.Always,
                _ => throw new ArgumentOutOfRangeException()
            });

            // return;

            // culling

            if (d.RasterizerState.CullMode == FaceCullMode.None)
            {
                engine.Device.device.Disable(EnableCap.CullFace);
            }
            else
            {
                engine.Device.device.Enable(EnableCap.CullFace);
                // yeah, it is reversed, counterclockwise vs clockwise?
                engine.Device.device.CullFace(d.RasterizerState.CullMode == FaceCullMode.Front ? CullFaceMode.Back : CullFaceMode.Front);
            }

            // blending

            if (d.BlendState.AttachmentStates[0].BlendEnabled)
            {
                engine.Device.device.Enable(EnableCap.Blend);
                var source = d.BlendState.AttachmentStates[0].SourceAlphaFactor switch
                {
                    BlendFactor.Zero => BlendingFactorSrc.Zero,
                    BlendFactor.One => BlendingFactorSrc.One,
                    BlendFactor.SourceAlpha => BlendingFactorSrc.SrcAlpha,
                    BlendFactor.InverseSourceAlpha => BlendingFactorSrc.OneMinusSrcAlpha,
                    BlendFactor.DestinationAlpha => BlendingFactorSrc.DstAlpha,
                    BlendFactor.InverseDestinationAlpha => BlendingFactorSrc.OneMinusDstAlpha,
                    BlendFactor.SourceColor => BlendingFactorSrc.SrcColor,
                    BlendFactor.InverseSourceColor => BlendingFactorSrc.OneMinusSrcColor,
                    BlendFactor.DestinationColor => BlendingFactorSrc.DstColor,
                    BlendFactor.InverseDestinationColor => BlendingFactorSrc.OneMinusSrcColor,
                    BlendFactor.BlendFactor => throw new NotImplementedException(),
                    BlendFactor.InverseBlendFactor => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                var dst = d.BlendState.AttachmentStates[0].DestinationAlphaFactor switch
                {
                    BlendFactor.Zero => BlendingFactorDest.Zero,
                    BlendFactor.One => BlendingFactorDest.One,
                    BlendFactor.SourceAlpha => BlendingFactorDest.SrcAlpha,
                    BlendFactor.InverseSourceAlpha => BlendingFactorDest.OneMinusSrcAlpha,
                    BlendFactor.DestinationAlpha => BlendingFactorDest.DstAlpha,
                    BlendFactor.InverseDestinationAlpha => BlendingFactorDest.OneMinusDstAlpha,
                    BlendFactor.SourceColor => BlendingFactorDest.SrcColor,
                    BlendFactor.InverseSourceColor => BlendingFactorDest.OneMinusSrcColor,
                    BlendFactor.DestinationColor => BlendingFactorDest.DstColor,
                    BlendFactor.InverseDestinationColor => BlendingFactorDest.OneMinusSrcColor,
                    BlendFactor.BlendFactor => throw new NotImplementedException(),
                    BlendFactor.InverseBlendFactor => throw new NotImplementedException(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                engine.Device.device.BlendFunc(source, dst);
            }
            else
            {
                engine.Device.device.Disable(EnableCap.Blend);
            }
        }

        private void DeviceEnableOrDisable(EnableCap enableCap, bool enable)
        {
            if (enable)
                engine.Device.device.Enable(enableCap);
            else
                engine.Device.device.Disable(enableCap);
        }

        public void BeginFrame()
        {
            // better not assume state was saved from the previous frame...
            currentMesh = null;
            currentPipeline = PipelineHandle.Empty;
            currentShader = null;
            currentCulling = null;
            currentZwrite = null;
            currentDepthTest = null;
            currentDepthTest = null;
            currentBlending = null;
            cameraManager.MainCamera.Aspect = engine.gameView.Aspect;
            cameraManager.SceneViewCamera.Aspect = engine.sceneView.Aspect;
        }

        public void ScreenshotCurrentBuffer(string filename, int colorAttachment = 0)
        {
            engine.textureManager.ScreenshotRenderTexture(mainObjectBuffer, filename, colorAttachment);
        }

        private Entity PickObject(Vector2 normalizedScreenPoint, ITexture renderTexture)
        {
            if (renderTexture == null)
                return Entity.Empty;
            var rt = engine.textureManager.GetTextureByHandle(renderTexture.Handle) as RenderTexture;
            if (rt == null)
            {
                return Entity.Empty;
            }
            rt.ActivateSourceFrameBuffer(1);
            Span<uint> buf = stackalloc uint[1];
            int x = (int)(normalizedScreenPoint.X * rt.Width * dynamicScale);
            int y = (int)(normalizedScreenPoint.Y * rt.Height * dynamicScale);
            engine.Device.device.ReadPixels(x, y, 1, 1, PixelFormat.RedInteger, PixelType.UnsignedInt, buf);
            var index = buf[0];
            if (index == 0)
                return Entity.Empty;
            if (index <= totalToDraw)
            {
                var entity = renderersData[index - 1].Item3;
                if (engine.entityManager.Exist(entity)) // it could be removed after rendering
                    return entity;
            }
            return Entity.Empty;
        }

        public Entity PickObject(Vector2 normalizedScreenPoint) => PickObject(normalizedScreenPoint, mainObjectBuffer);

        public Entity PickSceneViewObject()
        {
            var screenPoint = engine.inputManager.mouse.RawScreenPoint;
            if (!engine.sceneView.ViewRect.Contains(screenPoint))
            {
                return Entity.Empty;
            }
            var normalized = new Vector2((screenPoint.X - engine.sceneView.ViewRect.X) / engine.sceneView.ViewRect.Width,
                1 - (screenPoint.Y - engine.sceneView.ViewRect.Y) / engine.sceneView.ViewRect.Height);
            return PickObject(normalized, sceneViewTexture);
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
            
            ActivateScene(null);
            
            Stats = default;
            engine.Device.device.CheckError("Render begin");

            if (currentBackBufferWidth != (int)engine.gameView.ViewRect.Width ||
                currentBackBufferHeight != (int)engine.gameView.ViewRect.Height)
            {
                currentBackBufferWidth = (int)engine.gameView.ViewRect.Width;
                currentBackBufferHeight = (int)engine.gameView.ViewRect.Height;
                engine.textureManager.DisposeTexture(mainObjectDepthTexture);
                engine.textureManager.DisposeTexture(mainObjectColorTexture);
                engine.textureManager.DisposeTexture(mainObjectColor1Texture);
                engine.textureManager.DisposeTexture(mainObjectBuffer);
                engine.textureManager.DisposeTexture(opaqueRenderTexture);
                engine.textureManager.DisposeTexture(opaqueTexture2D);
                engine.textureManager.DisposeTexture(depthTexture2D);

                mainObjectDepthTexture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.DepthComponent);
                mainObjectColorTexture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.R8G8B8A8);
                mainObjectColor1Texture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.R32ui);
                mainObjectBuffer = engine.textureManager.CreateRenderTexture(mainObjectColorTexture, mainObjectDepthTexture, mainObjectColor1Texture);
                opaqueRenderTexture = engine.textureManager.CreateRenderTextureWithColorAndDepth(currentBackBufferWidth, currentBackBufferHeight, out opaqueTexture2D, out depthTexture2D);
                for (var index = 0; index < backBuffers.Length; index++)
                {
                    engine.textureManager.DisposeTexture(backBuffers[index]);
                    backBuffers[index] = engine.textureManager.CreateRenderTexture(currentBackBufferWidth, currentBackBufferHeight);
                }
            }
            if (currentSceneViewWidth != (int)engine.sceneView.ViewRect.Width ||
                currentSceneViewHeight != (int)engine.sceneView.ViewRect.Height)
            {
                currentSceneViewWidth = (int)engine.sceneView.ViewRect.Width;
                currentSceneViewHeight = (int)engine.sceneView.ViewRect.Height;
                engine.textureManager.DisposeTexture(sceneViewOpaqueTexture2D);
                engine.textureManager.DisposeTexture(sceneViewDepthTexture2D);
                engine.textureManager.DisposeTexture(sceneViewColor1Texture);
                engine.textureManager.DisposeTexture(sceneViewTexture);

                sceneViewDepthTexture2D = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.DepthComponent);
                sceneViewOpaqueTexture2D = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.R8G8B8A8);
                sceneViewColor1Texture = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.R32ui);
                sceneViewTexture = engine.textureManager.CreateRenderTexture(sceneViewOpaqueTexture2D, sceneViewDepthTexture2D, sceneViewColor1Texture);
            }

            if (currentGuiWidth != (int)engine.WindowHost.WindowWidth ||
                currentGuiHeight != (int)engine.WindowHost.WindowHeight)
            {
                currentGuiWidth = (int)engine.WindowHost.WindowWidth;
                currentGuiHeight = (int)engine.WindowHost.WindowHeight;
                engine.textureManager.DisposeTexture(guiTexture);
                guiTexture = engine.textureManager.CreateRenderTexture(currentGuiWidth, currentGuiHeight);
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
            currentBackBufferIndex = -1;

            engine.Device.device.CheckError("Before set CurrentBackBuffer");

            ActivateRenderTexture(CurrentBackBuffer, new Color4(15/255f,52/255f,97/255f, 1));

            sceneBuffer.UpdateBuffer(ref sceneData);
            sceneBuffer.Activate(Constants.SCENE_BUFFER_INDEX);

            //pixelShaderSceneBuffer.UpdateBuffer(ref scenePixelData);
            //pixelShaderSceneBuffer.Activate(Constants.PIXEL_SCENE_BUFFER_INDEX);

            objectBuffer.Activate(Constants.OBJECT_BUFFER_INDEX);
            engine.Device.device.CheckError("Before render all");
            engine.Device.device.BlendEquation(BlendEquationMode.FuncAdd);
        }

        public void PrepareRenderGui(float delta)
        {
            engine.Device.device.CheckError("PrepareRenderGui");
            ActivateRenderTexture(guiTexture, new Color4(0, 0, 0, 0));
        }

        private bool useDynamicScale = true;
        private float dynamicScale = 1;
        private float? pendingDynamicScale;
        private int DynamicWidth => useDynamicScale ? Math.Max(1, (int)(currentBackBufferWidth * dynamicScale)) : currentBackBufferWidth;
        private int DynamicHeight => useDynamicScale ? Math.Max(1, (int)(currentBackBufferHeight * dynamicScale)) : currentBackBufferHeight;

        public void ActivateRenderTexture(ITexture rt, Color4? color = null)
        {
            if (inRenderingLoop)
            {
                var tex = engine.textureManager.GetTextureByHandle(rt.Handle) as RenderTexture;
                tex!.ActivateFrameBuffer(inCoreRenderingLoop && rt == mainObjectBuffer && useDynamicScale ? dynamicScale : 1);
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
            WireframeMaterialData_t data = new() { width = 1, color = color };
            wireframe.SetMaterialData(ref data);
            Render(sphereMesh, wireframe, ShaderPassType.Forward, 0, Utilities.TRS(center, Quaternion.Identity, Vector3.One * radius));
        }

        public void RenderFullscreenPlane(Material material)
        {
            SetShader(material.GetShaderPass(ShaderPassType.Forward, false));
            EnableMaterial(material, ShaderPassType.Forward, false, null);
            SetMesh((Mesh)planeMesh);
            engine.Device.DrawIndexed(engine.meshManager.GetMeshByHandle(planeMesh.Handle).IndexCount(0), 0, 0, planeMesh.IndexType);
        }
        
        public void FinalizeRendering(int dstFrameBuffer)
        {
            ClearDirtyEntityBit();

            engine.Device.SetRenderTexture(null, dstFrameBuffer);
            engine.Device.device.Viewport(0, 0, (int)engine.WindowHost.WindowWidth, (int)engine.WindowHost.WindowHeight);
            
            engine.Device.device.CheckError("Blitz");
            blitMaterial.SetTexture("texture1", guiTexture);
            RenderFullscreenPlane(blitMaterial);
            
            inRenderingLoop = false;
            engine.statsManager.RenderStats = Stats;

            engine.Device.device.CheckError("Post rendering");
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
            engine.Device.device.Debug("  Rendering postprocesses");
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
                engine.Device.device.Debug("  Rendering postprocess");
                ActivateRenderTexture(OtherBackBuffer, Color4.White);
                post.RenderPostprocess(this, CurrentBackBuffer);
                SwapBackBuffers();
            }
            engine.Device.device.Debug("  Finished rendering postprocesses");
        }

        private struct CachedComponentDataAccess<T> where T : unmanaged, IComponentData
        {
            private IEntityManager entityManager;
            private ComponentDataAccess<T> cache = default;

            public CachedComponentDataAccess(IEntityManager em)
            {
                entityManager = em;
            }
            
            public ref T this[Entity entity]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (!cache.IsInitialized || !cache.Has(entity))
                        cache = entityManager.GetDataAccessByEntity<T>(entity);
                    return ref cache[entity];
                }
            }
        }

        public void UpdateTransforms()
        {
            var entityManager = engine.entityManager;
            dynamicParentedEntitiesArchetype.ParallelForEach<CopyParentTransform, LocalToWorld, DirtyPosition>((itr, thread, start, end, parents, localToWorld, dirtyPosition) =>
            {
                CachedComponentDataAccess<DirtyPosition> cachedDirtPosition = new CachedComponentDataAccess<DirtyPosition>(entityManager);
                CachedComponentDataAccess<LocalToWorld> cacheLocalToWorld = new CachedComponentDataAccess<LocalToWorld>(entityManager);
                for (int i = start; i < end; ++i)
                {
                    if (!dirtyPosition[i] && !cachedDirtPosition[parents[i].Parent])
                        continue;
                    ref var parentLocalToWorld = ref cacheLocalToWorld[parents[i].Parent];
                    localToWorld[i] = parentLocalToWorld;
                    if (parents[i].Local.HasValue)
                        localToWorld[i] = new LocalToWorld(){Matrix = parents[i].Local!.Value * localToWorld[i].Matrix};
                    dirtyPosition[i].Enable();
                }
            });
        }
        
        private void ClearDirtyEntityBit()
        {
            dirtEntities.ParallelForEach<DirtyPosition>((itr, thread, start, end, dirty) =>
            {
                for (int i = start; i < end; ++i)
                    dirty[i].Disable();
            });
        }

        private void EnableMaterial(Material material, ShaderPassType type, bool instancing, MaterialInstanceRenderData? instanceData = null)
        {
            ActivatePipeline(material.Pipeline);
            material.ActivateUniforms(type, instancing, instanceData);
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
                var worldspace = Vector4.Transform(vec4, matrix);
                corners[j] = worldspace.XYZ();
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
        Stopwatch sorting = new Stopwatch();
        private float viewDistanceModifier = 8;

        private (LocalToWorld, MaterialInstanceRenderData?, Entity)[] renderersData = new (LocalToWorld, MaterialInstanceRenderData?, Entity)[10000];
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
            updateWorldBoundsArchetype.ParallelForEach<LocalToWorld, MeshBounds, WorldMeshBounds, DirtyPosition>((itr, thread, start, end, l2w, meshBounds, worldMeshBounds, dirtyBit) =>
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
            toCullArchetype.ParallelForEach<LocalToWorld, PerformCullingBit, RenderEnabledBit, WorldMeshBounds>((itr, thread, start, end, l2w, _ , bits, worldMeshBounds) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (bits[i].IsForceDisabled())
                        continue;

                    if (bits[i].Layer > 0 && layers[bits[i].Layer].IsDisabled)
                        continue;
                    
                    ref var boundingBox = ref worldMeshBounds[i].box;
                    var pos = boundingBox.Center;
                    var boundingBoxSize = boundingBox.Size;
                    var size = boundingBoxSize.X + boundingBoxSize.Y + boundingBoxSize.Z;
                    size = MathF.Sqrt(Math.Max(size, 10));
                    bool doRender = (pos - cameraPosition).LengthSquared() < (size) * (size) * (size) * mod;
                    if (doRender)
                    {
                        bits[i].IsCulled = frustum.Contains(ref boundingBox) == ContainmentType.Disjoint;
                    }
                    else
                        bits[i].IsCulled = true;
                }
            });
            dynamicParentedEntitiesArchetype.WithComponentData<RenderEnabledBit>().ParallelForEach<RenderEnabledBit, CopyParentTransform>((itr, thread, start, end, renderBit, cpt) =>
            {
                CachedComponentDataAccess<RenderEnabledBit> cache = new CachedComponentDataAccess<RenderEnabledBit>(entityManager);
                for (int i = start; i < end; ++i)
                {
                    var parent = cpt[i];
                    if (parent.Parent != Entity.Empty)
                    {
                        var forceDisabled = renderBit[i].IsForceDisabled();
                        renderBit[i] = cache[parent.Parent];
                        if (forceDisabled)
                            renderBit[i].SetDisabled(true);
                    }
                }
            });
            entitiesSharingRenderingArchetype.ParallelForEach<RenderEnabledBit, ShareRenderEnabledBit>(
                (itr, thread, start, end, renderBits, shareRenderBits) =>
                {
                    CachedComponentDataAccess<RenderEnabledBit> cache = new CachedComponentDataAccess<RenderEnabledBit>(entityManager);
                    for (int i = start; i < end; ++i)
                    {
                        if (!renderBits[i] && shareRenderBits[i].OtherEntity != Entity.Empty)
                        {
                            renderBits[i] = cache[shareRenderBits[i].OtherEntity];
                        }
                    }
                });
            culler.Stop();
            engine.statsManager.Counters.Culling.Add(culler.Elapsed.TotalMilliseconds);

            ThreadLocal<(int opaque, int transparent)> count = new(true);
            toRenderArchetype.ParallelForEach<RenderEnabledBit, MeshRenderer>((itr, thread, start, end, renderBit, renderers) =>
            {
                int opaque = 0;
                int transparent = 0;
                for (int i = start; i < end; ++i)
                {
                    if (!renderBit[i])
                        continue;

                    if (renderBit[i].Layer > 0 && layers[renderBit[i].Layer].IsDisabled)
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
            if (renderersData.Length < totalToDraw)
            {
                renderersData = new (LocalToWorld, MaterialInstanceRenderData?, Entity)[totalToDraw];
                renderers = new MeshRenderer[totalToDraw];
                //materialInstanceData = new MaterialInstanceRenderData[totalToDraw];
            }
            int opaqueIndex = 0;
            int transparentIndex = opaque;
            toRenderArchetype.ForEachRRRO<LocalToWorld, RenderEnabledBit, MeshRenderer, MaterialInstanceRenderData>((itr, thread, start, end, l2w, render, meshRenderer, materialData) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (!render[i])
                        continue;

                    if (render[i].Layer > 0 && layers[render[i].Layer].IsDisabled)
                        continue;

                    var renderer = meshRenderer[i];

                    if (renderer.Opaque)
                    {
                        renderers[opaqueIndex] = renderer;
                        //materialInstanceData[opaqueIndex] = materialData?[i];
                        renderersData[opaqueIndex++] = (l2w[i], materialData?[i], itr[i]);
                    }
                    else
                    {
                        renderers[transparentIndex] = renderer;
                        //materialInstanceData[transparentIndex] = materialData?[i];
                        renderersData[transparentIndex++] = (l2w[i], materialData?[i], itr[i]);
                    }
                }
            });

            sorting.Restart();
            SortRenderersByMesh(0, opaque);
            SortRenderersByMesh(opaque + 1, totalToDraw);
            sorting.Stop();
            engine.statsManager.Counters.Sorting.Add(sorting.Elapsed.TotalMilliseconds);

            if (engine.gameView.IsVisible)
            {
                Render(ShaderPassType.Forward, 0, opaque, false);
            }

            if (engine.sceneView.IsVisible)
            {
                ActivateScene(new SceneData(cameraManager.SceneViewCamera, new FogSettings(){Enabled = false}, engine.lightManager.MainLight, engine.lightManager.SecondaryLight));
                ActivateRenderTexture(sceneViewTexture, new Color4(15/255f,52/255f,97/255f, 1));
                Render(ShaderPassType.Forward, 0, opaque, false);
                // restore current back buffer and scene
                ActivateScene(null);
                ActivateRenderTexture(CurrentBackBuffer);
            }
        }

        private void SortRenderersByMesh(int start, int end)
        {
            if (end <= start)
                return;
            Array.Sort(renderers, renderersData, start, end - start, Comparer<MeshRenderer>.Create((a, b) =>
            {
                if (a.PipelineHandle != b.PipelineHandle)
                {
                    return a.PipelineHandle.Handle.CompareTo(b.PipelineHandle.Handle);
                }
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
            engine.textureManager.BlitFramebuffers(mainObjectBuffer, opaqueRenderTexture, 0, 0, currentBackBufferWidth, currentBackBufferHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            ActivateDefaultRenderTexture();

            if (engine.gameView.IsVisible)
            {
                Render(ShaderPassType.Forward, opaque, totalToDraw, true);
            }

            if (engine.sceneView.IsVisible)
            {
                ActivateScene(new SceneData(cameraManager.SceneViewCamera, new FogSettings(){Enabled = false}, engine.lightManager.MainLight, engine.lightManager.SecondaryLight));
                ActivateRenderTexture(sceneViewTexture, null);
                Render(ShaderPassType.Forward, opaque, totalToDraw, true);
                var mainCamFrustum = new BoundingFrustum(cameraManager.MainCamera.ViewMatrix * cameraManager.MainCamera.ProjectionMatrix);
                this.DrawFrustum(mainCamFrustum, Vector4.One);
                engine.sceneView.OnSceneViewRender();
                // restore current back buffer and scene
                ActivateScene(null);
                ActivateRenderTexture(CurrentBackBuffer);
            }
        }

        private bool enableInstancing = true;

        private void Render(ShaderPassType shaderPassType, int start, int end, bool transparent)
        {
            engine.Device.device.Debug(transparent ? "  Rendering translucent" : "  Rendering opaque");
            sw.Restart();
            int savedByInstancing = 0;
            for (int i = start; i < end; ++i)
            {
                var mr = renderers[i];
                var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
                var shader = material.GetShaderPass(shaderPassType, false);
                var shaderInstanced = material.GetShaderPass(shaderPassType, true);
                var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);
                var meshId = mr.SubMeshId;

                var toBatch = 0;
                if (shaderInstanced != null && // shader supports instancing
                    enableInstancing &&                 // instancing is enabled
                    renderersData[i].Item2 == null)     // no material per instance data
                {
                    int j = i + 1;
                    while (j < end && renderers[j].MeshHandle == mr.MeshHandle &&
                           renderers[j].SubMeshId == mr.SubMeshId &&
                           renderers[j].MaterialHandle == mr.MaterialHandle &&
                           renderersData[j].Item2 == null)
                    {
                        toBatch++;
                        j++;
                    }   
                }

                if (toBatch <= 2)
                {
                    SetShader(shader);
                    SetMesh(mesh);
                
                    //materialtimer.Start();
                    EnableMaterial(material, ShaderPassType.Forward, false, renderersData[i].Item2);
                    //materialtimer.Stop();
                    
                    //buffertimer.Start();
                    objectData.WorldMatrix = renderersData[i].Item1;
                    objectData.InverseWorldMatrix = renderersData[i].Item1.Inverse;
                    objectData.ObjectIndex = (uint)i + 1;
                    objectData.DrawData = renderersData[i].Item2?.InstanceData ?? new Int4(-1, -1, -1, -1);
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
                    engine.Device.DrawIndexed(indicesCount, mesh.IndexStart(meshId), 0, mesh.IndexType);
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
                        instancesObjectInddicesArray = new uint[toBatch + 1];
                        instancesObjectDataArray = new Int4[toBatch + 1];
                    }

                    for (int k = 0; k < toBatch + 1; ++k)
                    {
                        instancesArray[k] = renderersData[i + k].Item1.Matrix;
                        inverseInstancesArray[k] = renderersData[i + k].Item1.Inverse;
                        instancesObjectInddicesArray[k] = (uint)(i + k);
                        instancesObjectDataArray[k] = renderersData[i + k].Item2?.InstanceData ?? new Int4(-1, -1, -1, -1);
                    }

                    //buffertimer.Start();
                    //var instancesBuffer =
                    //    engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, worldMatrices, BufferInternalFormat.Float4);
                    //var instancesInverseBuffer =
                    //    engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, inerseWorldMatrices, BufferInternalFormat.Float4);
                    instancesBuffer.UpdateBuffer(instancesArray.AsSpan(0, toBatch + 1));
                    instancesInverseBuffer.UpdateBuffer(inverseInstancesArray.AsSpan(0, toBatch + 1));
                    instancesObjectIndicesBuffer.UpdateBuffer(instancesObjectInddicesArray.AsSpan(0, toBatch + 1));
                    instancesObjectDataBuffer.UpdateBuffer(instancesObjectDataArray.AsSpan(0, toBatch + 1));
                    //buffertimer.Stop();

                    shader = shaderInstanced;

                    SetShader(shader);

                    SetMesh(mesh);

                    //materialtimer.Start();
                    instancingRenderData.Clear();
                    instancingRenderData.SetBuffer("InstancingModels", instancesBuffer);
                    instancingRenderData.SetBuffer("InstancingInverseModels", instancesInverseBuffer);
                    instancingRenderData.SetBuffer("ObjectIndices", instancesObjectIndicesBuffer);
                    instancingRenderData.SetBuffer("DrawData", instancesObjectDataBuffer);

                    EnableMaterial(material, ShaderPassType.Forward,  true, instancingRenderData);
                    //materialtimer.Stop();

#if DEBUG
                    currentShader.Validate();
#endif
                    //draw.Start();
                    var indicesCount = mesh.IndexCount(meshId);
                    Stats.IndicesDrawn += indicesCount * (toBatch + 1);
                    Stats.TrianglesDrawn += (indicesCount / 3) * (toBatch + 1);
                    Stats.InstancedDraws++;
                    engine.Device.DrawIndexedInstanced(indicesCount,  toBatch + 1, mesh.IndexStart(meshId), 0, 0, mesh.IndexType);

                    i += toBatch;
                }
            }
            sw.Stop();
            engine.statsManager.Counters.Drawing.Add(sw.Elapsed.TotalMilliseconds);
            Stats.InstancedDrawSaved += savedByInstancing;
        }

        private void SetShader(ShaderPass shader)
        {
            if (currentShader != shader)
            {
                Stats.ShaderSwitches++;
                currentShader = shader;
                //shadertimer.Start();
                shader.Activate();
                //shadertimer.Stop();
            }
        }

        internal void SetMesh(Mesh? mesh)
        {
            if (currentMesh != mesh)
            {
                Stats.MeshSwitches++;
                currentMesh = mesh;
                //meshtimer.Start();
                mesh?.Activate();
                //meshtimer.Stop();
            }
        }

        public void Render(MeshHandle meshHandle, MaterialHandle materialHandle, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var material = engine.materialManager.GetMaterialByHandle(materialHandle);
            Render(mesh, material, shaderPassType, submesh, localToWorld, worldToLocal, instanceData);
        }
        
        public void Render(IMesh mesh, Material material, ShaderPassType shaderPass, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null)
        {
            if (worldToLocal == null)
            {
                Matrix.Invert(localToWorld, out var  worldToLocal_);
                worldToLocal = worldToLocal_;
            }
            
            Debug.Assert(inRenderingLoop);
            SetShader(material.GetShaderPass(shaderPass, false));
            EnableMaterial(material, shaderPass, false, instanceData);
            SetMesh((Mesh)mesh);
            objectData.WorldMatrix = localToWorld;
            objectData.InverseWorldMatrix = worldToLocal.Value;
            objectBuffer.UpdateBuffer(ref objectData);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexed(count, start, 0, mesh.IndexType);
        }

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color)
        {
            lineMesh.SetVertices(start, end);
            lineMesh.RebuildIndices();
            SetShader(unlitMaterial.GetShaderPass(ShaderPassType.Forward, false));
            UnlitMaterialData_t data = new() { color = color };
            unlitMaterial.SetMaterialData(ref data);
            EnableMaterial(unlitMaterial, ShaderPassType.Forward, false);
            SetMesh(lineMesh);
            objectData.WorldMatrix = Matrix.Identity;
            objectData.InverseWorldMatrix = Matrix.Identity;
            objectBuffer.UpdateBuffer(ref objectData);
            engine.Device.DrawLineMesh(2, 0);
        }

        public void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Transform transform)
        {
            Render(mesh, material, shaderPassType, submesh, transform.LocalToWorldMatrix, transform.WorldToLocalMatrix);
        }

        public void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Vector3 position)
        {
            var matrix = Matrix.CreateTranslation(position);
            Render(mesh, material, shaderPassType, submesh, matrix);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount, Matrix localToWorld, Matrix? worldToLocal = null)
        {
            if (!worldToLocal.HasValue)
            {
                Matrix.Invert(localToWorld, out var worldToLocal_);
                worldToLocal = worldToLocal_;
            }
            SetShader(material.GetShaderPass(shaderPassType, false));
            EnableMaterial(material, shaderPassType, false);
            objectData.WorldMatrix = localToWorld;
            objectData.InverseWorldMatrix = worldToLocal.Value;
            objectBuffer.UpdateBuffer(ref objectData);
            SetMesh((Mesh)mesh);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexedInstanced(count, instancesCount, start, 0, 0, mesh.IndexType);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount)
        {
            SetShader(material.GetShaderPass(shaderPassType, false));
            EnableMaterial(material, shaderPassType, false);
            SetMesh((Mesh)mesh);
            var start = mesh.IndexStart(submesh);
            var count = mesh.IndexCount(submesh);
            engine.Device.DrawIndexedInstanced(count, instancesCount, start, 0, 0, mesh.IndexType);
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

        public void ActivateScene(in SceneData? scene)
        {
            var data = scene ?? new SceneData(engine.cameraManger.MainCamera,
                engine.lightManager.Fog,
                engine.lightManager.MainLight,
                engine.lightManager.SecondaryLight);
            UpdateSceneBuffer(in data);
            sceneBuffer.UpdateBuffer(ref sceneData);
            sceneBuffer.Activate(Constants.SCENE_BUFFER_INDEX);
        }

        private void UpdateSceneBuffer(in SceneData data)
        {
            var camera = data.SceneCamera;
            var proj = camera.ProjectionMatrix;
            var vm = camera.Transform.WorldToLocalMatrix;

            sceneData.ViewMatrix = vm;
            sceneData.ProjectionMatrix = proj;
            Matrix.Invert(vm, out var vmInv);
            Matrix.Invert(proj, out var projInv);
            sceneData.ViewMatrixInverse = vmInv;
            sceneData.ProjectionMatrixInverse = projInv;
            sceneData.LightPosition = data.MainLight.LightPosition;
            sceneData.CameraPosition = new Vector4(camera.Transform.Position, 1);
            sceneData.LightDirection = new Vector4(Vectors.Normalize((Vectors.Forward.Multiply(data.MainLight.LightRotation))), 0);
            sceneData.LightColor = data.MainLight.LightColor.XYZ();
            sceneData.LightIntensity = data.MainLight.LightIntensity; 
            sceneData.SecondaryLightDirection = new Vector4(Vectors.Forward.Multiply(data.SecondaryLight.LightRotation), 0);
            sceneData.SecondaryLightColor = data.SecondaryLight.LightColor.XYZ();
            sceneData.SecondaryLightIntensity = data.SecondaryLight.LightIntensity;
            sceneData.AmbientColor = data.MainLight.AmbientColor;
            sceneData.fogStart = data.Fog.Start;
            sceneData.fogEnd = data.Fog.End;
            sceneData.fogColor = data.Fog.Color;
            sceneData.fogEnabled = data.Fog.Enabled ? 1 : 0;
            sceneData.Time = (float)engine.TotalTime;
            sceneData.ZNear = camera.NearClip;
            sceneData.ZFar = camera.FarClip;
            sceneData.ScreenWidth = engine.gameView.ViewRect.Width;
            sceneData.ScreenHeight = engine.gameView.ViewRect.Height;
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
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Material = material;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Mesh = mesh;
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
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Material = material;
            engine.EntityManager.GetComponent<MeshRenderer>(entity).Mesh = mesh;
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
            renderManager.DrawLine(ray.Position + ray.Direction - Vectors.Left * 0.5f, ray.Position + ray.Direction, Color4.White);
            renderManager.DrawLine(ray.Position + ray.Direction - Vectors.Forward * 0.5f, ray.Position + ray.Direction, Color4.White);
        }
    }
}
