using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using TheEngine;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using Key = TheEngine.Input.Key;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>
/// Shared mechanics for "keyed world point" editors (graveyards, spell target positions): beam
/// markers with orientation arrows and label chips, nearest-to-ray picking, an ImGuizmo
/// translate/rotate gizmo on the selected point, and an armed-placement flow ("next terrain click
/// creates"). Subclasses plug in the data (collect / transform / create) and the inspector.
/// </summary>
public abstract class WorldPointModuleBase : IGameModule
{
    // a point is "hit" if the cursor ray passes within this fraction of the distance to it
    private const float PickAngularThreshold = 0.018f;
    private const float BeamHeight = 6f;

    protected readonly Engine engine;
    protected readonly IGameContext gameContext;
    protected readonly ISpawnEditorToolService toolService;
    protected readonly IWorldInteractionService interaction;
    protected readonly IInputManager inputManager;
    protected readonly IGameViewOverlayService overlays;
    protected readonly RaycastSystem raycastSystem;

    public object? ViewModel => null;

    public struct WorldPoint
    {
        public uint Key;
        public Vector3 Position;
        public float Orientation;
        public string Label;
        public Vector4 Color;
    }

    private readonly List<WorldPoint> points = new();
    private int builtForRevision = -1;
    private int builtForMap = int.MinValue;
    private bool placementPreviewValid;
    private Vector3 placementPreviewPos;

    // the shared grab/rotate state machine - the same G / G-again-snap / R / Escape / click
    // shortcuts the spawn Select tool has
    private readonly TransformDragger dragger;
    private readonly SelectedPointTarget dragTarget;
    private uint? lastSelectedKey;

    private sealed class SelectedPointTarget : IDragTarget
    {
        private readonly WorldPointModuleBase module;

        public SelectedPointTarget(WorldPointModuleBase module) => this.module = module;

        public Vector3 Position
        {
            get => module.TryGetSelectedTransform(out var pos, out _) ? pos : Vector3.Zero;
            set
            {
                if (module.TryGetSelectedTransform(out _, out var orientation))
                    module.SetSelectedTransform(value, orientation);
            }
        }

        public bool HasOrientation => true;

        public float Orientation
        {
            get => module.TryGetSelectedTransform(out _, out var orientation) ? orientation : 0f;
            set
            {
                if (module.TryGetSelectedTransform(out var pos, out _))
                    module.SetSelectedTransform(pos, value);
            }
        }
    }

    /// <summary>Armed by the inspector: the next terrain click creates a point there.</summary>
    public bool PlacementArmed { get; set; }

    public uint? SelectedKey { get; set; }

    protected abstract SpawnEditorTool Tool { get; }
    /// <summary>Distinct ImGuizmo id so this gizmo doesn't fight the spawn (0) / waypoint (1) gizmos.</summary>
    protected abstract int GizmoId { get; }
    protected abstract IInspectorSection Section { get; }
    /// <summary>Pump pending service loads / trigger a reload on map change. Runs every Update
    /// regardless of the active tool.</summary>
    protected abstract void PumpAndSync();
    /// <summary>Fills the CURRENT map's points (the list is cleared by the caller).</summary>
    protected abstract void CollectPoints(List<WorldPoint> output);
    /// <summary>The service's Revision - every data mutation bumps it, so the point list (and its
    /// per-point label strings) is only rebuilt when it changes, not every frame.</summary>
    protected abstract int DataRevision { get; }
    protected abstract bool TryGetSelectedTransform(out Vector3 position, out float orientation);
    protected abstract void SetSelectedTransform(Vector3 position, float orientation);
    /// <summary>The armed placement clicked the world - create the row (and select it).</summary>
    protected abstract void PlaceAt(Vector3 position);
    /// <summary>Delete/Backspace on the selected point (applied on Save, like the inspector's delete).</summary>
    protected abstract void DeleteSelected(uint key);
    /// <summary>Extra pickables beyond the point markers (e.g. the area trigger DBC shapes). Tried
    /// after the point pick and the armed placement, before click-deselect.</summary>
    protected virtual bool TryPickExtra(Ray ray, out uint key)
    {
        key = 0;
        return false;
    }
    /// <summary>Whether the selection survives a data/map rebuild. By default the key must be among
    /// the rendered points; subclasses may accept extra keys (e.g. DBC shapes on the map).</summary>
    protected virtual bool SelectionStillExists(uint key)
    {
        foreach (var p in points)
        {
            if (p.Key == key)
                return true;
        }
        return false;
    }

    protected WorldPointModuleBase(Engine engine,
        IGameContext gameContext,
        ISpawnEditorToolService toolService,
        IWorldInteractionService interaction,
        IInputManager inputManager,
        IGameViewOverlayService overlays,
        RaycastSystem raycastSystem)
    {
        this.engine = engine;
        this.gameContext = gameContext;
        this.toolService = toolService;
        this.interaction = interaction;
        this.inputManager = inputManager;
        this.overlays = overlays;
        this.raycastSystem = raycastSystem;
        dragTarget = new SelectedPointTarget(this);
        dragger = new TransformDragger(engine, inputManager, raycastSystem, interaction, toolService);
    }

    public virtual void Initialize() => overlays.SetSection(Tool, Section);

    public virtual void Dispose() => overlays.SetSection(Tool, null);

    public int CurrentMapId => gameContext.CurrentMapId;

    /// <summary>The in-flight grab/rotate hint for the hint bar (null when idle).</summary>
    public string? DragHint => dragger.ActiveHint;

    /// <summary>Moves the camera to the point (switching maps when needed).</summary>
    public void FlyTo(uint map, Vector3 position) => gameContext.SetMap((int)map, position);

    public void Update(float delta)
    {
        PumpAndSync();

        if (toolService.ActiveTool != Tool)
        {
            PlacementArmed = false;
            placementPreviewValid = false;
            dragger.Cancel(SelectedKey.HasValue ? dragTarget : null);
            return;
        }

        if (DataRevision != builtForRevision || CurrentMapId != builtForMap)
        {
            builtForRevision = DataRevision;
            builtForMap = CurrentMapId;
            points.Clear();
            CollectPoints(points);
            if (SelectedKey.HasValue && !SelectionStillExists(SelectedKey.Value))
                SelectedKey = null; // deleted, or the camera moved to another map
        }

        if (SelectedKey != lastSelectedKey)
        {
            // clean reset for the new selection, same as the spawn dragger (the pointer is captured
            // while dragging, so the selection cannot actually change mid-drag)
            lastSelectedKey = SelectedKey;
            dragger.Cancel(null);
            toolService.GizmoMode = GizmoMode.Translate;
        }

        if (interaction.IsCaptured && !dragger.IsActive)
            return;

        // a selection without a draggable transform (e.g. a trigger whose destination is on
        // another map) must not feed the dragger a zero position
        if (SelectedKey.HasValue && TryGetSelectedTransform(out _, out _) && dragger.Update(dragTarget))
            return;

        // Back(space) too: on macOS the physical delete key reports as Backspace
        if (SelectedKey is { } keyToDelete &&
            (inputManager.Keyboard.JustPressed(Key.Delete) || inputManager.Keyboard.JustPressed(Key.Back)))
        {
            DeleteSelected(keyToDelete);
            SelectedKey = null;
            return;
        }

        if (toolService.ShowGizmoHandles && GizmoManipulateSelected())
            return;
        if (ImGuizmo.IsUsing())
            return;

        UpdatePlacementPreview();

        if (!inputManager.Mouse.HasJustClicked(MouseButton.Left) || engine.GameView.IsPointerOverOverlay)
            return;

        var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
        if (PickPoint(ray) is { } picked)
        {
            SelectedKey = picked;
            interaction.UsePointerThisFrame();
            return;
        }

        if (PlacementArmed)
        {
            var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
            if (hit.HasValue)
            {
                PlacementArmed = false;
                PlaceAt(hit.Value.Item2);
                interaction.UsePointerThisFrame();
            }
            return;
        }

        if (TryPickExtra(ray, out var extraKey))
        {
            SelectedKey = extraKey;
            interaction.UsePointerThisFrame();
            return;
        }

        SelectedKey = null;
    }

    // ghost marker under the cursor while placement is armed - you SEE where a click will land.
    // The raycast runs here; the draws happen in Render (DrawSphere is render-loop-only).
    private void UpdatePlacementPreview()
    {
        placementPreviewValid = false;
        if (!PlacementArmed || engine.GameView.IsPointerOverOverlay)
            return;

        var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
        if (!hit.HasValue)
            return;

        placementPreviewPos = hit.Value.Item2;
        placementPreviewValid = true;
    }

    public virtual void Render(float delta)
    {
        if (toolService.ActiveTool != Tool)
            return;

        var rm = engine.RenderManager;
        var cameraPos = engine.CameraManager.MainCamera.Transform.Position;
        foreach (var p in points)
        {
            bool selected = SelectedKey == p.Key;
            // markers only in the nearest surroundings (the selected one stays at any range)
            if (!selected && (p.Position - cameraPos).LengthSquared() > MarkerMaxDistance * MarkerMaxDistance)
                continue;
            var color = selected ? new Vector4(Math.Min(p.Color.X * 1.3f, 1f), Math.Min(p.Color.Y * 1.3f, 1f), Math.Min(p.Color.Z * 1.3f, 1f), 1f) : p.Color;

            rm.DrawLine(p.Position, p.Position + Vectors.Up * BeamHeight, color);
            rm.DrawSphere(p.Position + Vectors.Up * 0.4f, selected ? 0.85f : 0.5f, color);

            // orientation arrow on the ground
            var dir = new Vector3(MathF.Cos(p.Orientation), MathF.Sin(p.Orientation), 0);
            var side = new Vector3(-dir.Y, dir.X, 0);
            var from = p.Position + Vectors.Up * 0.4f;
            var tip = from + dir * 2.2f;
            rm.DrawLine(from, tip, color);
            rm.DrawLine(tip, tip - dir * 0.6f + side * 0.4f, color);
            rm.DrawLine(tip, tip - dir * 0.6f - side * 0.4f, color);
        }

        if (PlacementArmed && placementPreviewValid)
        {
            var ghost = new Vector4(0.35f, 1.0f, 0.45f, 0.9f);
            rm.DrawSphere(placementPreviewPos + Vectors.Up * 0.4f, 0.6f, ghost);
            rm.DrawLine(placementPreviewPos, placementPreviewPos + Vectors.Up * BeamHeight, ghost);
        }
    }

    // label chips only for the nearest surroundings - a whole continent of labels is just noise
    private const float MarkerMaxDistance = 600f;

    public virtual void RenderGUI()
    {
        if (toolService.ActiveTool != Tool || points.Count == 0)
            return;

        dragger.DrawGuides(); // axis-lock guide line while a grab is constrained

        if (!ImGui.Begin("3D"u8))
        {
            ImGui.End();
            return;
        }

        var dl = ImGui.GetWindowDrawList();
        var view = engine.GameView.ViewRect;
        var camera = engine.CameraManager.MainCamera;
        var viewProj = camera.ViewMatrix * camera.ProjectionMatrix;
        var cameraPos = camera.Transform.Position;

        foreach (var p in points)
        {
            bool emphasized = SelectedKey == p.Key;
            // the selected point keeps its label at any range - you're working with it
            if (!emphasized && (p.Position - cameraPos).LengthSquared() > MarkerMaxDistance * MarkerMaxDistance)
                continue;
            var top = p.Position + Vectors.Up * BeamHeight;
            var clip = Vector4.Transform(new Vector4(top.X, top.Y, top.Z, 1f), viewProj);
            if (clip.W <= 0)
                continue;
            float nx = (clip.X / clip.W + 1f) * 0.5f;
            float ny = 1f - (clip.Y / clip.W + 1f) * 0.5f;
            if (nx < 0 || nx > 1 || ny < 0 || ny > 1)
                continue;

            var textSize = ImGui.CalcTextSize(p.Label);
            var pad = new System.Numerics.Vector2(5, 2);
            var anchor = new System.Numerics.Vector2(view.X + nx * view.Width, view.Y + ny * view.Height);
            var min = anchor + new System.Numerics.Vector2(-textSize.X * 0.5f - pad.X, -textSize.Y - pad.Y * 2);
            var max = min + textSize + pad * 2;

            dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, emphasized ? 0.95f : 0.70f), 4f);
            if (emphasized)
                dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text, 0.7f), 4f);
            dl.AddText(min + pad, ImGui.GetColorU32(ImGuiCol.Text, emphasized ? 1f : 0.85f), p.Label);
        }

        ImGui.End();
    }

    private uint? PickPoint(Ray ray)
    {
        uint? best = null;
        float bestScore = float.MaxValue;

        foreach (var p in points)
        {
            // aim at the middle of the beam - clicking anywhere along it should hit
            var target = p.Position + Vectors.Up * (BeamHeight * 0.35f);
            var toP = target - ray.Position;
            float t = Vector3.Dot(toP, ray.Direction);
            if (t <= 0)
                continue;
            var closest = ray.Position + ray.Direction * t;
            float dist = (target - closest).Length();
            float threshold = MathF.Max(PickAngularThreshold * t, BeamHeight * 0.45f);
            if (dist < threshold && dist < bestScore)
            {
                bestScore = dist;
                best = p.Key;
            }
        }

        return best;
    }

    private static unsafe Span<float> AsSpan(ref Matrix4x4 matrix) =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4x4, float>(ref matrix), 16);

    private unsafe bool GizmoManipulateSelected()
    {
        if (SelectedKey == null || !TryGetSelectedTransform(out var pos, out var orientation))
            return false;

        var view = engine.CameraManager.MainCamera.ViewMatrix;
        var proj = engine.CameraManager.MainCamera.ProjectionMatrix;
        var local = Matrix4x4.CreateTranslation(pos.X, pos.Y, pos.Z);

        // translate only: yaw rotation goes through the shared TransformDragger (R / dropdown),
        // exactly like creature spawns - the rotate ball would be misleading for a yaw-only point
        ImGuizmo.SetID(GizmoId);
        try
        {
            fixed (float* viewPtr = AsSpan(ref view))
            fixed (float* projPtr = AsSpan(ref proj))
            fixed (float* localPtr = AsSpan(ref local))
            {
                if (ImGuizmo.Manipulate(viewPtr, projPtr, ImGuizmoOperation.Translate, ImGuizmoMode.World, localPtr))
                {
                    var span = AsSpan(ref local);
                    SetSelectedTransform(new Vector3(span[12], span[13], span[14]), orientation);
                    return true;
                }
            }
        }
        finally
        {
            ImGuizmo.SetID(0);
        }

        return false;
    }

    private const float GroundSnapProbe = 60f;

    /// <summary>Nearest static surface to the current height (bridges/caves keep working).</summary>
    public Vector3 SnapToGround(Vector3 pos)
    {
        var hits = raycastSystem.RaycastAll(new Ray(pos.WithZ(pos.Z + GroundSnapProbe), Vectors.Down),
            pos.WithZ(pos.Z - GroundSnapProbe), Collisions.COLLISION_MASK_STATIC);
        if (hits == null || hits.Count == 0)
            return pos;

        float bestZ = pos.Z;
        float bestDelta = float.MaxValue;
        foreach (var (_, hitPos) in hits)
        {
            float delta = MathF.Abs(hitPos.Z - pos.Z);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestZ = hitPos.Z;
            }
        }
        return pos.WithZ(bestZ);
    }
}
