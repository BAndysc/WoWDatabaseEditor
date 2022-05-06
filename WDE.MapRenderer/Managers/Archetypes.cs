using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheMaths;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class M2AnimationComponentData : IManagedComponentData
{
    public readonly M2 Model;
    public int SetNewAnimation;
    // don't touch those fields outside of animation system
    public int _currentAnimation;
    public uint _length;
    public int _animInternalIndex;
    public float _time;
    public NativeBuffer<Matrix> _buffer = null!;

    public M2AnimationComponentData(M2 model)
    {
        Model = model;
        _currentAnimation = -1;
    }
}

public class Archetypes
{
    private readonly IEntityManager entityManager;
    public Archetype CollisionOnlyArchetype;
    public Archetype RenderEntityArchetype;
    public Archetype TerrainEntityArchetype;
    public Archetype AnimatedEntityArchetype;
    public Archetype StaticM2WorldObjectArchetype;                // <- renderer which is not animated
    public Archetype StaticM2WorldObjectAnimatedArchetype;       // <-- renderer which is animated
    public Archetype StaticM2WorldObjectAnimatedMasterArchetype; // <-- actually updates animation

    public Archetypes(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
        AnimatedEntityArchetype = entityManager.NewArchetype()
                .WithManagedComponentData<M2AnimationComponentData>();
                
        StaticM2WorldObjectArchetype = entityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshRenderer>();

        StaticM2WorldObjectAnimatedArchetype = StaticM2WorldObjectArchetype
            .WithManagedComponentData<MaterialInstanceRenderData>();

        StaticM2WorldObjectAnimatedMasterArchetype = StaticM2WorldObjectAnimatedArchetype
            .WithManagedComponentData<M2AnimationComponentData>();
                
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