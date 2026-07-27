using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Primitives;
using TheEngine.Rendering;
using TheEngine.Structures;
using TheEngine.Vulkan;
using TheMaths;
using Veldrid;

namespace TheEngine.Managers
{
    public partial class RenderManager : IRenderManager, IDisposable
    {
        private readonly Engine engine;
        private readonly bool flipY;
        private readonly ICommandList immediateCommandList;
        private readonly EngineCommandList immediateEngineCommandList;
        private ICommandList commandList;
        private EngineCommandList engineCommandList;

        private readonly ObjectDrawRenderStage objectDrawStage;
        private readonly CustomObjectDrawRenderStage additionalRenderersStage;
        private readonly LinesRenderStage linesStage;
        private readonly List<IRenderStage> stages = new();
        // Directional-light cascaded shadow maps - the depth maps are shared between the game and
        // scene views, re-fit and re-rendered before each view's opaque pass (see RenderShadowCascades).
        private readonly CascadedShadowMapManager csm;

        // First enabled CascadeShadowMap entity, resolved per frame; null == shadows off this frame.
        private CascadeShadowMap? activeShadowSettings;

        /// <summary>Max distance shadow casters are collected to (0 when off); read by <see cref="ObjectDrawRenderStage"/>.</summary>
        internal float ShadowDistance => activeShadowSettings?.Split3 ?? 0f;

        /// <summary>What the views display: the final image, or a debug visualization of an intermediate
        /// buffer (depth, shadow map, Forward+ light/decal complexity). Set from the per-view toolbar.</summary>
        public DebugView DebugView { get; set; } = DebugView.FinalImage;

        // "render once" requests scheduled via RenderOnce during Update; drained each frame by
        // additionalRenderersStage and cleared in FinalizeRendering.
        private readonly List<(LocalToWorld, MeshRenderer)> additionalRenderers = new();

        private SceneBuffer sceneData;

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
        private ITexture depthPrepassTexture;
        private ITexture mainObjectColorTexture;
        private ITexture mainObjectColor1Texture;
        private ITexture guiTexture;
        private ITexture sceneViewColor1Texture;
        private ITexture sceneViewTexture;
        // depth-only alias of sceneViewDepthTexture2D, used purely to run a standalone depth
        // prepass for the scene view's own Forward+ tile culling - mirrors depthPrepassTexture's
        // relationship to mainObjectDepthTexture.
        private ITexture sceneViewDepthPrepassTexture;
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

        // debug-view fullscreen pass (depth / shadow / Forward+ complexity); rendered into the
        // per-view debug textures when DebugView != FinalImage and displayed instead of the color.
        private Material<DebugMaterialData_t> debugMaterial = null!;
        private ITexture gameDebugTexture = null!;
        private ITexture sceneDebugTexture = null!;
        internal ITexture GameDebugTexture => gameDebugTexture;
        internal ITexture SceneDebugTexture => sceneDebugTexture;
        // set each frame by RenderDebugViews; the views only DISPLAY a debug texture that was actually
        // rendered + transitioned to ShaderRead this frame (IsVisible lags a frame, so the render/display
        // gates must agree or the GUI would sample a texture in the wrong layout -> Metal abort).
        private bool gameDebugRendered;
        private bool sceneDebugRendered;
        internal bool GameDebugReady => DebugView != DebugView.FinalImage && gameDebugRendered;
        internal bool SceneDebugReady => DebugView != DebugView.FinalImage && sceneDebugRendered;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct DebugMaterialData_t
        {
            public int mode;
            public BindlessTextureId depthIndex;
            public int padding2;
            public int padding3;
        }

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

        // Bindless slots of the scene opaque-color/depth render targets, registered when the targets
        // are (re)created (see the resize block). Shaders that read the scene (e.g. water) sample these
        // via SAMPLE_BINDLESS instead of a per-material texture binding.
        private int opaqueTextureBindlessIndex;
        private int depthTextureBindlessIndex;
        public int OpaqueTextureBindlessIndex => opaqueTextureBindlessIndex;
        public int DepthTextureBindlessIndex => depthTextureBindlessIndex;

        private RenderLayerData[] layers = Enumerable.Range(0, RenderLayer.MAX_LAYERS)
            .Select(layer => new RenderLayerData(){Layer = new RenderLayer((byte)layer, 0), Name = $"Unused {layer}"})
            .ToArray();
        private List<RenderLayer> freeLayers;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct BlitMaterialData_t
        {
            public int flipY;
            public BindlessTextureId texture1Index;
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
            immediateEngineCommandList = new EngineCommandList(immediateCommandList);
            commandList = immediateCommandList;
            engineCommandList = immediateEngineCommandList;

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
            csm = new CascadedShadowMapManager(engine);
            // keep order: "render once" draws after the ECS objects, before the line overlay
            additionalRenderersStage = new CustomObjectDrawRenderStage(engine, additionalRenderers);
            stages.Add(additionalRenderersStage);
            linesStage = new LinesRenderStage(engine);
            stages.Add(linesStage);

            sceneData = new SceneBuffer();

            planeMesh = engine.MeshManager.CreateMesh(in ScreenPlane.Instance);
            commandList.CheckError("create mesh");

            var blitShader = engine.ShaderManager.LoadShader("internalShaders/blit.json");

            var blitPipeline = this.engine.pipelineManager.CreatePipeline(blitShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = new BlendStateDescription(
                    RgbaFloat.Clear,
                    BlendAttachmentDescription.OverrideBlend),
                DepthStencilState = new DepthStencilStateDescription(false, true, ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false)
            }, false);

            blitMaterial = engine.MaterialManager.CreateMaterial<BlitMaterialData_t>(blitPipeline);
            BlitMaterialData_t data = new() { flipY = flipY ? 1 : 0 };
            blitMaterial.SetMaterialData(ref data);

            var debugShader = engine.ShaderManager.LoadShader("internalShaders/debug_view.json");
            var debugPipeline = engine.pipelineManager.CreatePipeline(debugShader, PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                BlendState = new BlendStateDescription(RgbaFloat.Clear, BlendAttachmentDescription.OverrideBlend),
                DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false)
            }, false);
            debugMaterial = engine.MaterialManager.CreateMaterial<DebugMaterialData_t>(debugPipeline);

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
        }

        public void Dispose()
        {
            csm.Dispose();
            objectDrawStage.Dispose();
            additionalRenderersStage.Dispose();
            linesStage.Dispose();
            engine.meshManager.DisposeMesh(sphereMesh);
            engine.meshManager.DisposeMesh(planeMesh);
            engine.textureManager.DisposeTexture(mainObjectDepthTexture);
            engine.textureManager.DisposeTexture(mainObjectColorTexture);
            engine.textureManager.DisposeTexture(mainObjectColor1Texture);
            engine.textureManager.DisposeTexture(mainObjectBuffer);
            engine.textureManager.DisposeTexture(depthPrepassTexture);
            engine.textureManager.DisposeTexture(guiTexture);
            engine.textureManager.DisposeTexture(sceneViewOpaqueTexture2D);
            engine.textureManager.DisposeTexture(sceneViewDepthTexture2D);
            engine.textureManager.DisposeTexture(sceneViewColor1Texture);
            engine.textureManager.DisposeTexture(sceneViewTexture);
            engine.textureManager.DisposeTexture(sceneViewDepthPrepassTexture);
            foreach (var t in backBuffers)
                engine.textureManager.DisposeTexture(t);

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

        public void BeginFrame()
        {
            commandList.Begin();
            cameraManager.MainCamera.Aspect = engine.gameView.Aspect;
            cameraManager.SceneViewCamera.Aspect = engine.sceneView.Aspect;
        }

        public void ScreenshotCurrentBuffer(string filename, int colorAttachment = 0)
        {
            engine.textureManager.ScreenshotRenderTexture(mainObjectBuffer, filename, colorAttachment);
        }

        private Entity PickObject(Vector2 normalizedScreenPoint, ITexture renderTexture, bool sceneView = false)
        {
            if (renderTexture == null)
                return Entity.Empty;
            var rt = engine.textureManager.GetTextureByHandle(renderTexture.Handle) as VulkanRenderTexture;
            if (rt == null)
            {
                return Entity.Empty;
            }
            Span<uint> buf = stackalloc uint[1];
            int x = (int)(normalizedScreenPoint.X * rt.Width * dynamicScale);
            int y = (int)(normalizedScreenPoint.Y * rt.Height * dynamicScale);
            commandList.ReadPixels(renderTexture, 1, x, y, 1, 1, buf);
            return ResolvePickIndex(buf[0], sceneView);
        }

        // maps a picked R32ui object-buffer value back to the entity that drew it
        private Entity ResolvePickIndex(uint index, bool sceneView = false)
        {
            if (index == 0)
                return Entity.Empty;
            // Scene-view gizmo icons reserve the highest fixed range (GizmoPickIdBase = 1<<25, above
            // the decal range), mapping back to the light/decal entity each icon represents. Only the
            // scene view draws them, so this never collides with the game-view picker.
            if (index >= EngineSceneView.GizmoPickIdBase)
            {
                var iconEntity = engine.sceneView.EntityAtGizmoPickIndex((int)(index - EngineSceneView.GizmoPickIdBase));
                if (engine.entityManager.Exist(iconEntity))
                    return iconEntity;
                return Entity.Empty;
            }
            // Decals reserve a high, fixed index range (see DecalManager.PickIdBase) that never
            // overlaps a real ObjectDrawRenderStage mesh index, so it's checked first.
            if (index >= DecalManager.PickIdBase)
            {
                var decalEntity = engine.decalManager.EntityAtPickIndex((int)(index - DecalManager.PickIdBase));
                if (engine.entityManager.Exist(decalEntity))
                    return decalEntity;
                return Entity.Empty;
            }
            // resolve through whichever draw set actually produced this image (the scene view may have
            // rendered its own independently-culled set, with a different index->entity mapping).
            int total = sceneView ? objectDrawStage.SceneViewTotalToDraw : objectDrawStage.TotalToDraw;
            if (index <= total)
            {
                var entity = sceneView ? objectDrawStage.SceneViewEntityAtIndex(index - 1) : objectDrawStage.EntityAtIndex(index - 1);
                if (engine.entityManager.Exist(entity)) // it could be removed after rendering
                    return entity;
            }
            return Entity.Empty;
        }

        public Entity PickObject(Vector2 normalizedScreenPoint) => PickObject(normalizedScreenPoint, mainObjectBuffer);

        // ReadPixelDeferred channels (independent per-frame readers must not share a slot)
        private const int DeferredChannelObjectPick = 0;
        private const int DeferredChannelDepthPick = 1;

        /// <summary>
        /// Stall-free variant of <see cref="PickObject"/> for continuous (per-frame) hover picking.
        /// The pixel copy is recorded into the frame's command stream and read back frames-in-flight
        /// frames later, so it never syncs the GPU - but the result is the pixel the user SAW a
        /// couple frames ago (perfect for hover, wrong tool for pixel-perfect click checks paired
        /// with this frame's state). Call every frame; returns Empty until the first result lands.
        /// </summary>
        public Entity PickObjectDeferred(Vector2 normalizedScreenPoint)
        {
            if (mainObjectBuffer == null)
                return Entity.Empty;
            var rt = engine.textureManager.GetTextureByHandle(mainObjectBuffer.Handle) as VulkanRenderTexture;
            if (rt == null)
                return Entity.Empty;
            int x = (int)(normalizedScreenPoint.X * rt.Width * dynamicScale);
            int y = (int)(normalizedScreenPoint.Y * rt.Height * dynamicScale);
            var value = commandList.ReadPixelDeferred(mainObjectBuffer, 1, x, y, DeferredChannelObjectPick);
            return value is { } index ? ResolvePickIndex(index) : Entity.Empty;
        }

        // Per-slot unprojection data for PickWorldPositionDeferred: the depth value returned by a
        // slot was rendered by the camera of the frame BEFORE the one that recorded the copy (the
        // copy is recorded during game update, before that frame renders), so each record stores the
        // previous frame's inverse matrices, captured at the end of FinalizeRendering.
        private readonly (Matrix invProj, Matrix invView, Vector2 ndc, bool valid)[] depthPickSlots = new (Matrix, Matrix, Vector2, bool)[8];
        private Matrix lastRenderedInvProj;
        private Matrix lastRenderedInvView;
        private bool lastRenderedMatricesValid;

        /// <summary>
        /// Stall-free world-position picking straight from the depth buffer - the "what world point
        /// is under the cursor" query without a physics raycast. Pixel-perfect against everything
        /// RENDERED (no collider needed), O(1), same deferred mechanism as
        /// <see cref="PickObjectDeferred"/>: call every frame, the result is a couple frames stale
        /// (= the image the user sees) and null until the first result lands or when the pixel hit
        /// the sky (cleared depth). Note it hits creatures/doodads too - it is NOT a drop-in for
        /// masked raycasts (COLLISION_MASK_STATIC etc.).
        /// </summary>
        public Vector3? PickWorldPositionDeferred(Vector2 normalizedScreenPoint)
        {
            if (depthTexture2D == null)
                return null;

            int x = (int)(normalizedScreenPoint.X * depthTexture2D.Width * dynamicScale);
            int y = (int)(normalizedScreenPoint.Y * depthTexture2D.Height * dynamicScale);

            int slot = commandList.DeferredReadSlot;
            var stored = depthPickSlots[slot % depthPickSlots.Length];
            var raw = commandList.ReadPixelDeferred(depthTexture2D, 0, x, y, DeferredChannelDepthPick);

            // ndc convention mirrors NormalizedScreenPointToRay (the proven picking math)
            var ndc = new Vector2(2 * normalizedScreenPoint.X - 1f, 2 * (1 - normalizedScreenPoint.Y) - 1f);
            if (lastRenderedMatricesValid)
                depthPickSlots[slot % depthPickSlots.Length] = (lastRenderedInvProj, lastRenderedInvView, ndc, true);

            if (raw is not { } bits || !stored.valid)
                return null;

            float depth = BitConverter.UInt32BitsToSingle(bits);
            if (depth >= 0.99999f)
                return null; // cleared depth - the cursor is on the sky

            // unproject clip (ndc, depth) -> eye -> world with the matrices that rendered that depth
            var eye = Vector4.Transform(new Vector4(stored.ndc.X, stored.ndc.Y, depth, 1f), stored.invProj);
            if (MathF.Abs(eye.W) < 1e-12f)
                return null;
            eye /= eye.W;
            var world = Vector4.Transform(new Vector4(eye.X, eye.Y, eye.Z, 1f), stored.invView);
            return new Vector3(world.X, world.Y, world.Z);
        }

        public Entity PickSceneViewObject()
        {
            var screenPoint = engine.inputManager.mouse.RawScreenPoint;
            if (!engine.sceneView.ViewRect.Contains(screenPoint))
            {
                return Entity.Empty;
            }
            var normalized = new Vector2((screenPoint.X - engine.sceneView.ViewRect.X) / engine.sceneView.ViewRect.Width,
                (screenPoint.Y - engine.sceneView.ViewRect.Y) / engine.sceneView.ViewRect.Height);
            return PickObject(normalized, sceneViewTexture, sceneView: true);
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

            // apply a pending resolution-scale change before ActivateScene(null) below computes
            // the Forward+ TilesX/TilesY from DynamicWidth/DynamicHeight - applying it later
            // would leave the tile grid sized with the old scale for one frame
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

            // resolve before the stages collect casters (they read ShadowDistance) and fit cascades.
            activeShadowSettings = csm.TryResolveSettings(out var shadowSettings) ? shadowSettings : null;
            if (activeShadowSettings is { } s)
                csm.EnsureResources(s.Resolution);

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
                engine.textureManager.DisposeTexture(depthPrepassTexture);
                engine.textureManager.DisposeTexture(opaqueRenderTexture);
                engine.textureManager.DisposeTexture(opaqueTexture2D);
                engine.textureManager.DisposeTexture(depthTexture2D);

                mainObjectDepthTexture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.DepthComponent);
                mainObjectColorTexture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.R8G8B8A8);
                mainObjectColor1Texture = engine.textureManager.CreateTexture(null, currentBackBufferWidth, currentBackBufferHeight, TextureFormat.R32ui);
                mainObjectBuffer = engine.textureManager.CreateRenderTexture(mainObjectColorTexture, mainObjectDepthTexture, mainObjectColor1Texture);
                depthPrepassTexture = engine.textureManager.CreateDepthOnlyRenderTexture(mainObjectDepthTexture);
                opaqueRenderTexture = engine.textureManager.CreateRenderTextureWithColorAndDepth(currentBackBufferWidth, currentBackBufferHeight, out opaqueTexture2D, out depthTexture2D);
                // register the scene RTs bindless once at creation; the slots stay valid until the next
                // resize recreates them (these textures are owned by RenderManager, so no GC hazard).
                opaqueTextureBindlessIndex = engine.textureManager.GetBindlessIndex(opaqueTexture2D);
                depthTextureBindlessIndex = engine.textureManager.GetBindlessIndex(depthTexture2D);
                for (var index = 0; index < backBuffers.Length; index++)
                {
                    engine.textureManager.DisposeTexture(backBuffers[index]);
                    backBuffers[index] = engine.textureManager.CreateRenderTexture(currentBackBufferWidth, currentBackBufferHeight);
                }
                engine.textureManager.DisposeTexture(gameDebugTexture);
                gameDebugTexture = engine.textureManager.CreateRenderTexture(currentBackBufferWidth, currentBackBufferHeight);
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
                engine.textureManager.DisposeTexture(sceneViewDepthPrepassTexture);

                sceneViewDepthTexture2D = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.DepthComponent);
                sceneViewOpaqueTexture2D = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.R8G8B8A8);
                sceneViewColor1Texture = engine.textureManager.CreateTexture(null, currentSceneViewWidth, currentSceneViewHeight, TextureFormat.R32ui);
                sceneViewTexture = engine.textureManager.CreateRenderTexture(sceneViewOpaqueTexture2D, sceneViewDepthTexture2D, sceneViewColor1Texture);
                sceneViewDepthPrepassTexture = engine.textureManager.CreateDepthOnlyRenderTexture(sceneViewDepthTexture2D);
                engine.textureManager.DisposeTexture(sceneDebugTexture);
                sceneDebugTexture = engine.textureManager.CreateRenderTexture(currentSceneViewWidth, currentSceneViewHeight);
            }

            if (currentGuiWidth != (int)engine.WindowHost.WindowWidth ||
                currentGuiHeight != (int)engine.WindowHost.WindowHeight)
            {
                currentGuiWidth = (int)engine.WindowHost.WindowWidth;
                currentGuiHeight = (int)engine.WindowHost.WindowHeight;
                engine.textureManager.DisposeTexture(guiTexture);
                guiTexture = engine.textureManager.CreateRenderTexture(currentGuiWidth, currentGuiHeight);
            }

            inCoreRenderingLoop = true;
            currentBackBufferIndex = -1;

            commandList.CheckError("Before set CurrentBackBuffer");

            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);

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

        public void ActivateRenderTexture(ITexture rt, Color4? color = null, LoadOp? depthLoadOp = null)
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
                    DepthLoadOp = depthLoadOp,
                    // the depth prepass aliases mainObjectBuffer's depth image, so it must rasterize
                    // at the same scaled viewport - otherwise the opaque pass' Equal test (and the
                    // Forward+ tile cull, which reads the DynamicWidth×DynamicHeight region) breaks
                    ViewportScale = inCoreRenderingLoop && (rt == mainObjectBuffer || rt == depthPrepassTexture) && useDynamicScale ? dynamicScale : 1,
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
            // both ends finish samplable: the caller (highlight postprocess) samples the source
            // later in the frame; rendering to it again re-transitions at pass begin anyway
            commandList.Barrier(source, ResourceUsage.TransferSource, ResourceUsage.ShaderRead);
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
            // blit/ssao/grid_plane never read `model` (their vert never calls
            // VERTEX_SETUP_INSTANCING) - no instancing buffers to prepare.
            EnableMaterial(material, material.GetShaderPass(ShaderPassType.Forward)!);
            engineCommandList.DrawIndexed(planeMesh, 0);
        }

        /// <summary>When a debug visualization is selected, renders it into each visible view's debug
        /// texture (sampling that view's depth/shadow/Forward+ grids) so the view can display it instead
        /// of the final image. Called after the opaque+transparent passes, when those buffers are ready.</summary>
        public void RenderDebugViews()
        {
            gameDebugRendered = false;
            sceneDebugRendered = false;
            if (DebugView == DebugView.FinalImage)
                return;

            if (engine.gameView.IsVisible && gameDebugTexture != null)
            {
                ActivateScene(null); // bind the main view's scene buffer (gridSet 0, cascade indices)
                BindCascadesForDebugView();
                RenderDebugView(gameDebugTexture, mainObjectDepthTexture);
                gameDebugRendered = true;
            }
            if (engine.sceneView.IsVisible && sceneDebugTexture != null)
            {
                ActivateScene(new SceneData(cameraManager.SceneViewCamera, engine.lightManager.MainDirectional, engine.lightManager.SecondaryDirectional));
                BindCascadesForDebugView();
                RenderDebugView(sceneDebugTexture, sceneViewDepthTexture2D);
                sceneDebugRendered = true;
            }

            // RenderDebugView leaves no active pass; reopen one on the back buffer so the rest of the
            // frame (RenderPostProcess) sees the same state RenderTransparent normally leaves.
            ActivateScene(null);
            ActivateDefaultRenderTexture();
        }

        /// <summary>ActivateScene resets CascadeCount to 0, but the "Shadow map" view needs the cascade
        /// indices, so re-apply and rebind them (still valid - the depth textures persist) before drawing.</summary>
        private void BindCascadesForDebugView()
        {
            if (DebugView != DebugView.ShadowCascade || activeShadowSettings is not { } settings)
                return;
            if (!engine.lightManager.MainDirectional.Exists)
                return;
            ApplyCascadesToSceneBuffer(settings);
            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
        }

        private void RenderDebugView(ITexture debugTarget, ITexture depthTexture)
        {
            // the depth is a depth attachment after the opaque/transparent passes; sample it as a texture
            if (commandList.InRenderingPass)
                commandList.EndRenderingPass();
            commandList.Barrier(depthTexture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);

            DebugMaterialData_t data = new() { mode = (int)DebugView, depthIndex = engine.textureManager.GetBindlessIndex(depthTexture) };
            debugMaterial.SetMaterialData(ref data);

            ActivateRenderTexture(debugTarget, Color4.Black);
            RenderFullscreenPlane(debugMaterial);

            // ready the debug texture for the GUI pass to sample (ImGui.Image)
            commandList.EndRenderingPass();
            commandList.Barrier(debugTarget, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
        }
        
        public void FinalizeRendering(int dstFrameBuffer)
        {
            foreach (var stage in stages)
                stage.EndFrame();

            // the "render once" requests were scheduled in this frame's Update and drained by the
            // stage above; clear them so next frame starts empty
            additionalRenderers.Clear();

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
            BlitMaterialData_t blitData = new() { flipY = flipY ? 1 : 0, texture1Index = engine.TextureManager.GetBindlessIndex(guiTexture) };
            blitMaterial.SetMaterialData(ref blitData);
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
            stats.Set1CacheHits += commandList.Set1CacheHits;
            stats.Set1CacheMisses += commandList.Set1CacheMisses;
            engine.statsManager.RenderStats = stats;

            // for PickWorldPositionDeferred: a copy recorded during the NEXT frame's update reads the
            // depth THIS frame just rendered - remember this camera's inverse matrices for it
            Matrix.Invert(cameraManager.MainCamera.ProjectionMatrix, out lastRenderedInvProj);
            lastRenderedInvView = cameraManager.MainCamera.InverseViewMatrix;
            lastRenderedMatricesValid = true;

            // snapshot this frame's GPU memory churn from our allocator, then reset for the next frame
            engine.statsManager.GpuAllocationsPerFrame = VMASharp.Vma.Allocations;
            engine.statsManager.GpuFreesPerFrame = VMASharp.Vma.Frees;
            engine.statsManager.GpuDeviceAllocationsPerFrame = VMASharp.Vma.DeviceAllocations;
            engine.statsManager.GpuDeviceFreesPerFrame = VMASharp.Vma.DeviceFrees;
            VMASharp.Vma.ResetFrameStats();
        }

        internal void RenderOpaque(int dstFrameBuffer)
        {
            foreach (var stage in stages)
                stage.PrepareFrame(cameraManager.MainCamera);

            // When the scene view culls with its own camera, build a second draw set for it. Must run
            // after the main PrepareFrame (it reuses the WorldMeshBounds refreshed there) and after the
            // shadow casters were collected, since it overwrites the shared per-entity cull bit.
            if (engine.sceneView.IsVisible && engine.sceneView.OwnCulling)
                objectDrawStage.PrepareSceneFrame(cameraManager.SceneViewCamera);

            if (engine.gameView.IsVisible)
            {
                ActivateRenderTexture(depthPrepassTexture, depthLoadOp: LoadOp.Clear);
                objectDrawStage.Render(RenderPoint.DepthPrepass, engineCommandList, cameraManager.MainCamera);

                // Forward+ tiled light/decal culling: cull point lights and decals against the
                // depth prepass result, between the depth pass and opaque shading - the only
                // point where both the depth texture (as a sampled image) and the light/decal
                // grid/index SSBOs are accessible outside a rendering pass.
                commandList.EndRenderingPass();
                commandList.Barrier(mainObjectDepthTexture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
                int depthBindlessIndex = engine.textureManager.GetBindlessIndex(mainObjectDepthTexture);
                commandList.DispatchTileCullCompute(depthBindlessIndex, DynamicWidth, DynamicHeight, sceneData.TilesX, sceneData.TilesY);

                // render the main camera's shadow cascades and rebind the main scene buffer (now with
                // cascades) for the opaque pass below. Reuses the opaque set collected in PrepareFrame.
                var mainLight = engine.lightManager.MainDirectional;
                if (activeShadowSettings is { } shadowSettings && mainLight.Exists)
                    RenderShadowCascades(cameraManager.MainCamera, mainLight, shadowSettings,
                        new SceneData(cameraManager.MainCamera, mainLight, engine.lightManager.SecondaryDirectional));

                // custom off-screen passes (e.g. the waypoint path projector) - run before opaque so
                // their results can be sampled by the opaque pass / decals this frame. The scene buffer
                // (main camera + cascades) is snapshotted/restored around each stage inside, so a stage
                // that rebinds it for its own off-screen camera can't affect the others or the opaque pass.
                RenderBeforeOpaque();
            }

            ActivateRenderTexture(CurrentBackBuffer, cameraManager.MainCamera.BackgroundColor, depthLoadOp: LoadOp.Load);

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
                ActivateScene(new SceneData(cameraManager.SceneViewCamera, engine.lightManager.MainDirectional, engine.lightManager.SecondaryDirectional));

                if (point == RenderPoint.Opaque)
                {
                    // Forward+ tiled light/decal culling for the scene view's own camera - same
                    // depth-prepass-then-dispatch shape as the main view's in RenderOpaque(), just
                    // targeting the scene view's own depth alias and writing into the SCENE_*
                    // grid/index buffers (isSceneView: true) instead of the main view's. Done once
                    // per frame (gated on Opaque); the resulting grids stay valid for this view's
                    // Transparent pass too, since nothing else writes into them in between.
                    int sceneViewWidth = Math.Max(1, currentSceneViewWidth);
                    int sceneViewHeight = Math.Max(1, currentSceneViewHeight);
                    ActivateRenderTexture(sceneViewDepthPrepassTexture, depthLoadOp: LoadOp.Clear);
                    objectDrawStage.Render(RenderPoint.DepthPrepass, engineCommandList, cameraManager.SceneViewCamera);

                    commandList.EndRenderingPass();
                    commandList.Barrier(sceneViewDepthTexture2D, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
                    int sceneDepthBindlessIndex = engine.textureManager.GetBindlessIndex(sceneViewDepthTexture2D);
                    commandList.DispatchTileCullCompute(sceneDepthBindlessIndex, sceneViewWidth, sceneViewHeight, sceneData.TilesX, sceneData.TilesY, isSceneView: true);

                    // render the scene-view camera's shadow cascades (re-using the shared cascade
                    // textures) and rebind the scene-view scene buffer with cascades for its opaque pass.
                    var sceneMainLight = engine.lightManager.MainDirectional;
                    if (activeShadowSettings is { } sceneShadowSettings && sceneMainLight.Exists)
                        RenderShadowCascades(cameraManager.SceneViewCamera, sceneMainLight, sceneShadowSettings,
                            new SceneData(cameraManager.SceneViewCamera, sceneMainLight, engine.lightManager.SecondaryDirectional));
                }

                // depthLoadOp: Load - the depth prepass above already populated sceneViewDepthTexture2D
                // for this frame's Opaque pass, and Transparent must preserve Opaque's depth test results.
                ActivateRenderTexture(sceneViewTexture, point == RenderPoint.Opaque ? (Color4?)cameraManager.SceneViewCamera.BackgroundColor : null, depthLoadOp: LoadOp.Load);

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

        private partial struct UpdateChildTransformsJob : IParallelJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<CopyParentTransform> parents;
            private ComponentDataAccess<LocalToWorld> localToWorld;
            private ComponentDataAccess<DirtyPosition> dirtyPosition;

            public IEntityManager entityManager;

            public void Execute(int thread, int start, int end)
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
            }
        }

        public void UpdateTransforms()
        {
            var entityManager = engine.entityManager;
            new UpdateChildTransformsJob { entityManager = entityManager }.Run(dynamicParentedEntitiesArchetype);
        }
        
        private void ClearDirtyEntityBit()
        {
            dirtEntities.ParallelForEach<DirtyPosition>((itr, thread, start, end, dirty) =>
            {
                for (int i = start; i < end; ++i)
                    dirty[i].Disable();
            });
        }

        private void EnableMaterial(Material material, IShaderPass shaderPass)
        {
            engineCommandList.SetMaterial(material, shaderPass);
        }

        public static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld)
        {
            return WorldMeshBounds.FromLocal(in local, in localToWorld);
        }

        internal static WorldMeshBounds LocalToWorld(in MeshBounds local, in LocalToWorld localToWorld, ref Span<Vector3> corners)
        {
            return WorldMeshBounds.FromLocal(in local, in localToWorld, ref corners);
        }
        
        // for engine-internal renderers (UIManager, ImGuiController) that record their own commands
        internal ICommandList CommandList => commandList;

        public void Render(MeshHandle meshHandle, MaterialHandle materialHandle, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, Int4? instanceInt = null)
        {
            var mesh = engine.meshManager.GetMeshByHandle(meshHandle);
            var material = engine.materialManager.GetMaterialByHandle(materialHandle);
            Render(mesh, material, shaderPassType, submesh, localToWorld, worldToLocal, instanceInt);
        }

        public void Render(IMesh mesh, Material material, ShaderPassType shaderPass, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, Int4? instanceInt = null)
        {
            if (worldToLocal == null)
            {
                Matrix.Invert(localToWorld, out var  worldToLocal_);
                worldToLocal = worldToLocal_;
            }

            Debug.Assert(inRenderingLoop);
            engineCommandList.PrepareInstancingData(material, localToWorld, worldToLocal.Value, 0, instanceInt, 1);
            EnableMaterial(material, material.GetShaderPass(shaderPass)!);
            engineCommandList.DrawIndexed(mesh, submesh);
        }

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color)
        {
            linesStage.Add(start, end, color);
        }

        public void RenderOnce(LocalToWorld localToWorld, MeshRenderer renderer)
        {
            if (inRenderingLoop)
                throw new Exception("Don't call RenderOnce in Render(), this is a schedule method, you are expected to call it in Update and the Engine will take care of everything.");

            additionalRenderers.Add((localToWorld, renderer));
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

        /// <summary>Batched draw where each instance has its own transform and draw-data int4 (unlike
        /// <see cref="RenderInstancedIndirect(IMesh, Material, ShaderPassType, int, int, Matrix, Matrix?)"/>,
        /// which broadcasts one). For many distinct objects sharing a mesh - e.g. gizmo icons.</summary>
        public void RenderInstanced(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh,
            ReadOnlySpan<Matrix> models, ReadOnlySpan<Int4> drawData)
        {
            if (models.Length == 0)
                return;

            Debug.Assert(inRenderingLoop);
            engineCommandList.PrepareInstancingData(material, models, drawData);
            EnableMaterial(material, material.GetShaderPass(shaderPassType)!);
            engineCommandList.DrawIndexedInstanced(mesh, submesh, models.Length);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount, Matrix localToWorld, Matrix? worldToLocal = null)
        {
            if (!worldToLocal.HasValue)
            {
                Matrix.Invert(localToWorld, out var worldToLocal_);
                worldToLocal = worldToLocal_;
            }
            // every instance in this draw shares one world transform (e.g. UIManager's
            // world-space glyph batches) - broadcast it into instancesCount identical SSBO slots.
            engineCommandList.PrepareInstancingData(material, localToWorld, worldToLocal.Value, 0, null, instancesCount);
            EnableMaterial(material, material.GetShaderPass(shaderPassType)!);
            engineCommandList.DrawIndexedInstanced(mesh, submesh, instancesCount);
        }

        public void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int instancesCount)
        {
            // screen-space glyph batches never read `model` - no instancing buffers to prepare.
            EnableMaterial(material, material.GetShaderPass(shaderPassType)!);
            engineCommandList.DrawIndexedInstanced(mesh, submesh, instancesCount);
        }


        public void ActivateScene(in SceneData? scene)
        {
            // Forward+ tiled light/decal culling: both the main game view and the editor's scene
            // view get their own tile grid, tile-culled against their own camera/depth (see
            // RenderStages, which dispatches the scene view's cull pass separately into the
            // SCENE_*_BINDING buffers selected by GridSet). The raw Light/Decal arrays
            // themselves are shared - only the per-tile grid/index results differ per view.
            if (scene == null)
            {
                sceneData.GridSet = 0;
                sceneData.TilesX = Math.Min((DynamicWidth + Constants.FORWARD_PLUS_TILE_SIZE - 1) / Constants.FORWARD_PLUS_TILE_SIZE, Constants.FORWARD_PLUS_MAX_TILES_X);
                sceneData.TilesY = Math.Min((DynamicHeight + Constants.FORWARD_PLUS_TILE_SIZE - 1) / Constants.FORWARD_PLUS_TILE_SIZE, Constants.FORWARD_PLUS_MAX_TILES_Y);
            }
            else
            {
                sceneData.GridSet = 1;
                int sceneViewWidth = Math.Max(1, currentSceneViewWidth);
                int sceneViewHeight = Math.Max(1, currentSceneViewHeight);
                sceneData.TilesX = Math.Min((sceneViewWidth + Constants.FORWARD_PLUS_TILE_SIZE - 1) / Constants.FORWARD_PLUS_TILE_SIZE, Constants.FORWARD_PLUS_MAX_TILES_X);
                sceneData.TilesY = Math.Min((sceneViewHeight + Constants.FORWARD_PLUS_TILE_SIZE - 1) / Constants.FORWARD_PLUS_TILE_SIZE, Constants.FORWARD_PLUS_MAX_TILES_Y);
            }

            // GatherLights also resolves this frame's directional (sun) lights, so it must run
            // before the scene buffer below is built from MainDirectional/SecondaryDirectional.
            sceneData.LightCount = engine.lightManager.GatherLights(out var lights);
            commandList.UploadLightData(lights.AsSpan(0, sceneData.LightCount));
            sceneData.DecalCount = engine.decalManager.GatherDecals(out var decalsArr);
            commandList.UploadDecalData(decalsArr.AsSpan(0, sceneData.DecalCount));

            var data = scene ?? new SceneData(engine.cameraManger.MainCamera,
                engine.lightManager.MainDirectional,
                engine.lightManager.SecondaryDirectional);
            UpdateSceneBuffer(in data);

            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
        }

        public void SetSceneCameraOverride(in Matrix view, in Matrix projection, Vector3 cameraPosition)
        {
            sceneData.ViewMatrix = view;
            sceneData.ProjectionMatrix = projection;
            Matrix.Invert(view, out var vmInv);
            Matrix.Invert(projection, out var projInv);
            sceneData.ViewMatrixInverse = vmInv;
            sceneData.ProjectionMatrixInverse = projInv;
            sceneData.CameraPosition = new Vector4(cameraPosition, 1);
            sceneData.CascadeCount = 0; // no shadow sampling for the off-screen preview
            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
        }

        public void DrawRenderers(EngineCommandList cl, ReadOnlySpan<(LocalToWorld, MeshRenderer)> renderers)
        {
            // one instance per draw; each buffer holds a single element addressed at firstInstance 0
            Span<Matrix> models = stackalloc Matrix[1];
            Span<Matrix> invModels = stackalloc Matrix[1];
            Span<uint> objIdx = stackalloc uint[1];
            Span<Int4> drawData = stackalloc Int4[1];
            Span<int> matIdx = stackalloc int[1];
            for (int i = 0; i < renderers.Length; i++)
            {
                var l2w = renderers[i].Item1;
                var mr = renderers[i].Item2;
                var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
                var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);
                var pass = material.GetShaderPass(ShaderPassType.Forward);
                if (pass == null)
                    continue;

                models[0] = l2w.Matrix;
                invModels[0] = l2w.Inverse;
                objIdx[0] = 0u;
                drawData[0] = mr.InstanceData ?? new Int4(-1, -1, -1, -1);
                matIdx[0] = material.MaterialArrayIndex;

                cl.SetBuffer(ShaderUniforms.InstancingModels, cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)models));
                cl.SetBuffer(ShaderUniforms.InstancingInverseModels, cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)invModels));
                cl.SetBuffer(ShaderUniforms.InstancingObjectIndices, cl.UploadTransientBuffer((ReadOnlySpan<uint>)objIdx));
                cl.SetBuffer(ShaderUniforms.InstancingDrawData, cl.UploadTransientBuffer((ReadOnlySpan<Int4>)drawData));
                cl.SetBuffer(ShaderUniforms.InstancingMaterialIndex, cl.UploadTransientBuffer((ReadOnlySpan<int>)matIdx));
                cl.SetMaterial(material, pass);
                cl.DrawIndexedInstanced(mesh, mr.SubMeshId, 1, 0);
            }
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
            sceneData.LightPosition = Vector3.Zero;
            sceneData.CameraPosition = new Vector4(camera.Transform.Position, 1);
            // resolved directional lights (the first two directional Light entities); when absent,
            // intensity 0 leaves the directional term dark. Ambient comes from the primary light.
            sceneData.LightDirection = new Vector4(data.MainLight.Exists ? data.MainLight.Direction : Vectors.Down, 0);
            sceneData.LightColor = data.MainLight.Color;
            sceneData.LightIntensity = data.MainLight.Exists ? data.MainLight.Intensity : 0f;
            sceneData.SecondaryLightDirection = new Vector4(data.SecondaryLight.Exists ? data.SecondaryLight.Direction : Vectors.Down, 0);
            sceneData.SecondaryLightColor = data.SecondaryLight.Color;
            sceneData.SecondaryLightIntensity = data.SecondaryLight.Exists ? data.SecondaryLight.Intensity : 0f;
            sceneData.AmbientColor = data.MainLight.AmbientColor;
            var fog = camera.Fog;
            sceneData.fogStart = fog.Start;
            sceneData.fogEnd = fog.End;
            sceneData.fogColor = fog.Color;
            sceneData.fogEnabled = fog.Enabled ? 1 : 0;
            sceneData.Time = (float)engine.TotalTime;
            sceneData.ZNear = camera.NearClip;
            sceneData.ZFar = camera.FarClip;
            sceneData.ScreenWidth = engine.gameView.ViewRect.Width;
            sceneData.ScreenHeight = engine.gameView.ViewRect.Height;
            // Shadows are disabled by default; only the per-view bind issued right after the cascades
            // are rendered (RenderShadowCascades -> ApplyCascadesToSceneBuffer) turns them on. This
            // keeps the depth prepass (and the shadow depth pass itself) from sampling the cascade
            // textures while they're being written, and is safe before the first cascade render.
            sceneData.CascadeCount = 0;
        }

        /// <summary>Binds the scene UBO with the light's view/ortho matrices in place of the camera's,
        /// so the reused Depth pass renders geometry into a cascade depth map. Shadow sampling is
        /// disabled (CascadeCount = 0) so the depth pass never reads the maps it is writing.</summary>
        private void BindShadowSceneBuffer(in Matrix lightView, in Matrix lightProj)
        {
            sceneData.ViewMatrix = lightView;
            sceneData.ProjectionMatrix = lightProj;
            Matrix.Invert(lightView, out var vmInv);
            Matrix.Invert(lightProj, out var projInv);
            sceneData.ViewMatrixInverse = vmInv;
            sceneData.ProjectionMatrixInverse = projInv;
            sceneData.CascadeCount = 0;
            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
        }

        /// <summary>Copies the most recently fit cascades (matrices, view-space splits, bindless
        /// texture indices) plus the configured sampling params into the scene buffer and enables
        /// shadow sampling.</summary>
        private void ApplyCascadesToSceneBuffer(in CascadeShadowMap settings)
        {
            sceneData.CascadeViewProj0 = csm.LightViewProj[0];
            sceneData.CascadeViewProj1 = csm.LightViewProj[1];
            sceneData.CascadeViewProj2 = csm.LightViewProj[2];
            sceneData.CascadeViewProj3 = csm.LightViewProj[3];
            sceneData.CascadeSplits = new Vector4(csm.SplitDistances[0], csm.SplitDistances[1], csm.SplitDistances[2], csm.SplitDistances[3]);
            sceneData.CascadeTexture0 = csm.BindlessIndex(0);
            sceneData.CascadeTexture1 = csm.BindlessIndex(1);
            sceneData.CascadeTexture2 = csm.BindlessIndex(2);
            sceneData.CascadeTexture3 = csm.BindlessIndex(3);
            sceneData.CascadeCount = CascadedShadowMapManager.CascadeCount;
            sceneData.ShadowMapResolution = csm.Resolution;
            sceneData.ShadowNormalBias = settings.NormalBias;
            sceneData.ShadowConstantBias = settings.ConstantBias;
            sceneData.ShadowPcfRadius = settings.PcfRadius;
            sceneData.ShadowBlur = settings.Blur;
            sceneData.ShadowCascadeBlend = settings.CascadeBlend;
        }

        /// <summary>Fits and renders the directional-light shadow cascades for one camera, then
        /// restores that camera's scene buffer (now carrying the cascades) ready for its opaque pass.
        /// Reuses the opaque geometry collected by <see cref="ObjectDrawRenderStage.PrepareFrame"/>
        /// as the shadow casters. Must run inside the rendering loop, after the camera's depth prepass.
        /// </summary>
        // Invokes every RenderPoint.BeforeOpaque stage just before the opaque pass, with no target
        // active. Each stage owns its own off-screen render texture, pass and barriers (see
        // EngineCommandList.BeginRenderTexture / BarrierToShaderRead) - the engine is target-agnostic
        // here; it only guarantees this runs before opaque so the results can be sampled this frame.
        private void RenderBeforeOpaque()
        {
            foreach (var stage in stages)
                if ((stage.RenderPoints & RenderPoint.BeforeOpaque) != 0)
                {
                    // isolate each stage: it may rebind the scene buffer for its own off-screen camera
                    // (SetSceneCameraOverride), so snapshot the shared scene (main camera + cascades) and
                    // restore it after, keeping the other stages and the opaque pass on the main scene.
                    var scene = sceneData;
                    stage.Render(RenderPoint.BeforeOpaque, engineCommandList, cameraManager.MainCamera);
                    sceneData = scene;
                    commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
                }
        }

        private void RenderShadowCascades(ICamera camera, in DirectionalLightData light, in CascadeShadowMap settings, in SceneData restoreData)
        {
            csm.ComputeCascades(camera, light.Direction, settings, objectDrawStage.ShadowCasterReach);

            for (int i = 0; i < CascadedShadowMapManager.CascadeCount; i++)
            {
                // depth-only target, cleared to far; BeginRenderingPass resets depth bias to 0
                ActivateRenderTexture(csm.RenderTarget(i), depthLoadOp: LoadOp.Clear);
                BindShadowSceneBuffer(csm.LightView[i], csm.LightProj[i]);
                // configured slope-scaled depth bias pushes the stored depth away from the light to
                // suppress self-shadowing acne (combined with the shader's normal-offset bias).
                commandList.SetDepthBias(settings.DepthBiasConstant, settings.DepthBiasSlope);
                objectDrawStage.Render(RenderPoint.Shadow, engineCommandList, camera);
            }

            commandList.EndRenderingPass();
            for (int i = 0; i < CascadedShadowMapManager.CascadeCount; i++)
                commandList.Barrier(csm.DepthTexture(i), ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);

            // restore the camera's scene buffer (the shadow loop left the light's matrices bound),
            // now carrying the fresh cascades for the opaque pass that follows.
            UpdateSceneBuffer(restoreData);
            ApplyCascadesToSceneBuffer(settings);
            commandList.BindTransientUniformBuffer(Constants.SCENE_BUFFER_INDEX, ref sceneData);
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
