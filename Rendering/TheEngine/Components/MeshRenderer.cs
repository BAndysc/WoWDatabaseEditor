using TheEngine.ECS;
using TheEngine.Handles;

namespace TheEngine.Components
{
    public struct MeshRenderer : IComponentData
    {
        public MeshHandle MeshHandle;
        public MaterialHandle MaterialHandle;
        public int SubMeshId;
    }
}