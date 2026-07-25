using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Physics
{
    /// <summary>Bepu capsules are aligned along the local Y axis.</summary>
    public struct CapsuleCollider : IComponentData
    {
        public float Radius = 0.5f;
        public float Length = 1;
        public Vector3 Center = Vector3.Zero;

        public CapsuleCollider()
        {
        }
    }
}
