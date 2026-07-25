using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Physics
{
    public struct BoxCollider : IComponentData
    {
        public Vector3 Size = new(1, 1, 1);
        public Vector3 Center = Vector3.Zero;

        public BoxCollider()
        {
        }
    }
}
