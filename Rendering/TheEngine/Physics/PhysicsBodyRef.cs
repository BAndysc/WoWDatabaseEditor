using BepuPhysics.Collidables;
using TheEngine.ECS;
using TheEngine.Entities;

namespace TheEngine.Physics
{
    /// <summary>
    /// Runtime-only component added by <see cref="PhysicsManager"/>'s reconcile pass once
    /// an authoring collider/rigidbody has been turned into a Bepu body or static. This is
    /// the single owner of the Bepu-side resources for the entity: removing it (which happens
    /// automatically when the entity, or any of its collider components, is destroyed) frees
    /// the body/static and its shape.
    /// </summary>
    internal struct PhysicsBodyRef : IComponentData
    {
        public int Handle;
        public bool IsStatic;
        public bool IsKinematic;
        public TypedIndex Shape;

        // picked up automatically by ComponentTypeData<PhysicsBodyRef> via naming convention
        public static void OnRemoved(Engine engine, Entity entity, ref PhysicsBodyRef component)
        {
            engine.physicsManager.RemoveBody(entity, in component);
        }
    }
}
