using TheEngine.Components;
using TheEngine.ECS;

namespace WDE.MapRenderer.Managers;

public class Archetypes
{
    private readonly IEntityManager entityManager;
    public Archetype CollisionOnlyArchetype;
    public Archetype RenderEntityArchetype;
    public Archetype TerrainEntityArchetype;

    public Archetypes(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
        CollisionOnlyArchetype = entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Collider>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();
        TerrainEntityArchetype = entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();
        RenderEntityArchetype = entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<MeshBounds>()
            .WithComponentData<DirtyPosition>()
//                .WithComponentData<Collider>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();
    }
}