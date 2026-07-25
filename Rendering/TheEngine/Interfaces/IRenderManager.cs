using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Rendering;
using TheEngine.Structures;
using TheMaths;
using Veldrid;

namespace TheEngine.Interfaces
{
    public interface IRenderManager
    {
        void SetupRendererEntity(Entity entity, MeshHandle mesh, Material material, int subMesh, Matrix localToWorld, Int4? instanceData = null);
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterStaticRenderer(StaticRenderHandle staticRenderHandle);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterDynamicRenderer(DynamicRenderHandle staticRenderHandle);
        void DrawLine(Vector3 start, Vector3 end, Vector4 color);
        /// <summary>
        /// Schedules a one-frame draw of a mesh, batched alongside the ECS object renderers
        /// (one shared instancing buffer, set=1 bound once per material run). Must be called
        /// from Update, never from a Render callback - the engine collects these before the
        /// frame and draws them at the opaque/transparent points. These draws are not pickable.
        /// </summary>
        void RenderOnce(LocalToWorld localToWorld, MeshRenderer renderer);
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Transform transform);
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, Int4? instanceInt = null);
        void Render(MeshHandle mesh, MaterialHandle material, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, Int4? instanceInt = null);
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Vector3 position);
        void RenderFullscreenPlane(Material material);
        void ActivateDefaultRenderTexture();
        void ActivateRenderTexture(ITexture rt, Color4? color = null, LoadOp? depthLoadOp = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int count, Matrix localToWorld, Matrix? worldToLocal = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int count);
        /// <summary>Batched draw where each instance has its own transform and draw-data int4 - for
        /// many distinct objects sharing a mesh (gizmo icons, NPC status icons).</summary>
        void RenderInstanced(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, System.ReadOnlySpan<Matrix> models, System.ReadOnlySpan<Int4> drawData);
        void ActivateScene(in SceneData? scene);
        /// <summary>Overrides the bound scene camera (view/projection + eye position) for subsequent
        /// draws in the current pass — e.g. to render a model into an off-screen render texture from a
        /// dedicated preview camera. Directional/ambient lighting from the last <see cref="ActivateScene"/>
        /// is kept. Intended for BeforeOpaque off-screen passes; the orchestrator re-activates the main
        /// scene before the opaque pass, so no manual restore is needed.</summary>
        void SetSceneCameraOverride(in Matrix view, in Matrix projection, Vector3 cameraPosition);
        /// <summary>Records draws for a set of mesh renderers into the currently-active pass, using the
        /// same per-object instancing (models/drawData/materialIndex, so M2 bones + materials resolve
        /// correctly) as the main object pass. The caller owns the target/pass and camera (begin the render
        /// texture and call <see cref="SetSceneCameraOverride"/> first).</summary>
        void DrawRenderers(EngineCommandList commandList, System.ReadOnlySpan<(Components.LocalToWorld, Components.MeshRenderer)> renderers);
        /// <summary>What the views display: final image or a debug visualization (depth/shadow/grids).</summary>
        Rendering.DebugView DebugView { get; set; }
        void AddPostprocess(IPostProcess postProcess);
        void RemovePostprocess(IPostProcess postProcess);
        void SetDynamicResolutionScale(float scale);
        void DrawSphere(Vector3 center, float radius, Vector4 color);
        /// <summary>
        /// Synchronously reads the object id under the given point from the last fully
        /// rendered frame. This is a GPU readback (on Vulkan it will wait for the previous
        /// frame's fence) - call it at event frequency (clicks), never every frame.
        /// </summary>
        Entity PickObject(Vector2 normalizedScreenPosition);

        /// <summary>Stall-free pick for continuous hover: the readback is recorded into the frame
        /// and resolved frames-in-flight frames later, so it never syncs the GPU. The result is a
        /// couple frames stale (= what the user sees). Call every frame; Empty until the first
        /// result lands.</summary>
        Entity PickObjectDeferred(Vector2 normalizedScreenPosition);

        /// <summary>Stall-free world position under the cursor, reconstructed from the depth buffer
        /// (no physics raycast; hits everything rendered, colliders not needed). Same deferred
        /// mechanism as <see cref="PickObjectDeferred"/>: call every frame, result is a couple
        /// frames stale, null until the first result lands or when pointing at the sky.</summary>
        Vector3? PickWorldPositionDeferred(Vector2 normalizedScreenPosition);
        ITexture DepthTexture { get; }
        ITexture OpaqueTexture { get; }
        int OpaqueTextureBindlessIndex { get; }
        int DepthTextureBindlessIndex { get; }
        /// <summary>
        /// Adds a custom render stage; stages render in registration order, after the built-in
        /// object stage. Register and unregister outside the render loop. The caller keeps
        /// ownership of the stage and disposes it after unregistering.
        /// </summary>
        void RegisterRenderStage(IRenderStage stage);
        void UnregisterRenderStage(IRenderStage stage);
        /// <summary>
        /// Copies (with scaling) one render texture into another as part of the frame, color
        /// (linear) and depth (nearest). Use this instead of ITextureManager.BlitRenderTextures
        /// inside the render loop - this one is recorded in order with the draws. It ends the
        /// current rendering pass (blits are only legal outside one), so activate a render
        /// texture afterwards before issuing more draws.
        /// </summary>
        void BlitRenderTextures(ITexture source, ITexture destination);
        RenderLayer RegisterRenderLayer(string layerName);
        void UnregisterRenderLayer(RenderLayer layer);
        void ToggleRenderLayer(RenderLayer layer, bool enable);
        bool IsRenderLayerEnabled(byte layer);
        IReadOnlyList<RenderLayerData> RenderLayers { get; }

        public static OutputDescription ShadowPassOutput => new OutputDescription(new OutputAttachmentDescription(PixelFormat.R32_Float));
        public static OutputDescription DefaultOutput => new OutputDescription(new OutputAttachmentDescription(PixelFormat.R32_Float), new OutputAttachmentDescription(PixelFormat.R16_G16_B16_A16_Float), new OutputAttachmentDescription(PixelFormat.R32_UInt));
    }

    public static class RenderManagerExtensions
    {
        public static void RenderOnce(this IRenderManager renderManager, IMesh mesh, Material material, int submesh, Matrix localToWorld)
        {
            var renderer = new MeshRenderer { SubMeshId = submesh, Material = material, Mesh = mesh };
            renderManager.RenderOnce(new LocalToWorld { Matrix = localToWorld }, renderer);
        }

        public static void RenderOnce(this IRenderManager renderManager, IMesh mesh, Material material, int submesh, Transform transform)
        {
            renderManager.RenderOnce(mesh, material, submesh, transform.LocalToWorldMatrix);
        }

        public static void RenderOnce(this IRenderManager renderManager, IMesh mesh, Material material, int submesh, Vector3 position)
        {
            renderManager.RenderOnce(mesh, material, submesh, Matrix.CreateTranslation(position));
        }

        public static void DrawBox(this IRenderManager renderManager, Vector3 min, Vector3 max, Vector4 color)
        {
            renderManager.DrawLine(new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, min.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, max.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z), color);
            
            
            renderManager.DrawLine(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z), color);
            renderManager.DrawLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), color);
            renderManager.DrawLine(new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z), color);
            renderManager.DrawLine(new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, min.Y, min.Z), color);
            
            renderManager.DrawLine(new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z), color);
            renderManager.DrawLine(new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, min.Y, max.Z), color);
        }
        
        public static void DrawFrustum(this IRenderManager renderManager, BoundingFrustum f, Vector4 color)
        {
            Vector3[] corners = f.GetCorners();
            
            // Draw the frustum edges
            for (int i = 0; i < 4; i++)
            {
                renderManager.DrawLine(corners[i], corners[(i + 1) % 4], color);
                renderManager.DrawLine(corners[i + 4], corners[(i + 1) % 4 + 4], color);
                renderManager.DrawLine(corners[i], corners[i + 4], color);
            }

            // Draw the near and far plane connections
            for (int i = 0; i < 4; i++)
            {
                renderManager.DrawLine(corners[i], corners[i + 4], color);
            }
        }
    }
}
