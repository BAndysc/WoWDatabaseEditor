using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IRenderManager
    {
        void SetupRendererEntity(Entity entity, MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterStaticRenderer(StaticRenderHandle staticRenderHandle);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterDynamicRenderer(DynamicRenderHandle staticRenderHandle);
        void DrawLine(Vector3 start, Vector3 end, Vector4 color);
        void Render(IMesh mesh, Material material, int submesh, Transform transform);
        void Render(IMesh mesh, Material material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null);
        void Render(MeshHandle mesh, MaterialHandle material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null, MaterialInstanceRenderData? instanceData = null);
        void Render(IMesh mesh, Material material, int submesh, Vector3 position);
        void RenderFullscreenPlane(Material material);
        void ActivateDefaultRenderTexture();
        void ActivateRenderTexture(TextureHandle rt, Color4? color = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count, Matrix localToWorld, Matrix? worldToLocal = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count);
        float ViewDistanceModifier { get; set; }
        void ActivateScene(in SceneData? scene);
        void AddPostprocess(IPostProcess postProcess);
        void RemovePostprocess(IPostProcess postProcess);
        void SetDynamicResolutionScale(float scale);
        void DrawSphere(Vector3 center, float radius, Vector4 color);
        Entity PickObject(Vector2 normalizedScreenPosition);
        TextureHandle DepthTexture { get; }
        TextureHandle OpaqueTexture { get; }
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
