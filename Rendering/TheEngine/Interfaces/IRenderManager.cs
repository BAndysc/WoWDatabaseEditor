using TheAvaloniaOpenGL.Resources;
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
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Transform transform);
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null, Int4? instanceInt = null);
        void Render(MeshHandle mesh, MaterialHandle material, ShaderPassType shaderPassType, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null, Int4? instanceInt = null);
        void Render(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, Vector3 position);
        void RenderFullscreenPlane(Material material);
        void ActivateDefaultRenderTexture();
        void ActivateRenderTexture(ITexture rt, Color4? color = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int count, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, ShaderPassType shaderPassType, int submesh, int count, MaterialInstanceRenderData? instanceData = null);
        float ViewDistanceModifier { get; set; }
        void ActivateScene(in SceneData? scene);
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
        ITexture DepthTexture { get; }
        ITexture OpaqueTexture { get; }
        /// <summary>
        /// Adds a custom render stage; stages render in registration order, after the built-in
        /// object stage. Register and unregister outside the render loop. The caller keeps
        /// ownership of the stage and disposes it after unregistering.
        /// </summary>
        void RegisterRenderStage(IRenderStage stage);
        void UnregisterRenderStage(IRenderStage stage);
        /// <summary>
        /// When true (the default), the frame is recorded into a deferred command list and
        /// executed at the end of the frame, like a Vulkan command buffer; when false every
        /// command executes immediately on the GL context. Takes effect at the start of the
        /// next frame. Exists to A/B the two paths on the GL backend.
        /// </summary>
        bool DeferredRecording { get; set; }
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
        IReadOnlyList<RenderLayerData> RenderLayers { get; }

        public static OutputDescription ShadowPassOutput => new OutputDescription(new OutputAttachmentDescription(PixelFormat.R32_Float));
        public static OutputDescription DefaultOutput => new OutputDescription(new OutputAttachmentDescription(PixelFormat.R32_Float), new OutputAttachmentDescription(PixelFormat.R16_G16_B16_A16_Float), new OutputAttachmentDescription(PixelFormat.R32_UInt));
    }

    public static class RenderManagerExtensions
    {
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
