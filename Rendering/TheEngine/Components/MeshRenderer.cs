using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;

namespace TheEngine.Components
{
    public struct MeshRenderer : IComponentData
    {
        public int SubMeshId;
        public bool Opaque;
        public MeshHandle MeshHandle;
        public MaterialHandle MaterialHandle;

        public Material Material
        {
            set
            {
                MaterialHandle = value.Handle;
                Opaque = !value.BlendingEnabled;
            }
        }
    }

    public static class MeshRenderers
    {
        public static void SetRenderer(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, Material material)
        {
            entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = mesh.Handle;
            entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            entityManager.GetComponent<MeshRenderer>(entity).Material = material;
            entityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
        }
        
        public static void SetCollider(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, uint collisionMask)
        {
            entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = mesh.Handle;
            entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            entityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
            entityManager.GetComponent<Collider>(entity).CollisionMask = collisionMask;
        }
    }
}