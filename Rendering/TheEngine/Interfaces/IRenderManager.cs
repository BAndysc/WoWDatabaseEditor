using TheEngine.Entities;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IRenderManager
    {
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterStaticRenderer(StaticRenderHandle staticRenderHandle);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Transform t);
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Matrix localToWorld);
        void UnregisterDynamicRenderer(DynamicRenderHandle staticRenderHandle);
        void DrawLine(Vector3 start, Vector3 end);
        void Render(IMesh mesh, Material material, int submesh, Transform transform);
        void Render(IMesh mesh, Material material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null);
        void Render(IMesh mesh, Material material, int submesh, Vector3 position);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count, Matrix localToWorld, Matrix? worldToLocal = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count);
        float ViewDistanceModifier { get; set; }
    }

    public static class RenderManagerExtensions
    {
        public static void DrawBox(this IRenderManager renderManager, Vector3 min, Vector3 max)
        {
            renderManager.DrawLine(new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, min.Y, max.Z));
            renderManager.DrawLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z));
            renderManager.DrawLine(new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, max.Y, max.Z));
            renderManager.DrawLine(new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z));
            
            
            renderManager.DrawLine(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z));
            renderManager.DrawLine(new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z));
            renderManager.DrawLine(new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z));
            renderManager.DrawLine(new Vector3(min.X, max.Y, min.Z), new Vector3(min.X, min.Y, min.Z));
            
            renderManager.DrawLine(new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z));
            renderManager.DrawLine(new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z));
            renderManager.DrawLine(new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z));
            renderManager.DrawLine(new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, min.Y, max.Z));
        }
    }
}
