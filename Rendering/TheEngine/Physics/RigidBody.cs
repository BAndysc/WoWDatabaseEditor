using TheEngine.ECS;

namespace TheEngine.Physics
{
    public enum BodyType
    {
        Dynamic,
        Kinematic
    }

    /// <summary>
    /// Presence of this component on a collider entity makes it a dynamic/kinematic Bepu body.
    /// Absence (collider-only entity) makes it a static collidable.
    /// </summary>
    public struct RigidBody : IComponentData
    {
        public BodyType Type = BodyType.Dynamic;
        public float Mass = 1;
        public float LinearDamping = 0;
        public float AngularDamping = 0;
        public float GravityScale = 1;

        public RigidBody()
        {
        }
    }
}
