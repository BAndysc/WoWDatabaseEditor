using System;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

public enum SpawnEditorTool
{
    Select,
    Waypoint,
    Formation,
    SpawnGroup,
    Pool,
    Graveyard,
    SpellTarget,
    CreatureLink,
    AreaTrigger,
}

/// <summary>Which transform the manipulation gizmo edits. Orthogonal to <see cref="SpawnEditorTool"/> -
/// it's a shared state the tools consult, not a tool itself. R sets Rotate; the toolbar dropdown
/// sets any. Tools that don't support the current mode simply can't be switched into it (see
/// <see cref="SpawnEditorToolGizmoSupport"/>).</summary>
public enum GizmoMode
{
    Translate,
    Rotate,
    Scale,
}

public static class SpawnEditorToolGizmoSupport
{
    public static bool Supports(this SpawnEditorTool tool, GizmoMode mode) => tool switch
    {
        // no per-spawn scale column to persist, so no Scale for spawns (yet)
        SpawnEditorTool.Select => mode != GizmoMode.Scale,
        // a path point has no orientation
        SpawnEditorTool.Waypoint => mode == GizmoMode.Translate,
        // world points (graveyards / spell targets / teleport destinations) have a position and an orientation
        SpawnEditorTool.Graveyard => mode != GizmoMode.Scale,
        SpawnEditorTool.SpellTarget => mode != GizmoMode.Scale,
        SpawnEditorTool.AreaTrigger => mode != GizmoMode.Scale,
        // formations / spawn groups have no transform gizmo at all
        _ => false,
    };

    public static bool SupportsAnyGizmo(this SpawnEditorTool tool) =>
        tool is SpawnEditorTool.Select or SpawnEditorTool.Waypoint
            or SpawnEditorTool.Graveyard or SpawnEditorTool.SpellTarget
            or SpawnEditorTool.AreaTrigger;
}

/// <summary>
/// The single active world-editing tool. Tools are mutually exclusive - each editor module only
/// interacts with the world while its tool is active. Set from the game-view toolbar.
/// Also holds the shared <see cref="GizmoMode"/> (clamped to what the active tool supports).
/// </summary>
[UniqueProvider]
public interface ISpawnEditorToolService
{
    SpawnEditorTool ActiveTool { get; set; }
    GizmoMode GizmoMode { get; set; }
    event Action? Changed;

    /// <summary>Bumped whenever a <see cref="GizmoMode"/> set was refused because the active tool
    /// doesn't support it (e.g. R in the Waypoint tool). Poll it to flash the user a reason -
    /// silently ignoring the keypress reads as a broken key. <see cref="LastRefusedMode"/> says what
    /// was asked for.</summary>
    int ModeRefusalCounter { get; }

    GizmoMode LastRefusedMode { get; }

    /// <summary>Whether the on-screen ImGuizmo handles are drawn at all. Keyboard-first users (G/R)
    /// can turn them off; the G/R grab keeps working. Session-only, defaults to on.</summary>
    bool ShowGizmoHandles { get; set; }
}

public class SpawnEditorToolService : ISpawnEditorToolService
{
    private SpawnEditorTool activeTool = SpawnEditorTool.Select;
    private GizmoMode gizmoMode = GizmoMode.Translate;

    public SpawnEditorTool ActiveTool
    {
        get => activeTool;
        set
        {
            if (activeTool == value)
                return;
            activeTool = value;
            if (!activeTool.Supports(gizmoMode))
                gizmoMode = GizmoMode.Translate;
            Changed?.Invoke();
        }
    }

    public GizmoMode GizmoMode
    {
        get => gizmoMode;
        set
        {
            if (gizmoMode == value)
                return;
            if (value != GizmoMode.Translate && !activeTool.Supports(value))
            {
                LastRefusedMode = value;
                ModeRefusalCounter++;
                return;
            }
            gizmoMode = value;
            Changed?.Invoke();
        }
    }

    public int ModeRefusalCounter { get; private set; }

    public GizmoMode LastRefusedMode { get; private set; }

    public bool ShowGizmoHandles { get; set; } = true;

    public event Action? Changed;
}
