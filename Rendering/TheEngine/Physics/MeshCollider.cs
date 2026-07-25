using TheEngine.ECS;
using TheEngine.Handles;

namespace TheEngine.Physics
{
    /// <summary>
    /// Static-only triangle-soup collider built from an existing render mesh.
    /// If the entity also has a <see cref="RigidBody"/>, the reconcile pass logs a
    /// warning and treats the body as static anyway (Bepu meshes have no inertia tensor).
    /// </summary>
    public struct MeshCollider : IComponentData
    {
        public MeshHandle Mesh;
        public int SubMesh = 0;

        public MeshCollider()
        {
        }
    }
}
