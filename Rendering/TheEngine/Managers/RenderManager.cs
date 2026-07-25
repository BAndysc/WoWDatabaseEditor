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
using TheEngine.Rendering;
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
        private readonly ICommandList immediateCommandList;
        private readonly DeferredCommandList deferredCommandList;
        private readonly EngineCommandList immediateEngineCommandList;
        private readonly EngineCommandList deferredEngineCommandList;
        private ICommandList commandList;
        private EngineCommandList engineCommandList;
        private bool deferredRecording;
        private bool? pendingDeferredRecording;

        private readonly ObjectDrawRenderStage objectDrawStage;
        private readonly LinesRenderStage linesStage;
        private readonly List<IRenderStage> stages = new();

        private SceneBuffer sceneData;
        //private PixelShaderSceneBuffer scenePixelData;

        private ICameraManager cameraManager;

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

        // utils
        private IMesh sphereMesh = null!;
        private Material<WireframeMaterialData_t> wireframe = null!;
        // end utils

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

        internal RenderManager(Engine engine, bool flipY)
        {
            this.engine = engine;
            this.flipY = flipY;
            immediateCommandList = engine.Backend.CreateExecutor(engine.textureManager);
            deferredCommandList = new DeferredCommandList(immediateCommandList);
            immediateEngineCommandList = new EngineCommandList(immediateCommandList);
            deferredEngineCommandList = new EngineCommandList(deferredCommandList);
            deferredRecording = true;
            commandList = deferredCommandList;
            engineCommandList = deferredEngineCommandList;

            layers[0].Name = "(default)";
            freeLayers = layers.Skip(1).Reverse().Select(x => x.Layer).ToList();

            cameraManager = engine.CameraManager;

            dirtEntities = engine.entityManager.NewArchetype()
                .WithComponentData<DirtyPosition>();

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

            objectDrawStage = new ObjectDrawRenderStage(engine, this);
            stages.Add(objectDrawStage);
            linesStage = new LinesRenderStage(engine);
            stages.Add(linesStage);

            sceneData = new SceneBuffer();

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            commandList.CheckError("create mesh");

            var blitShader = engine.ShaderManager.LoadShader("internalShaders/blit.json");
            // var blitDepthShader = engine.ShaderManager.LoadShader("internalShaders/blit_depth.json");

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

            // utils
            sphereMesh = engine.meshManager.CreateMesh(ObjParser.LoadObj("meshes/sphere.obj").MeshData);

            // the engine must be self-contained: its shaders live in internalShaders
            // (games may ship their own copy in data/ for their own materials)
            var wireframeShader = engine.shaderManager.LoadShader("internalShaders/wireframe.json");
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
            objectDrawStage.Dispose();
            linesStage.Dispose();
            engine.meshManager.DisposeMesh(sphereMesh);
            //outlineTexture.Dispose();
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

            deferredCommandList.Dispose();
            immediateCommandList.Dispose();
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

        internal RenderLayerData[] LayersArray => layers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRenderLayerEnabled(byte layer)
        {
            return !layers[layer].IsDisabled;
        }

        public void RegisterRenderStage(IRenderStage stage)
        {
            Debug.Assert(!inRenderingLoop, "render stages must be (un)registered outside the render loop");
            stages.Add(stage);
        }

        public void UnregisterRenderStage(IRenderStage stage)
        {
            Debug.Assert(!inRenderingLoop, "render stages must be (un)registered outside the render loop");
            stages.Remove(stage);
        }


        public bool DeferredRecording
        {
            get => deferredRecording;
            set => pendingDeferredRecording = value;
        }

        public void BeginFrame()
        {
            // the recording mode can only change between frames, when no commands are in flight
            if (pendingDeferredRecording.HasValue)
            {
                deferredRecording = pendingDeferredRecording.Value;
                pendingDeferredRecording = null;
                commandList = deferredRecording ? deferredCommandList : immediateCommandList;
                engineCommandList = deferredRecording ? deferredEngineCommandList : immediateEngineCommandList;
            }
            commandList.Begin();
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
            Span<uint> buf = stackalloc uint[1];
            int x = (int)(normalizedScreenPoint.X * rt.Width * dynamicScale);
            int y = (int)(normalizedScreenPoint.Y * rt.Height * dynamicScale);
            commandList.ReadPixels(renderTexture, 1, x, y, 1, 1, buf);
            var index = buf[0];
            if (index == 0)
                return Entity.Empty;
            if (index <= objectDrawStage.TotalToDraw)
            {
                var entity = objectDrawStage.EntityAtIndex(index - 1);
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
            commandList.CheckError("pre UpdateSceneBuffer");
            
            ActivateScene(null);

            engineCommandList.ResetStats();
            commandList.CheckError("Render begin");

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

            commandList.CheckError("Before set CurrentBackBuffer");

            ActivateRenderTexture(CurrentBackBuffer, new Color4(15/255f,52/255f,97/255f, 1));

            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);

            //commandList.BindTransientUniformBuffer(Constants.PIXEL_SCENE_BUFFER_INDEX, ref scenePixelData);

            // bind last frame's leftover object data, so that the slot is never unbound
            engineCommandList.RebindObjectData();
            commandList.CheckError("Before render all");
        }

        public void PrepareRenderGui(float delta)
        {
            commandList.CheckError("PrepareRenderGui");
            // the gui pass samples the game/scene view images through ImGui draws
            if (commandList.InRenderingPass)
                commandList.EndRenderingPass();
            commandList.Barrier(CurrentBackBuffer, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
            if (engine.sceneView.IsVisible && sceneViewTexture != null)
                commandList.Barrier(sceneViewTexture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
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
                // transitional convenience: a target switch implicitly ends the current pass,
                // so callers don't have to manage pass boundaries themselves yet
                if (commandList.InRenderingPass)
                    commandList.EndRenderingPass();
                commandList.BeginRenderingPass(new RenderPassDescriptor
                {
                    Target = rt,
                    ColorLoadOp = color.HasValue ? LoadOp.Clear : LoadOp.Load,
                    ClearColor = color ?? default,
                    ViewportScale = inCoreRenderingLoop && rt == mainObjectBuffer && useDynamicScale ? dynamicScale : 1,
                });
            }
        }
        
        public void ActivateDefaultRenderTexture()
        {
            ActivateRenderTexture(CurrentBackBuffer);
        }

        public void BlitRenderTextures(ITexture source, ITexture destination)
        {
            Debug.Assert(inRenderingLoop);
            // blits are only legal outside a rendering pass; the caller activates
            // the next render target (= begins the next pass) afterwards
            if (commandList.InRenderingPass)
                commandList.EndRenderingPass();
            commandList.Barrier(source, ResourceUsage.RenderTarget, ResourceUsage.TransferSource);
            commandList.Barrier(destination, ResourceUsage.ShaderRead, ResourceUsage.TransferDestination);
            commandList.Blit(source, destination, 0, 0, source.Width, source.Height, 0, 0, destination.Width, destination.Height, BlitMask.Color, BlitFilter.Linear);
            commandList.Blit(source, destination, 0, 0, source.Width, source.Height, 0, 0, destination.Width, destination.Height, BlitMask.Depth, BlitFilter.Nearest);
            commandList.Barrier(source, ResourceUsage.TransferSource, ResourceUsage.RenderTarget);
            commandList.Barrier(destination, ResourceUsage.TransferDestination, ResourceUsage.ShaderRead);
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
            EnableMaterial(material, material.GetShaderPass(ShaderPassType.Forward, false)!);
            engineCommandList.DrawIndexed(planeMesh, 0);
        }
        
        public void FinalizeRendering(int dstFrameBuffer)
        {
            foreach (var stage in stages)
                stage.EndFrame();

            ClearDirtyEntityBit();

            if (commandList.InRenderingPass)
                commandList.EndRenderingPass();
            commandList.Barrier(guiTexture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
            commandList.BeginRenderingPass(new RenderPassDescriptor
            {
                Target = null,
                DefaultFramebuffer = dstFrameBuffer,
                Width = (int)engine.WindowHost.WindowWidth,
                Height = (int)engine.WindowHost.WindowHeight,
                ColorLoadOp = LoadOp.Load,
            });

            commandList.CheckError("Blitz");
            blitMaterial.SetTexture("texture1", guiTexture);
            RenderFullscreenPlane(blitMaterial);
            commandList.EndRenderingPass();

            inRenderingLoop = false;

            commandList.CheckError("Post rendering");
            // with deferred recording the frame actually executes inside End (record + replay),
            // so the executor's switch counters are only meaningful afterwards
            commandList.End();

            var stats = engineCommandList.Stats;
            stats.ShaderSwitches += commandList.ShaderSwitches;
            stats.MeshSwitches += commandList.MeshSwitches;
            engine.statsManager.RenderStats = stats;
        }

        internal void RenderOpaque(int dstFrameBuffer)
        {
            foreach (var stage in stages)
                stage.PrepareFrame(cameraManager.MainCamera);

            RenderStages(RenderPoint.Opaque);
        }

        internal void RenderTransparent(int dstFrameBuffer)
        {
            // copy the opaque result aside, so transparent shaders can sample the opaque color/depth
            commandList.EndRenderingPass();
            commandList.Barrier(mainObjectBuffer, ResourceUsage.RenderTarget, ResourceUsage.TransferSource);
            commandList.Barrier(opaqueRenderTexture, ResourceUsage.ShaderRead, ResourceUsage.TransferDestination);
            commandList.Blit(mainObjectBuffer, opaqueRenderTexture, 0, 0, currentBackBufferWidth, currentBackBufferHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, BlitMask.Color | BlitMask.Depth, BlitFilter.Nearest);
            commandList.Barrier(opaqueRenderTexture, ResourceUsage.TransferDestination, ResourceUsage.ShaderRead);
            commandList.Barrier(mainObjectBuffer, ResourceUsage.TransferSource, ResourceUsage.RenderTarget);
            ActivateDefaultRenderTexture();

            RenderStages(RenderPoint.Transparent);
        }

        /// <summary>
        /// Invokes every stage subscribed to the given render point, once per visible view
        /// (game view with the main camera, scene view with the scene camera). The scene
        /// view's target, scene data and clear are managed here, so stages just record draws.
        /// </summary>
        private void RenderStages(RenderPoint point)
        {
            if (engine.gameView.IsVisible)
            {
                foreach (var stage in stages)
                    if ((stage.RenderPoints & point) != 0)
                        stage.Render(point, engineCommandList, cameraManager.MainCamera);
            }

            // the old immediate DrawLine drew into the back buffer regardless of the game view's
            // visibility, so pending lines are flushed there even when the stage loop was skipped
            if (point == RenderPoint.Transparent)
                linesStage.Flush(engineCommandList);

            if (engine.sceneView.IsVisible)
            {
                ActivateScene(new SceneData(cameraManager.SceneViewCamera, new FogSettings(){Enabled = false}, engine.lightManager.MainLight, engine.lightManager.SecondaryLight));
                ActivateRenderTexture(sceneViewTexture, point == RenderPoint.Opaque ? new Color4(15/255f,52/255f,97/255f, 1) : null);

                foreach (var stage in stages)
                    if ((stage.RenderPoints & point) != 0)
                        stage.Render(point, engineCommandList, cameraManager.SceneViewCamera);

                if (point == RenderPoint.Transparent)
                {
                    var mainCamFrustum = new BoundingFrustum(cameraManager.MainCamera.ViewMatrix * cameraManager.MainCamera.ProjectionMatrix);
                    this.DrawFrustum(mainCamFrustum, Vector4.One);
                    engine.sceneView.OnSceneViewRender();
                    // the frustum and the scene-view overlay only queue lines - flush them
                    // into the scene view before its target is deactivated
                    linesStage.Flush(engineCommandList);
                }

                // restore current back buffer and scene
                ActivateScene(null);
                ActivateRenderTexture(CurrentBackBuffer);
            }
        }

        internal void RenderPostProcess()
        {
            // the game's translucent callback (gizmos, path visualizers) runs after
            // RenderTransparent, so its lines are flushed here, before the upscale
            linesStage.Flush(engineCommandList);

            commandList.InsertDebugMarker("  Rendering postprocesses");
            inCoreRenderingLoop = false;
            if (useDynamicScale)
            {
                // upscale the dynamically-scaled image to a full resolution back buffer
                commandList.EndRenderingPass();
                commandList.Barrier(CurrentBackBuffer, ResourceUsage.RenderTarget, ResourceUsage.TransferSource);
                commandList.Barrier(OtherBackBuffer, ResourceUsage.ShaderRead, ResourceUsage.TransferDestination);
                commandList.Blit(CurrentBackBuffer, OtherBackBuffer, 0, 0, DynamicWidth, DynamicHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, BlitMask.Color, BlitFilter.Linear);
                commandList.Blit(CurrentBackBuffer, OtherBackBuffer, 0, 0, DynamicWidth, DynamicHeight, 0, 0, currentBackBufferWidth, currentBackBufferHeight, BlitMask.Depth, BlitFilter.Nearest);
                commandList.Barrier(OtherBackBuffer, ResourceUsage.TransferDestination, ResourceUsage.RenderTarget);

                SwapBackBuffers();
                ActivateDefaultRenderTexture(); // to make sure we set the correct viewport
            }

            foreach (var post in postProcesses)
            {
                commandList.InsertDebugMarker("  Rendering postprocess");
                if (commandList.InRenderingPass)
                    commandList.EndRenderingPass();
                // the previous back buffer is sampled by the postprocess while the other one is rendered to
                commandList.Barrier(CurrentBackBuffer, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
                commandList.Barrier(OtherBackBuffer, ResourceUsage.ShaderRead, ResourceUsage.RenderTarget);
                ActivateRenderTexture(OtherBackBuffer, Color4.White);
                post.RenderPostprocess(this, CurrentBackBuffer);
                SwapBackBuffers();
            }
            commandList.InsertDebugMarker("  Finished rendering postprocesses");
        }

        internal struct CachedComponentDataAccess<T> where T : unmanaged, IComponentData
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
                    if (parents[i].Parent == Entity.Empty)
                        continue;
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

        private void EnableMaterial(Material material, IShaderPass shaderPass, MaterialInstanceRenderData? instanceData = null)
        {
            engineCommandList.SetMaterial(material, shaderPass, instanceData);
        }

        public static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld)
        {
            return WorldMeshBounds.FromLocal(in local, in localToWorld);
        }

        internal static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld, ref Span<Vector3> corners)
        {
            return WorldMeshBounds.FromLocal(in local, in localToWorld, ref corners);
        }
        
        private float viewDistanceModifier = 8;


        // for engine-internal renderers (UIManager, ImGuiController) that record their own commands
        internal ICommandList CommandList => commandList;

        public void Render(MeshHandle meshHandle, MaterialHandle materialHandle, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null, Int4? instanceInt = null)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var material = engine.materialManager.GetMaterialByHandle(materialHandle);
            Render(mesh, material, shaderPassType, submesh, localToWorld, worldToLocal, instanceData, instanceInt);
        }
        
        public void Render(IMesh mesh, Material material, ShaderPassType shaderPass, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null, Int4? instanceInt = null)
        {
            if (worldToLocal == null)
            {
                Matrix.Invert(localToWorld, out var  worldToLocal_);
                worldToLocal = worldToLocal_;
            }
            
            Debug.Assert(inRenderingLoop);
            EnableMaterial(material, material.GetShaderPass(shaderPass, false)!, instanceData);
            engineCommandList.SetObjectData(localToWorld, worldToLocal.Value, 0, instanceInt);
            engineCommandList.DrawIndexed(mesh, submesh);
        }

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color)
        {
            linesStage.Add(start, end, color);
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

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null)
        {
            if (!worldToLocal.HasValue)
            {
                Matrix.Invert(localToWorld, out var worldToLocal_);
                worldToLocal = worldToLocal_;
            }
            EnableMaterial(material, material.GetShaderPass(shaderPassType, false)!, instanceData);
            engineCommandList.SetObjectData(localToWorld, worldToLocal.Value);
            engineCommandList.DrawIndexedInstanced(mesh, submesh, instancesCount);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount, MaterialInstanceRenderData? instanceData = null)
        {
            EnableMaterial(material, material.GetShaderPass(shaderPassType, false)!, instanceData);
            engineCommandList.DrawIndexedInstanced(mesh, submesh, instancesCount);
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
            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
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

        public void SetupRendererEntity(Entity entity, MeshHandle meshHandle, Material material, int subMesh, Matrix localToWorld, Int4? instanceData = null)
        {
            var l2w = new LocalToWorld() { Matrix = localToWorld };
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            engine.EntityManager.GetComponent<LocalToWorld>(entity) = l2w;
            MeshRenderer renderer = new() { SubMeshId = subMesh, Material = material, Mesh = mesh, Opaque = !material.BlendingEnabled, InstanceData = instanceData};
            engine.EntityManager.AddArrayComponent(entity, renderer);
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
            MeshRenderer renderer = new() { SubMeshId = subMesh, Material = material, Mesh = mesh, Opaque = !material.BlendingEnabled };
            engine.EntityManager.AddArrayComponent(entity, renderer);
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
