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
        void DrawLine(Vector3 start, Vector3 end);
        void Render(IMesh mesh, Material material, int submesh, Transform transform);
        void Render(IMesh mesh, Material material, int submesh, Matrix localToWorld, Matrix? worldToLocal = null);
        void Render(IMesh mesh, Material material, int submesh, Vector3 position);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count, Matrix localToWorld, Matrix? worldToLocal = null);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count);
        float ViewDistanceModifier { get; set; }
    }
}
