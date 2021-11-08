using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Components
{
    public struct WorldMeshBounds : IComponentData
    {
        public BoundingBox box;
        
        private WorldMeshBounds(BoundingBox box)
        {
            this.box = box;
        }

        public static implicit operator BoundingBox(WorldMeshBounds d) => d.box;
        public static explicit operator WorldMeshBounds(BoundingBox b) => new WorldMeshBounds(b);
    }
}