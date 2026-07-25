using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;

namespace WDE.MapSpawns.Rendering;

/// <summary>Axis constraint of an in-flight grab (Blender's G then X/Y/Z).</summary>
public enum DragAxis
{
    None,
    X,
    Y,
    Z,
}

/// <summary>What the <see cref="TransformDragger"/> moves: anything with a world position and
/// optionally a yaw. Implementations write through to the live object/model on every set.</summary>
public interface IDragTarget
{
    Vector3 Position { get; set; }

    /// <summary>false = the target has no yaw (waypoint points) - R/rotate is inert for it and the
    /// caller's own gizmo handles any other rotation concept (gameobject quaternions).</summary>
    bool HasOrientation { get; }

    float Orientation { get; set; }
}

/// <summary>
/// The one grab/rotate state machine shared by every editor - the spawn-dragger semantics for
/// anything with a position (and optionally a yaw):
///  - G grabs: the target follows the cursor on a horizontal plane (keeping its offset under the
///    cursor); G again snaps it to the nearest static surface and rebases the plane there;
///    holding Alt raises/lowers instead (camera-facing vertical plane); Ctrl snaps to a 0.5 grid;
///    click drops (fires <see cref="Committed"/>), Escape cancels and restores.
///  - X / Y / Z during a grab lock the move to that world axis (same key again unlocks) - a
///    colored guide line marks the locked axis. Enter (or click) drops as usual.
///  - Typing digits during a grab/rotation enters an exact value: yards along the locked axis
///    (no axis yet = Z, raise/lower being the common precise move) or degrees while rotating.
///    Backspace edits, Enter/click applies, Escape cancels everything.
///  - Holding Shift slows the mouse-driven motion to a crawl for fine placement.
///  - R (or the toolbar dropdown) enters yaw rotation: the target turns to face wherever the
///    cursor points on its horizontal plane; Ctrl snaps to 15° steps; click drops,
///    Escape restores; the shared gizmo mode falls back to Translate afterwards. Targets without
///    a yaw ignore rotation entirely (and tools whose gizmo-mode support excludes Rotate never
///    even see the mode change).
/// Owns the pointer capture while active. The caller drives it once per frame with the CURRENT
/// selection and must call <see cref="Cancel"/> when the selection or the active tool changes.
/// </summary>
public sealed class TransformDragger
{
    private const float GroundRayHeight = 4000f;
    private const float RotateSnapStep = MathF.PI / 12f; // Ctrl while rotating: 15°
    private const float GridSnapStep = 0.5f;             // Ctrl while grabbing

    private readonly Engine engine;
    private readonly IInputManager inputManager;
    private readonly RaycastSystem raycastSystem;
    private readonly IWorldInteractionService interaction;
    private readonly ISpawnEditorToolService toolService;

    private CaptureLease lease;
    // the gizmo mode lives in ISpawnEditorToolService (the toolbar dropdown shows/sets it); this
    // mirrors the last value we acted on, so an external change (the dropdown) is detected in Update
    private GizmoMode lastSeenMode = GizmoMode.Translate;

    private bool planeDragging;
    private float planeZ;
    private Vector3 planeDragOffset;
    private Vector3 planeDragOriginalPos;
    private bool verticalDragging; // Alt held: the grab moves along Z instead of the horizontal plane
    private float verticalZOffset;

    private bool rotating;
    private float rotateStartOrientation;

    // Blender-style extras on top of the mouse drive: a world-axis lock, a typed exact value and
    // a Shift precision brake. All reset when the drag ends however it ends.
    private DragAxis axisLock;
    private string numericEntry = "";

    /// <summary>Fired when a drag/rotation finishes with a drop click (never on cancel) - the
    /// owner persists the target's now-final transform.</summary>
    public event Action? Committed;

    public bool IsActive => planeDragging || rotating;

    /// <summary>World axis the in-flight grab is locked to (None outside a grab too).</summary>
    public DragAxis AxisLock => planeDragging ? axisLock : DragAxis.None;

    /// <summary>Where the in-flight grab started - the anchor the axis guide line runs through.</summary>
    public Vector3 GrabOrigin => planeDragOriginalPos;

    // hint rebuilt only when the state it narrates changes (the hint bar reads it every frame)
    private string? cachedHint;
    private (bool rotating, bool vertical, DragAxis axis, string numeric) hintKey = (false, false, DragAxis.None, "");

    /// <summary>Hint-bar line while a drag/rotation is in flight (null otherwise) - the state-
    /// sensitive replacement for the owning tool's idle hints.</summary>
    public string? ActiveHint
    {
        get
        {
            if (!IsActive)
                return null;
            var key = (rotating, verticalDragging, axisLock, numericEntry);
            if (key != hintKey)
            {
                hintKey = key;
                cachedHint = BuildHint();
            }
            return cachedHint;
        }
    }

    private string BuildHint()
    {
        if (numericEntry.Length > 0)
            return rotating
                ? $"Rotate by: {numericEntry}° · Enter/click: apply · Backspace: edit · Esc: cancel"
                : $"Move {AxisName(axisLock == DragAxis.None ? DragAxis.Z : axisLock)}: {numericEntry} yd · Enter/click: apply · Backspace: edit · Esc: cancel";
        if (rotating)
            return "Rotating · faces the cursor · click: drop · Esc: cancel · Ctrl: 15° steps · Shift: precise · type degrees for an exact turn";
        if (axisLock != DragAxis.None)
            return $"Grabbing along {AxisName(axisLock)} · click: drop · {AxisName(axisLock)}: unlock · type a distance for an exact move · Ctrl: grid snap · Shift: precise";
        return verticalDragging
            ? "Raising/lowering · click: drop · Esc: cancel · release Alt: move horizontally · Ctrl: grid snap · Shift: precise"
            : "Grabbing · click: drop · Esc: cancel · G: snap to ground · X/Y/Z: lock to an axis · hold Alt: raise/lower · Ctrl: grid snap · Shift: precise";
    }

    private static string AxisName(DragAxis axis) => axis switch
    {
        DragAxis.X => "X",
        DragAxis.Y => "Y",
        DragAxis.Z => "Z",
        _ => "",
    };

    public TransformDragger(Engine engine,
        IInputManager inputManager,
        RaycastSystem raycastSystem,
        IWorldInteractionService interaction,
        ISpawnEditorToolService toolService)
    {
        this.engine = engine;
        this.inputManager = inputManager;
        this.raycastSystem = raycastSystem;
        this.interaction = interaction;
        this.toolService = toolService;
    }

    /// <summary>Cancels any in-flight drag/rotation, restoring the original transform onto
    /// <paramref name="target"/> (pass null when the target is gone - nothing to restore onto).
    /// Also resyncs the gizmo-mode mirror, so it is safe to call every inactive frame.</summary>
    public void Cancel(IDragTarget? target)
    {
        if (planeDragging && target != null)
            target.Position = planeDragOriginalPos;
        if (rotating && target is { HasOrientation: true })
            target.Orientation = rotateStartOrientation;
        planeDragging = false;
        rotating = false;
        axisLock = DragAxis.None;
        numericEntry = "";
        lastSeenMode = toolService.GizmoMode;
        ReleaseCapture();
    }

    /// <summary>Per-frame driver for the current selection. Returns true while a drag/rotation is
    /// in progress - the caller must then skip its own gizmo/click handling for the frame.</summary>
    public bool Update(IDragTarget target)
    {
        // R = rotate mode; the toolbar dropdown sets the same shared state, so both paths funnel
        // into the change detection below (the tool service clamps unsupported modes away)
        if (!rotating && inputManager.Keyboard.JustPressed(Key.R))
        {
            planeDragging = false;
            ReleaseCapture();
            toolService.GizmoMode = GizmoMode.Rotate;
        }

        if (toolService.GizmoMode != lastSeenMode)
        {
            lastSeenMode = toolService.GizmoMode;
            // entering rotate mode on a yaw-only target = the interactive mouse rotation (the full
            // rotation gizmo would be misleading); quaternion targets fall through to their gizmo
            if (lastSeenMode == GizmoMode.Rotate && !rotating && target.HasOrientation)
            {
                planeDragging = false;
                ReleaseCapture();
                rotating = true;
                rotateStartOrientation = target.Orientation;
                interaction.TryCapture(out lease);
            }
        }

        if (!rotating && inputManager.Keyboard.JustPressed(Key.G))
        {
            if (!planeDragging)
            {
                BeginPlaneDrag(target);
            }
            else
            {
                SnapToGround(target);
                // rebase the drag onto the snapped height so it keeps following the cursor smoothly
                planeZ = target.Position.Z;
                SeedPlaneOffset(target);
            }
        }

        if (rotating)
        {
            UpdateRotation(target);
            return true;
        }

        if (planeDragging)
        {
            UpdatePlaneDrag(target);
            return true;
        }

        return false;
    }

    private void SetMode(GizmoMode mode)
    {
        toolService.GizmoMode = mode;
        lastSeenMode = toolService.GizmoMode; // the service may clamp, mirror what it actually holds
    }

    private void ReleaseCapture()
    {
        if (lease.IsActive)
            lease.Release();
        lease = default;
    }

    private void BeginPlaneDrag(IDragTarget target)
    {
        ReleaseCapture();
        interaction.TryCapture(out lease);
        planeDragging = true;
        verticalDragging = false;
        axisLock = DragAxis.None;
        numericEntry = "";
        planeDragOriginalPos = target.Position;
        planeZ = planeDragOriginalPos.Z;
        SeedPlaneOffset(target);
    }

    /// <summary>A vertical plane through the target, facing the camera - intersecting the cursor
    /// ray with it turns vertical mouse motion into a Z change.</summary>
    private static TheMaths.Plane VerticalPlane(Vector3 pos, Ray ray)
    {
        var normal = new Vector3(ray.Direction.X, ray.Direction.Y, 0);
        if (normal.LengthSquared() < 1e-6f) // looking straight down - any horizontal normal works
            normal = new Vector3(0, 1, 0);
        return new TheMaths.Plane(pos, Vector3.Normalize(-normal));
    }

    // captures the gap between the target and where the cursor ray currently hits the drag plane, so
    // the target keeps its relative position under the cursor instead of snapping to it.
    private void SeedPlaneOffset(IDragTarget target)
    {
        var pos = target.Position;
        var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
        var plane = new TheMaths.Plane(new Vector3(pos.X, pos.Y, planeZ), Vectors.Up);
        planeDragOffset = plane.Intersects(ref ray, out Vector3 hit) ? pos - hit : Vector3.Zero;
    }

    private void UpdatePlaneDrag(IDragTarget target)
    {
        HandleAxisLockKeys(target);
        HandleNumericKeys();

        if (numericEntry.Length > 0 && TryParseNumericEntry(out float exact))
        {
            // typed exact move: along the locked axis (Z when none was picked - raise/lower is the
            // common precise move); the mouse is ignored until the entry is cleared
            var axis = axisLock == DragAxis.None ? DragAxis.Z : axisLock;
            target.Position = planeDragOriginalPos + AxisVector(axis) * exact;
        }
        else
        {
            UpdateMouseDrivenGrab(target);
        }

        if (inputManager.Mouse.HasJustClicked(MouseButton.Left) || inputManager.Keyboard.JustPressed(Key.Enter))
        {
            planeDragging = false;
            axisLock = DragAxis.None;
            numericEntry = "";
            ReleaseCapture();
            interaction.UsePointerThisFrame(); // the drop click must not also pick/deselect
            Committed?.Invoke();
        }
        else if (inputManager.Keyboard.JustPressed(Key.Escape))
        {
            target.Position = planeDragOriginalPos;
            planeDragging = false;
            axisLock = DragAxis.None;
            numericEntry = "";
            ReleaseCapture();
        }
    }

    private void UpdateMouseDrivenGrab(IDragTarget target)
    {
        var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);

        // Alt toggles between the horizontal plane and a camera-facing vertical plane (raise/lower);
        // both transitions reseed their offset so the target never jumps under the cursor.
        // An axis lock supersedes Alt (Z-lock IS the raise/lower).
        bool vertical = axisLock == DragAxis.Z || (axisLock == DragAxis.None && VerticalHeld);
        if (vertical != verticalDragging)
        {
            verticalDragging = vertical;
            if (vertical)
            {
                var seedPlane = VerticalPlane(target.Position, ray);
                verticalZOffset = seedPlane.Intersects(ref ray, out Vector3 seedHit)
                    ? target.Position.Z - seedHit.Z
                    : 0;
            }
            else
            {
                planeZ = target.Position.Z;
                SeedPlaneOffset(target);
            }
        }

        Vector3 newPos = target.Position;
        bool moved = false;
        if (vertical)
        {
            var vplane = VerticalPlane(target.Position, ray);
            if (vplane.Intersects(ref ray, out Vector3 vhit))
            {
                float z = vhit.Z + verticalZOffset;
                if (SnapHeld)
                    z = MathF.Round(z / GridSnapStep) * GridSnapStep;
                newPos = axisLock == DragAxis.Z
                    ? new Vector3(planeDragOriginalPos.X, planeDragOriginalPos.Y, z)
                    : target.Position.WithZ(z);
                moved = true;
            }
        }
        else
        {
            var plane = new TheMaths.Plane(new Vector3(0, 0, planeZ), Vectors.Up);
            if (plane.Intersects(ref ray, out Vector3 hit))
            {
                newPos = hit + planeDragOffset;
                newPos.Z = planeZ;
                // an X/Y lock pins the other two coordinates to where the grab started
                if (axisLock == DragAxis.X)
                    newPos = new Vector3(newPos.X, planeDragOriginalPos.Y, planeDragOriginalPos.Z);
                else if (axisLock == DragAxis.Y)
                    newPos = new Vector3(planeDragOriginalPos.X, newPos.Y, planeDragOriginalPos.Z);
                if (SnapHeld)
                {
                    newPos.X = MathF.Round(newPos.X / GridSnapStep) * GridSnapStep;
                    newPos.Y = MathF.Round(newPos.Y / GridSnapStep) * GridSnapStep;
                }
                moved = true;
            }
        }

        if (!moved)
            return;

        // Shift = precision brake: creep toward where the cursor asks instead of teleporting there
        if (PrecisionHeld && !SnapHeld)
            newPos = Vector3.Lerp(target.Position, newPos, PrecisionFactor);
        target.Position = newPos;
        if (vertical)
            planeZ = target.Position.Z;
    }

    /// <summary>X/Y/Z during a grab toggle a world-axis lock (same key unlocks, another key
    /// switches). Unlocking or switching re-anchors nothing - the constraint always measures
    /// from the grab origin, so toggling never makes the target jump.</summary>
    private void HandleAxisLockKeys(IDragTarget target)
    {
        var kb = inputManager.Keyboard;
        DragAxis? pressed = kb.JustPressed(Key.X) ? DragAxis.X
            : kb.JustPressed(Key.Y) ? DragAxis.Y
            : kb.JustPressed(Key.Z) && !SnapHeld ? DragAxis.Z // Ctrl+Z is undo, never a lock
            : null;
        if (pressed == null)
            return;

        axisLock = axisLock == pressed ? DragAxis.None : pressed.Value;
        // restart the constraint from the grab origin so the axes stay orthogonal to each other
        target.Position = planeDragOriginalPos;
        planeZ = planeDragOriginalPos.Z;
        verticalDragging = false; // re-seeded next frame by UpdateMouseDrivenGrab
        SeedPlaneOffset(target);
    }

    private void HandleNumericKeys()
    {
        var kb = inputManager.Keyboard;
        for (int d = 0; d <= 9; ++d)
        {
            if (kb.JustPressed((Key)((int)Key.D0 + d)) || kb.JustPressed((Key)((int)Key.NumPad0 + d)))
                numericEntry += (char)('0' + d);
        }
        if ((kb.JustPressed(Key.OemMinus) || kb.JustPressed(Key.Subtract)) && numericEntry.Length == 0)
            numericEntry = "-";
        if ((kb.JustPressed(Key.OemPeriod) || kb.JustPressed(Key.Decimal) || kb.JustPressed(Key.OemComma)) && !numericEntry.Contains('.'))
            numericEntry += ".";
        if (kb.JustPressed(Key.Back) && numericEntry.Length > 0)
            numericEntry = numericEntry[..^1];
    }

    private bool TryParseNumericEntry(out float value) =>
        float.TryParse(numericEntry, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);

    private static Vector3 AxisVector(DragAxis axis) => axis switch
    {
        DragAxis.X => new Vector3(1, 0, 0),
        DragAxis.Y => new Vector3(0, 1, 0),
        _ => new Vector3(0, 0, 1),
    };

    private bool SnapHeld => inputManager.Keyboard.IsDown(Key.LeftCtrl) || inputManager.Keyboard.IsDown(Key.RightCtrl)
        || inputManager.Keyboard.IsDown(Key.LWin) || inputManager.Keyboard.IsDown(Key.RWin);

    private bool VerticalHeld => inputManager.Keyboard.IsDown(Key.LeftAlt) || inputManager.Keyboard.IsDown(Key.RightAlt);

    private const float PrecisionFactor = 0.12f; // Shift held: fraction of the remaining gap applied per frame
    private bool PrecisionHeld => inputManager.Keyboard.IsDown(Key.LeftShift) || inputManager.Keyboard.IsDown(Key.RightShift);

    // absolute look-at: the target's yaw always points at the cursor's spot on the target's own
    // horizontal plane (not a relative mouse-delta accumulation)
    private void UpdateRotation(IDragTarget target)
    {
        HandleNumericKeys();

        if (numericEntry.Length > 0 && TryParseNumericEntry(out float degrees))
        {
            // typed exact rotation: degrees relative to where the rotation started
            target.Orientation = rotateStartOrientation + degrees * (MathF.PI / 180f);
        }
        else
        {
            var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
            var plane = new TheMaths.Plane(new Vector3(0, 0, target.Position.Z), Vectors.Up);
            if (plane.Intersects(ref ray, out Vector3 hit))
            {
                var toCursor = hit - target.Position;
                if (toCursor.X * toCursor.X + toCursor.Y * toCursor.Y > 1e-6f) // cursor exactly on the target: keep the last yaw
                {
                    float yaw = MathF.Atan2(toCursor.Y, toCursor.X);
                    if (SnapHeld)
                        yaw = MathF.Round(yaw / RotateSnapStep) * RotateSnapStep;
                    else if (PrecisionHeld)
                    {
                        // creep along the shortest arc instead of snapping to the cursor's bearing
                        float diff = MathF.IEEERemainder(yaw - target.Orientation, MathF.Tau);
                        yaw = target.Orientation + diff * PrecisionFactor;
                    }
                    target.Orientation = yaw;
                }
            }
        }

        if (inputManager.Mouse.HasJustClicked(MouseButton.Left) || inputManager.Keyboard.JustPressed(Key.Enter))
        {
            rotating = false;
            numericEntry = "";
            SetMode(GizmoMode.Translate);
            ReleaseCapture();
            interaction.UsePointerThisFrame();
            Committed?.Invoke();
        }
        else if (inputManager.Keyboard.JustPressed(Key.Escape))
        {
            target.Orientation = rotateStartOrientation;
            rotating = false;
            numericEntry = "";
            SetMode(GizmoMode.Translate);
            ReleaseCapture();
        }
    }

    /// <summary>Draws the axis-lock guide line (Blender-style: red X, green Y, blue Z through the
    /// grab origin) into the "3D" window. Owners call this from their RenderGUI while the dragger
    /// may be active - it is a no-op when no axis is locked.</summary>
    public void DrawGuides()
    {
        if (!planeDragging || axisLock == DragAxis.None)
            return;

        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var camera = engine.CameraManager.MainCamera;
        var viewProj = camera.ViewMatrix * camera.ProjectionMatrix;
        var view = engine.GameView.ViewRect;
        var axis = AxisVector(axisLock);
        uint color = axisLock switch
        {
            DragAxis.X => ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.95f, 0.25f, 0.25f, 0.9f)),
            DragAxis.Y => ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.30f, 0.85f, 0.30f, 0.9f)),
            _ => ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.30f, 0.55f, 1.00f, 0.9f)),
        };

        const float guideHalfLength = 200f; // yards each way - reads as infinite at editing distances
        var dl = ImGui.GetWindowDrawList();
        // draw as short world-space segments so the line follows perspective and clips cleanly
        const int segments = 64;
        System.Numerics.Vector2? prev = null;
        for (int i = 0; i <= segments; ++i)
        {
            float t = (i / (float)segments) * 2f - 1f;
            var world = planeDragOriginalPos + axis * (t * guideHalfLength);
            var clip = Vector4.Transform(new Vector4(world.X, world.Y, world.Z, 1f), viewProj);
            if (clip.W <= 0)
            {
                prev = null;
                continue;
            }
            float nx = (clip.X / clip.W + 1f) * 0.5f;
            float ny = 1f - (clip.Y / clip.W + 1f) * 0.5f;
            var pt = new System.Numerics.Vector2(view.X + nx * view.Width, view.Y + ny * view.Height);
            if (prev.HasValue)
                dl.AddLine(prev.Value, pt, color, 1.5f);
            prev = pt;
        }

        ImGui.End();
    }

    /// <summary>Nearest static surface to the current height (bridges/caves keep working).</summary>
    private void SnapToGround(IDragTarget target)
    {
        var pos = target.Position;
        var hits = raycastSystem.RaycastAll(new Ray(pos.WithZ(GroundRayHeight), Vectors.Down), pos,
            Collisions.COLLISION_MASK_STATIC);
        if (hits == null || hits.Count == 0)
            return;

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

        target.Position = pos.WithZ(bestZ);
    }
}
