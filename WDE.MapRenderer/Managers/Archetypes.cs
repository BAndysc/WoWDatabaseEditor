using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheMaths;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class M2AnimationComponentData : IManagedComponentData
{
    public readonly M2 Model;
    public M2AnimationComponentData? AttachedTo;
    public AnimationDataFlags Flags = AnimationDataFlags.None;

    public int SetNewAnimation;
    public M2AttachmentType? AttachmentType;
    // don't touch those fields outside of animation system
    public int _currentAnimation;
    public uint _length;
    public int _animInternalIndex;
    public float _time;
    public NativeBuffer<Matrix> _buffer = null!;

    public M2AnimationComponentData(M2 model, M2AnimationComponentData? attachedTo = null, M2AttachmentType? attachmentType = null)
    {
        Model = model;
        AttachedTo = attachedTo;
        AttachmentType = attachmentType;
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

    public Archetype CullingArchetype; // objects that are culled by AABB
    public Archetype DynamicObjectArchetype; // object that can move around (DirtyPosition)
    
    public Archetype WorldObjectCollider;

    public Archetype AnimatedWorldObjectArchetype;
    public Archetype WorldObjectArchetype;
    public Archetype AttachmentsAnimationRootArchetype;
    public Archetype WorldObjectMeshRendererArchetype;

    
    public Archetypes(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
        AnimatedEntityArchetype = entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithManagedComponentData<M2AnimationComponentData>();
                
        StaticM2WorldObjectArchetype = entityManager.NewArchetype()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<PerformCullingBit>()
                .WithComponentData<WorldMeshBounds>()
                .WithComponentData<MeshBounds>()
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
            .WithComponentData<PerformCullingBit>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();
        
        RenderEntityArchetype = entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<MeshBounds>()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();

        CullingArchetype = entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<PerformCullingBit>()
            .WithComponentData<WorldMeshBounds>();

        DynamicObjectArchetype = entityManager.NewArchetype()
            .WithComponentData<DirtyPosition>();

        WorldObjectCollider = CollisionOnlyArchetype
            .WithComponentData<MeshBounds>()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<DisabledObjectBit>()
            .WithComponentData<CopyParentTransform>();

        WorldObjectArchetype = entityManager.NewArchetype()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<MeshBounds>()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<PerformCullingBit>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<LocalToWorld>();

        AnimatedWorldObjectArchetype = WorldObjectArchetype
            .WithManagedComponentData<M2AnimationComponentData>();
        
        AttachmentsAnimationRootArchetype = entityManager.NewArchetype()
            .Includes(CullingArchetype)
            .Includes(DynamicObjectArchetype)
            .WithComponentData<LocalToWorld>()
            .WithComponentData<CopyParentTransform>()
            .WithComponentData<MeshBounds>()
            .WithManagedComponentData<M2AnimationComponentData>();
        
        WorldObjectMeshRendererArchetype = entityManager.NewArchetype()
            .Includes(DynamicObjectArchetype)
            .Includes(CullingArchetype)
            .Includes(RenderEntityArchetype)
            .Includes(StaticM2WorldObjectAnimatedArchetype)
            .WithComponentData<CopyParentTransform>();
    }
}