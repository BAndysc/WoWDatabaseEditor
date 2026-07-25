using System;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Physics
{
    public interface IPhysicsManager
    {
        Vector3 Gravity { get; set; }
        bool Enabled { get; set; }

        event Action<Entity, Entity> ContactBegin;
        event Action<Entity, Entity> ContactEnd;

        bool RayCast(in Ray ray, float maxDistance, out PhysicsHit hit);
    }
}
