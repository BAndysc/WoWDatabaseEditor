using TheEngine.Entities;
using TheEngine.Handles;

namespace TheEngine.Interfaces
{
    public interface IRenderManager
    {
        StaticRenderHandle RegisterStaticRenderer(MeshHandle mesh, Material material, int subMesh, Transform transform);
        void UnregisterStaticRenderer(StaticRenderHandle staticRenderHandle);
        
        DynamicRenderHandle RegisterDynamicRenderer(MeshHandle mesh, Material material, int subMesh, Transform transform);
        void UnregisterDynamicRenderer(DynamicRenderHandle staticRenderHandle);
        void Render(IMesh mesh, Material material, int submesh, Transform transform);
        void RenderInstancedIndirect(IMesh mesh, Material material, int submesh, int count);
    }
}
