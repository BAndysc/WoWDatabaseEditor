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
        void Render(IMesh mesh, Material material, int submesh, Transform transform);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count);
    }
}
