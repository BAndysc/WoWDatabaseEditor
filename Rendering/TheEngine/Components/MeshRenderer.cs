using System.Runtime.InteropServices;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;

namespace TheEngine.Components
{
    [ArrayComponent]
    public struct MeshRenderer : IComponentData
    {
        // The precomputed batching/sort key (see SortKey for the bit layout and limits) is consumed by
        // the ObjectDrawRenderStage radix sort. It is recomputed whenever a field feeding it changes.
        private int subMeshId;
        private MeshHandle meshHandle;
        private StaticReference meshGcHandle;
        private MaterialHandle materialHandle;
        private StaticReference materialGcHandle;
        private PipelineHandle pipelineHandle;
        private bool opaque;
        private SortKey sortKey;
        public Int4? InstanceData;
        // WorldBounds and SkipDraw are frame-transient: SkipDraw is written by the per-renderer culling pass
        // and read by the draw pass within the same frame. They are kept inline (rather than in a separate
        // per-frame structure) on purpose, so the culling and draw passes touch a single contiguous buffer.
        public WorldMeshBounds WorldBounds;
        public bool SkipDraw;
        // Persistent per-renderer visibility (e.g. geosets hidden by default), folded into SkipDraw by the culling pass
        public bool Hidden;

        // SubMeshId and Opaque feed the sort key, so they are properties that recompute it on assignment.
        // Object-initializer assignment still works; whichever key field is set last produces the final key.
        public int SubMeshId
        {
            readonly get => subMeshId;
            set { subMeshId = value; RecomputeSortKey(); }
        }

        public bool Opaque
        {
            readonly get => opaque;
            set { opaque = value; RecomputeSortKey(); }
        }

        /// <summary>Precomputed 64-bit batching/sort key (see <see cref="Handles.SortKey"/>). Stable for the lifetime of the renderer's mesh+material+submesh.</summary>
        public readonly SortKey SortKey => sortKey;

        public MeshHandle MeshHandle => meshHandle;
        public MaterialHandle MaterialHandle => materialHandle;
        public PipelineHandle PipelineHandle => pipelineHandle;

        private void RecomputeSortKey()
        {
            sortKey = SortKey.Build(opaque, pipelineHandle.ShaderId, pipelineHandle.PipelineId,
                meshHandle.Handle, subMeshId);
        }

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
                    meshGcHandle = value.GetStaticReference();
                }

                RecomputeSortKey();
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
                    materialGcHandle = value.GetStaticReference();
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

        // picked up automatically by ComponentTypeData<MeshRenderer> via naming convention
        public static void OnRemoved(Engine engine, Entity entity, ref MeshRenderer component)
        {
            component.Mesh = null;
            component.Material = null;
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
            entityManager.GetComponent<LegacyCollider>(entity).CollisionMask = collisionMask;
        }
    }
}