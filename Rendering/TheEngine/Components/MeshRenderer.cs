using TheEngine.ECS;
using TheEngine.Handles;

namespace TheEngine.Components
{
    public struct MeshRenderer : IComponentData
    {
        public int SubMeshId;
        public bool Opaque;
        public MeshHandle MeshHandle;
        public MaterialHandle MaterialHandle;
    }
}