using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Physics
{
    public struct SphereCollider : IComponentData
    {
        public float Radius = 0.5f;
        public Vector3 Center = Vector3.Zero;

        public SphereCollider()
        {
        }
    }
}
