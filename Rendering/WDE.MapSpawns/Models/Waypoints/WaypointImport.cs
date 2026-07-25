using TheMaths;
using WDE.Common.Database;

namespace WDE.MapSpawns.Models.Waypoints;

/// <summary>
/// An externally-sourced path (e.g. sniffed npc movement) to bring into the waypoint editor as a
/// regular editable path. Enqueued from any thread via
/// <see cref="IWaypointEditorService.RequestImportPath"/>; the waypoint module executes it on the
/// engine thread (which owns the editor state).
/// </summary>
public sealed class WaypointImportRequest
{
    /// <summary>Points in order. Delay/Orientation may be prefilled; PathId/PointId are reassigned.</summary>
    public required List<UniversalWaypoint> Points { get; init; }

    /// <summary>When non-zero, the path is attached to the nearest loaded creature spawn with this
    /// entry around <see cref="AttachHint"/> (if the core supports creature paths). No match or 0 =
    /// a standalone path with a freshly allocated id.</summary>
    public uint AttachEntry { get; init; }

    /// <summary>Where to look for the attach target - usually the path's first point.</summary>
    public Vector3 AttachHint { get; init; }
}

/// <summary>
/// A read-only path overlay drawn by the waypoint render stage but not part of the editor (no
/// picking, no editing) - e.g. sniffed movement shown before importing. Engine thread only.
/// </summary>
public sealed class WaypointPreviewPath
{
    public required IReadOnlyList<Vector3> Points { get; init; }
    public Vector4 Color { get; init; } = new(0.8f, 0.4f, 1.0f, 1.0f);
}
