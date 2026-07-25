using TheEngine.ECS;

namespace TheEngine.Physics
{
    /// <summary>
    /// Per-collider physics material. Bounciness has no native Bepu equivalent and is
    /// approximated via the contact spring/recovery-velocity settings (see PhysicsManager).
    /// </summary>
    public struct PhysicsMaterial : IComponentData
    {
        public float Friction = 1;
        public float Bounciness = 0;

        public PhysicsMaterial()
        {
        }
    }
}
