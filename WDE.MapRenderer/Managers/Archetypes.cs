using ImGuiNET;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using WDE.MapRenderer.Managers.Entities;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class M2AnimationComponentData : IManagedComponentData
{
    public readonly M2 Model;
    public M2AnimationComponentData? AttachedTo;
    public AnimationDataFlags Flags = AnimationDataFlags.None;

    public AnimationDataFlags OneShotFlags = AnimationDataFlags.None;
    public int SetNewOneShotAnimation;
    public int SetNewAnimation;
    public M2AttachmentType? AttachmentType;
    public float SpeedModifier = 1;
    // don't touch those fields outside of animation system
    public int _currentAnimation;
    public uint _length;
    public int _animInternalIndex;
    public float _time;
    public INativeBuffer<Matrix> _buffer = null!;
    public INativeBuffer<Vector4> _colors = null!;
    public INativeBuffer<Matrix> _textureTransforms = null!;

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

    public Archetype CullingArchetype; // objects that are culled by AABB
    public Archetype DynamicObjectArchetype; // object that can move around (DirtyPosition)
    
    public Archetype WorldObjectCollider;

    public Archetype AnimatedWorldObjectArchetype;
    public Archetype WorldObjectArchetype;
    public Archetype AttachmentArchetype;
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
            .WithComponentData<MeshRenderer>()
            .WithComponentData<Adt_M2Object>()
            .WithManagedComponentData<MdxRenderer>();

        StaticM2WorldObjectAnimatedArchetype = StaticM2WorldObjectArchetype
            .WithManagedComponentData<MaterialInstanceRenderData>()
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
            .WithComponentData<LocalToWorld>()
            .WithComponentData<MeshRenderer>()
            .WithManagedComponentData<MaterialInstanceRenderData>();

        AnimatedWorldObjectArchetype = WorldObjectArchetype
            .WithManagedComponentData<M2AnimationComponentData>()
            .WithManagedComponentData<MdxRenderer>();
        
        AttachmentArchetype = entityManager.NewArchetype()
            .Includes(CullingArchetype)
            .Includes(DynamicObjectArchetype)
            .Includes(RenderEntityArchetype)
            .WithComponentData<LocalToWorld>()
            .WithComponentData<CopyParentTransform>()
            .WithComponentData<MeshBounds>()
            .WithManagedComponentData<MaterialInstanceRenderData>()
            .WithManagedComponentData<M2AnimationComponentData>();

        // Creature/attachment per-renderer child entities no longer use this archetype (renderers are array
        // components on the parent entity now), but ChunkManager still uses it for WMO render entities.
        WorldObjectMeshRendererArchetype = entityManager.NewArchetype()
            .Includes(DynamicObjectArchetype)
            .Includes(CullingArchetype)
            .Includes(RenderEntityArchetype)
            .Includes(StaticM2WorldObjectArchetype)
            .WithComponentData<CopyParentTransform>()
            .WithManagedComponentData<MaterialInstanceRenderData>();
    }
}


[AutoRegister]
public class M2AnimationComponentInspector : IInspectorDrawer<M2AnimationComponentData>
{
    private readonly IEntityManager entityManager;

    public M2AnimationComponentInspector(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
    }

    public void Draw(M2AnimationComponentData component)
    {
        ImGui.TextUnformatted("Internal anim index: " + component._animInternalIndex);
        ImGui.TextUnformatted("Current anim: " + component._currentAnimation);
        ImGui.TextUnformatted("Time: " + component._time);
        ImGui.TextUnformatted("Length: " + component._length);
        ImGui.TextUnformatted("Speed modifier: " + component.SpeedModifier);
        ImGui.TextUnformatted("Flags: " + component.Flags);

        var lookup = component.Model.sequenceIdToAnimationLookup;
        List<int> validAnimationIds = new List<int>();
        List<string> validAnimationNames = new List<string>();
        for (int animId = 0; animId < lookup.Length; ++animId)
        {
            if (lookup[animId] != -1)
            {
                validAnimationIds.Add(animId);
                var name = Enum.GetName(typeof(M2AnimationType), animId) ?? $"Anim {animId}";
                validAnimationNames.Add(name);
            }
        }

        int animIndex = 0;
        if (ImGui.Combo("Set Animation", ref animIndex, validAnimationNames.ToArray(), validAnimationNames.Count))
        {
            component.SetNewAnimation = validAnimationIds[animIndex];
        }
    }
}
