using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Physics
{
    public struct PhysicsHit
    {
        public Entity Entity;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
    }
}
