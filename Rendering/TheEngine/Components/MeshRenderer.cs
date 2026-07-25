using System.Runtime.InteropServices;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Components
{
    [ArrayComponent]
    public struct MeshRenderer : IComponentData
    {
        public int SubMeshId;
        private MeshHandle meshHandle;
        private GCHandle meshGcHandle;
        private MaterialHandle materialHandle;
        private GCHandle materialGcHandle;
        private PipelineHandle pipelineHandle;
        public Int4? InstanceData;
        // WorldBounds and SkipDraw are frame-transient: SkipDraw is written by the per-renderer culling pass
        // and read by the draw pass within the same frame. They are kept inline (rather than in a separate
        // per-frame structure) on purpose, so the culling and draw passes touch a single contiguous buffer.
        public WorldMeshBounds WorldBounds;
        public bool Opaque;
        public bool SkipDraw;
        // Persistent per-renderer visibility (e.g. geosets hidden by default), folded into SkipDraw by the culling pass
        public bool Hidden;

        public MeshHandle MeshHandle => meshHandle;
        public MaterialHandle MaterialHandle => materialHandle;
        public PipelineHandle PipelineHandle => pipelineHandle;

        public IMesh? Mesh
        {
            set
            {
                if (meshGcHandle != default)
                {
                    meshGcHandle.Free();
                    meshGcHandle = default;
                    meshHandle = default;
                }

                if (value != null)
                {
                    meshHandle = value.Handle;
                    meshGcHandle = GCHandle.Alloc(value);
                }
            }
        }

        public Material? Material
        {
            set
            {
                if (materialGcHandle != default)
                {
                    materialGcHandle.Free();
                    materialGcHandle = default;
                    materialHandle = default;
                    pipelineHandle = default;
                }

                if (value != null)
                {
                    materialHandle = value.Handle;
                    materialGcHandle = GCHandle.Alloc(value);
                    pipelineHandle = value.Pipeline.Handle;
                    Opaque = !value.BlendingEnabled;
                }
                else
                {
                    // no material - default to opaque
                    Opaque = true;
                }
            }
        }
    }

    public static class MeshRenderers
    {
        /** returns the index of the added renderer within the entity's MeshRenderer array component */
        public static int SetRenderer(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, Material? material, Int4? instanceData = null, bool hidden = false)
        {
            MeshRenderer renderer = new() { SubMeshId = subMesh, Material = material, Mesh = mesh, InstanceData = instanceData, Hidden = hidden};
            entityManager.AddArrayComponent(entity, renderer);
            var rendererIndex = entityManager.GetArrayComponents<MeshRenderer>(entity).Length - 1;
            if (entityManager.HasComponent<MeshBounds>(entity))
            {
                ref var bounds = ref entityManager.GetComponent<MeshBounds>(entity);
                if (bounds.box.Size == default)
                {
                    bounds.box = mesh.Bounds;
                }
                else
                {
                    bounds.box = BoundingBox.Union(bounds, mesh.Bounds);
                }
                if (entityManager.HasComponent<LocalToWorld>(entity) &&
                    entityManager.HasComponent<WorldMeshBounds>(entity))
                {
                    ref var localToWorld = ref entityManager.GetComponent<LocalToWorld>(entity);
                    var worldBounds = WorldMeshBounds.FromLocal(in bounds, in localToWorld);
                    ref var worldMeshBounds = ref entityManager.GetComponent<WorldMeshBounds>(entity);
                    worldMeshBounds = worldBounds;
                }
            }
            else
            {
                var bounds = new MeshBounds(){box = mesh.Bounds};

                if (entityManager.HasComponent<LocalToWorld>(entity) &&
                    entityManager.HasComponent<WorldMeshBounds>(entity))
                {
                    ref var localToWorld = ref entityManager.GetComponent<LocalToWorld>(entity);
                    var worldBounds = WorldMeshBounds.FromLocal(in bounds, in localToWorld);
                    ref var worldMeshBounds = ref entityManager.GetComponent<WorldMeshBounds>(entity);
                    if (worldMeshBounds.box.Size == default)
                    {
                        worldMeshBounds.box = worldBounds;
                    }
                    else
                    {
                        worldMeshBounds.box = BoundingBox.Union(worldMeshBounds, worldBounds);
                    }
                }
            }
            return rendererIndex;
        }

        public static void SetCollider(this Entity entity, IEntityManager entityManager, IMesh mesh, int subMesh, uint collisionMask)
        {
            MeshRenderer renderer = new() { SubMeshId = subMesh, Mesh = mesh };
            entityManager.AddArrayComponent(entity, renderer);
            entityManager.GetComponent<MeshBounds>(entity).box = mesh.Bounds;
            entityManager.GetComponent<Collider>(entity).CollisionMask = collisionMask;
        }
    }
}