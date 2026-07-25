using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Prism.Ioc;
using TheEngine;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

public class SpawnDragger : System.IDisposable
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IInputManager inputManager;
    private readonly IWorldInteractionService interaction;
    private readonly IWorldSpawnEditService editService;
    private readonly IFormationEditorService formationService;
    private readonly ISpawnEditorToolService toolService;
    private readonly List<CreatureSpawnInstance> memberScratch = new();

    private readonly Engine engine;

    // the shared grab/rotate state machine (G / G-again snap / R yaw rotation / Escape / click)
    private readonly TransformDragger dragger;
    private readonly SpawnDragTarget dragTarget = new();

    private CaptureLease gizmoLease;
    private bool gizmoMoved;

    private SpawnInstance? lastSelected;

    /// <summary>Adapts the selected spawn for <see cref="TransformDragger"/>: creatures expose
    /// their yaw (R = interactive mouse rotation); gameobjects have a full quaternion, so they
    /// report no yaw and rotate via the ImGuizmo rotate ball instead.</summary>
    private sealed class SpawnDragTarget : IDragTarget
    {
        public SpawnInstance? Spawn;

        public Vector3 Position
        {
            get => Spawn?.WorldObject?.Position ?? Vector3.Zero;
            set
            {
                if (Spawn?.WorldObject != null)
                    Spawn.WorldObject.Position = value;
            }
        }

        public bool HasOrientation => Spawn is CreatureSpawnInstance { Creature: not null };

        public float Orientation
        {
            get => Spawn is CreatureSpawnInstance { Creature: { } c } ? c.Orientation : 0f;
            set
            {
                if (Spawn is CreatureSpawnInstance { Creature: { } c })
                    c.Orientation = value;
            }
        }
    }

    public SpawnDragger(IContainerProvider containerProvider,
        ISpawnSelectionService spawnSelectionService,
        IInputManager inputManager,
        RaycastSystem raycastSystem,
        IWorldInteractionService interaction,
        IWorldSpawnEditService editService,
        IFormationEditorService formationService,
        ISpawnEditorToolService toolService,
        Engine engine)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.inputManager = inputManager;
        this.interaction = interaction;
        this.editService = editService;
        this.formationService = formationService;
        this.toolService = toolService;
        this.engine = engine;
        dragger = new TransformDragger(engine, inputManager, raycastSystem, interaction, toolService);
        dragger.Committed += () =>
        {
            if (dragTarget.Spawn != null)
                CommitTransform(dragTarget.Spawn);
        };
    }

    /// <summary>The in-flight grab/rotate hint for the hint bar (null when idle).</summary>
    public string? DragHint => dragger.ActiveHint;

    /// <summary>Axis-lock guide line while a grab is constrained (no-op otherwise).</summary>
    public void DrawGuides() => dragger.DrawGuides();

    /// <summary>Persists the spawn's current world transform as a pending edit (UPDATE on Save).
    /// Called when a drag/rotation finishes (not on cancel, which restores the old transform).
    /// Public: the Select inspector's numeric transform fields commit through here too, so
    /// formation members follow a leader edit exactly like they do for a gizmo drag.</summary>
    public void CommitTransform(SpawnInstance spawn)
    {
        if (!editService.IsAvailable || spawn.WorldObject == null)
            return;
        float orientation = spawn switch
        {
            CreatureSpawnInstance cr when cr.Creature != null => cr.Creature.Orientation,
            GameObjectSpawnInstance go when go.GameObject != null => go.GameObject.Orientation,
            _ => 0f
        };
        // gameobjects persist their FULL rotation quaternion (rotation0-3), not just yaw - the
        // rotate gizmo can tilt them freely and a tilt must survive the save
        Quaternion? rotation = spawn is GameObjectSpawnInstance { GameObject: { } g } ? g.Rotation : null;
        editService.MoveSpawn(spawn is CreatureSpawnInstance, spawn.Guid, spawn.WorldObject.Position, orientation, rotation);

        // moving/rotating a formation LEADER drags its members along (SyncConstraints repositions
        // them live) - persist their induced transforms too, or the DB keeps their old positions
        if (spawn is CreatureSpawnInstance leader)
        {
            // re-run the constraint sync so the members reflect the leader's FINAL transform (their
            // per-frame sync may lag the last frame of the drag by one frame)
            formationService.SyncConstraints();

            memberScratch.Clear();
            formationService.CollectMemberCreatures(leader.Guid, memberScratch);
            foreach (var member in memberScratch)
            {
                if (member.WorldObject == null || member.Creature == null)
                    continue;
                editService.MoveSpawn(true, member.Guid, member.WorldObject.Position, member.Creature.Orientation);
            }
        }
    }

    private void ReleaseGizmoCapture()
    {
        if (gizmoLease.IsActive)
            gizmoLease.Release();
        gizmoLease = default;
    }

    static Span<float> AsSpan(ref Matrix4x4 matrix)
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<Matrix4x4, float>(ref matrix),
            16
        );
    }

    /// <summary>Cancels any in-flight drag/rotation (restoring the original transform) and hides the
    /// gizmo state. Called when the active tool is not Select — the spawn grabber belongs to the
    /// Select tool only; other tools (waypoints, formations...) own the pointer themselves.</summary>
    public void Deactivate()
    {
        dragTarget.Spawn = lastSelected is { IsSpawned: true } ? lastSelected : null;
        dragger.Cancel(dragTarget.Spawn != null ? dragTarget : null);
        gizmoMoved = false;
        ReleaseGizmoCapture();
    }

    public void Update(float delta)
    {
        var selected = spawnSelectionService.SelectedSpawn.Value;

        if (!ReferenceEquals(selected, lastSelected))
        {
            lastSelected = selected;
            // while a drag is in flight the pointer is captured, so the selection cannot actually
            // change mid-drag - this is just a clean reset for the new selection
            dragger.Cancel(null);
            toolService.GizmoMode = GizmoMode.Translate;
            gizmoMoved = false;
            ReleaseGizmoCapture();
        }

        if (selected is not { IsSpawned: true } selectedSpawn)
            return;

        if (interaction.IsCaptured && !dragger.IsActive && !gizmoLease.IsActive)
            return;

        dragTarget.Spawn = selectedSpawn;
        if (dragger.Update(dragTarget))
            return;

        if (toolService.ShowGizmoHandles)
            Manipulate(selectedSpawn);
    }

    private unsafe void Manipulate(SpawnInstance selectedSpawn)
    {
        var view = engine.CameraManager.MainCamera.ViewMatrix;
        var proj = engine.CameraManager.MainCamera.ProjectionMatrix;
        var local = selectedSpawn.WorldObject!.LocalToWorld;

        var operation = toolService.GizmoMode switch
        {
            // creatures only persist yaw, so their rotate ball is constrained to the Z axis -
            // a free tilt would silently snap back on save; gameobjects rotate freely (full
            // quaternion, persisted as rotation0-3)
            GizmoMode.Rotate => selectedSpawn is CreatureSpawnInstance ? ImGuizmoOperation.RotateZ : ImGuizmoOperation.Rotate,
            GizmoMode.Scale => ImGuizmoOperation.Scale,
            _ => ImGuizmoOperation.Translate,
        };

        // Ctrl (or Cmd) = snap: translate to a 0.5 grid, rotate to 15° steps, scale to 0.1
        var io = ImGui.GetIO();
        bool snapHeld = io.KeyCtrl || io.KeySuper;
        float snapStep = operation switch
        {
            ImGuizmoOperation.Rotate or ImGuizmoOperation.RotateZ => 15f, // ImGuizmo rotate snap is in degrees
            ImGuizmoOperation.Scale => 0.1f,
            _ => 0.5f,
        };
        var snap = stackalloc float[3] { snapStep, snapStep, snapStep };

        fixed (float* viewPtr = AsSpan(ref view))
        fixed (float* projPtr = AsSpan(ref proj))
        fixed (float* localPtr = AsSpan(ref local))
        {
            // ImGuizmo only scales correctly in Local space; translate/rotate use World.
            var space = operation == ImGuizmoOperation.Scale ? ImGuizmoMode.Local : ImGuizmoMode.World;
            if (ImGuizmo.Manipulate(viewPtr, projPtr, operation, space, localPtr, null, snapHeld ? snap : null))
            {
                selectedSpawn.WorldObject!.LocalToWorld = local;
                gizmoMoved = true;
            }
        }

        bool usingGizmo = ImGuizmo.IsUsing();
        if (usingGizmo && !gizmoLease.IsActive)
            interaction.TryCapture(out gizmoLease);
        else if (!usingGizmo && gizmoLease.IsActive)
        {
            ReleaseGizmoCapture();
            if (gizmoMoved)
            {
                gizmoMoved = false;
                CommitTransform(selectedSpawn);
            }
        }
    }

    public void RenderTransparent() { }

    public void Dispose() { }
}