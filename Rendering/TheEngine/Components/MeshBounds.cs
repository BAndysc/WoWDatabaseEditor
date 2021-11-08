using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct MeshBounds : IComponentData
    {
        public BoundingBox box;
        
        private MeshBounds(BoundingBox box)
        {
            this.box = box;
        }

        public static implicit operator BoundingBox(MeshBounds d) => d.box;
        public static explicit operator MeshBounds(BoundingBox b) => new MeshBounds(b);
    }
}