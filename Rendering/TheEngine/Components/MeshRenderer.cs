using System.Runtime.InteropServices;
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
        private MeshHandle meshHandle;
        internal GCHandle meshGcHandle;
        public MeshHandle MeshHandle => meshHandle;
        private MaterialHandle materialHandle;
        internal GCHandle materialGcHandle;
        public MaterialHandle MaterialHandle => materialHandle;

        public IMesh Mesh
        {
            set
            {
                if (meshGcHandle != default)
                {
                    meshGcHandle.Free();
                }
                meshHandle = value.Handle;
                meshGcHandle = GCHandle.Alloc(value);
            }
        }

        public Material Material
        {
            set
            {
                if (materialGcHandle != default)
                {
                    materialGcHandle.Free();
                }
                materialHandle = value.Handle;
                materialGcHandle = GCHandle.Alloc(value);
                Opaque = !value.BlendingEnabled;
            }
        }
    }

    public static class MeshRenderers
    {
        public static void SetRenderer(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, Material material)
        {
            entityManager.GetComponent<MeshRenderer>(entity).Mesh = mesh;
            entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            entityManager.GetComponent<MeshRenderer>(entity).Material = material;
            entityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
        }
        
        public static void SetCollider(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, uint collisionMask)
        {
            entityManager.GetComponent<MeshRenderer>(entity).Mesh = mesh;
            entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = subMesh;
            entityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
            entityManager.GetComponent<Collider>(entity).CollisionMask = collisionMask;
        }
    }
}