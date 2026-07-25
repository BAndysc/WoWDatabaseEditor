using WDE.Common.Database;

namespace WDE.MapSpawns.Models.Waypoints;

// Adapters that present an edited UniversalWaypoint as one of the concrete waypoint
// interfaces, so the path can be fed to QueryGenerator<T> for SQL generation. `key` is the
// path id (or creature guid) shared by every point of the path.

public sealed class WaypointDataAdapter : IWaypointData
{
    private readonly UniversalWaypoint w;
    public WaypointDataAdapter(UniversalWaypoint w, uint key) { this.w = w; PathId = key; }
    public uint PathId { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public float? Orientation => w.Orientation;
    public float? Velocity => w.Velocity;
    public uint Delay => w.Delay ?? 0;
    public bool? SmoothTransition => w.SmoothTransition;
    public int MoveType => w.MoveType ?? 0;
    public int Action => w.Action ?? 0;
    public byte ActionChance => w.ActionChance ?? 0;
    public UniversalWaypoint ToUniversal() => w with { PathId = PathId };
}

public sealed class SmartScriptWaypointAdapter : ISmartScriptWaypoint
{
    private readonly UniversalWaypoint w;
    public SmartScriptWaypointAdapter(UniversalWaypoint w, uint key) { this.w = w; PathId = key; }
    public uint PathId { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public float? Orientation => w.Orientation;
    public float? Velocity => w.Velocity;
    public uint Delay => w.Delay ?? 0;
    public bool? SmoothTransition => w.SmoothTransition;
    public string? Comment => w.Comment;
    public UniversalWaypoint ToUniversal() => w with { PathId = PathId };
}

public sealed class ScriptWaypointAdapter : IScriptWaypoint
{
    private readonly UniversalWaypoint w;
    public ScriptWaypointAdapter(UniversalWaypoint w, uint key) { this.w = w; PathId = key; }
    public uint PathId { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public uint WaitTime => w.Delay ?? 0;
    public string? Comment => w.Comment;
    public UniversalWaypoint ToUniversal() => w with { PathId = PathId };
}

public sealed class MangosWaypointAdapter : IMangosWaypoint
{
    private readonly UniversalWaypoint w;
    public MangosWaypointAdapter(UniversalWaypoint w, uint key) { this.w = w; PathId = key; }
    public uint PathId { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public float Orientation => w.Orientation ?? 0;
    public uint WaitTime => w.Delay ?? 0;
    public uint ScriptId => w.ScriptId ?? 0;
    public string? Comment => w.Comment;
    public UniversalWaypoint ToUniversal() => w with { PathId = PathId };
}

public sealed class MangosCreatureMovementAdapter : IMangosCreatureMovement
{
    private readonly UniversalWaypoint w;
    public MangosCreatureMovementAdapter(UniversalWaypoint w, uint key) { this.w = w; Guid = key; }
    public uint Guid { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public float Orientation => w.Orientation ?? 0;
    public uint WaitTime => w.Delay ?? 0;
    public uint ScriptId => w.ScriptId ?? 0;
    public string? Comment => w.Comment;
    public UniversalWaypoint ToUniversal() => w with { PathId = Guid };
}

/// <summary>An edited path name as the IMangosWaypointsPathName contract (for the
/// waypoint_path_name rewrite provider). An empty name means "remove the row".</summary>
public sealed class MangosPathNameAdapter : IMangosWaypointsPathName
{
    public MangosPathNameAdapter(uint pathId, string name)
    {
        PathId = pathId;
        Name = name;
    }

    public uint PathId { get; }
    public string Name { get; }
}

// creature_movement_template has a compound key: `key` is the Entry, `key2` is the PathId.
public sealed class MangosCreatureMovementTemplateAdapter : IMangosCreatureMovementTemplate
{
    private readonly UniversalWaypoint w;
    public MangosCreatureMovementTemplateAdapter(UniversalWaypoint w, uint key, uint key2) { this.w = w; Entry = key; PathId = key2; }
    public uint Entry { get; }
    public uint PathId { get; }
    public uint PointId => w.PointId;
    public float X => w.X;
    public float Y => w.Y;
    public float Z => w.Z;
    public float Orientation => w.Orientation ?? 0;
    public uint WaitTime => w.Delay ?? 0;
    public uint ScriptId => w.ScriptId ?? 0;
    public string? Comment => w.Comment;
    public UniversalWaypoint ToUniversal() => w with { PathId = Entry, PathId2 = PathId };
}
