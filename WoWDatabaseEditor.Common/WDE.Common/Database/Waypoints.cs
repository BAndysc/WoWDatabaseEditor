namespace WDE.Common.Database;

public struct UniversalWaypoint
{
    public uint PathId { get; set; }
    public uint PathId2 { get; set; }
    public uint PointId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public float? Orientation { get; set; }
    public float? Velocity { get; set; }
    public uint? Delay { get; set; }
    public bool? SmoothTransition { get; set; }
    public int? MoveType { get; set; }
    public int? Action { get; set; }
    public byte? ActionChance { get; set; }
    public uint? ScriptId { get; set; }
    public string? Comment { get; set; }
}

public interface IWaypoint
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public UniversalWaypoint ToUniversal();
}

public interface IWaypointData : IWaypoint
{
    public uint PathId { get; }
    public uint PointId { get; }
    public float? Orientation { get; }
    public float? Velocity { get; }
    public uint Delay { get; }
    public bool? SmoothTransition { get; }
    public int MoveType { get; }
    public int Action { get; }
    public byte ActionChance { get; }
}

public interface ISmartScriptWaypoint : IWaypoint
{
    public uint PathId { get; }
    public uint PointId { get; }
    public float? Orientation { get; }
    public float? Velocity { get; }
    public uint Delay { get; }
    public bool? SmoothTransition { get; }
    public string? Comment { get; }
}

public interface IScriptWaypoint : IWaypoint
{
    public uint PathId { get; }
    public uint PointId { get; }
    public uint WaitTime { get; }
    public string? Comment { get; }
}

public interface IMangosWaypoint : IWaypoint
{
    public uint PathId { get; }
    public uint PointId { get; }
    public float Orientation { get; }
    public uint WaitTime { get; }
    public uint ScriptId { get; }
    public string? Comment { get; }
}

public interface IMangosCreatureMovement : IWaypoint
{
    public uint Guid { get; }
    public uint PointId { get; }
    public float Orientation { get; }
    public uint WaitTime { get; }
    public uint ScriptId { get; }
    public string? Comment { get; }
}

public interface IMangosCreatureMovementTemplate : IWaypoint
{
    public uint Entry { get; }
    public uint PathId { get; }
    public uint PointId { get; }
    public float Orientation { get; }
    public uint WaitTime { get; }
    public uint ScriptId { get; }
    public string? Comment { get; }
}

public interface IMangosWaypointsPathName
{
    public uint PathId { get; }
    public string Name { get; }
}