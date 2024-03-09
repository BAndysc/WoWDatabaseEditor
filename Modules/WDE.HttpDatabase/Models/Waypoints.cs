
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonWaypointData : IWaypointData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            PointId = PointId,
            PathId = PathId,
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Velocity = Velocity,
            Delay = Delay,
            SmoothTransition = SmoothTransition,
            MoveType = MoveType,
            Action = Action,
            ActionChance = ActionChance
        };
    }

    public uint PathId { get; set; }
    public uint PointId { get; set; }
    public float? Orientation { get; set; }
    public float? Velocity { get; set; }
    public uint Delay { get; set; }
    public bool? SmoothTransition { get; set; }
    public int MoveType { get; set; }
    public int Action { get; set; }
    public byte ActionChance { get; set; }
}

public class JsonSmartScriptWaypoint : ISmartScriptWaypoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            PathId = PathId,
            PointId = PointId,
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Velocity = Velocity,
            Delay = Delay,
            SmoothTransition = SmoothTransition
        };
    }

    public uint PathId { get; set; }
    public uint PointId { get; set; }
    public float? Orientation { get; set; }
    public float? Velocity { get; set; }
    public uint Delay { get; set; }
    public bool? SmoothTransition { get; set; }
    public string? Comment { get; set; }
}

public class JsonScriptWaypoint : IScriptWaypoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            PathId = PathId,
            PointId = PointId,
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Velocity = Velocity,
            Delay = Delay,
            SmoothTransition = SmoothTransition
        };
    }

    public uint PathId { get; set; }
    public uint PointId { get; set; }
    public uint WaitTime { get; set; }
    public float? Orientation { get; set; }
    public float? Velocity { get; set; }
    public uint Delay { get; set; }
    public bool? SmoothTransition { get; set; }
    public string? Comment { get; set; }
}

public class JsonMangosWaypoint : IMangosWaypoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Delay = WaitTime,
            ScriptId = ScriptId,
            Comment = Comment,
            PathId = PathId,
            PointId = PointId
        };
    }

    public uint PathId { get; set; }
    public uint PointId { get; set; }
    public float Orientation { get; set; }
    public uint WaitTime { get; set; }
    public uint ScriptId { get; set; }
    public string? Comment { get; set; }
}

public class JsonMangosCreatureMovement : IMangosCreatureMovement
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Delay = WaitTime,
            ScriptId = ScriptId,
            Comment = Comment,
            PathId = Guid,
            PointId = PointId
        };
    }

    public uint Guid { get; set; }
    public uint PointId { get; set; }
    public float Orientation { get; set; }
    public uint WaitTime { get; set; }
    public uint ScriptId { get; set; }
    public string? Comment { get; set; }
}

public class JsonMangosCreatureMovementTemplate : IMangosCreatureMovementTemplate
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            X = X,
            Y = Y,
            Z = Z,
            Orientation = Orientation,
            Delay = WaitTime,
            ScriptId = ScriptId,
            Comment = Comment,
            PathId = Entry,
            PointId = PointId
        };
    }

    public uint Entry { get; set; }
    public uint PathId { get; set; }
    public uint PointId { get; set; }
    public float Orientation { get; set; }
    public uint WaitTime { get; set; }
    public uint ScriptId { get; set; }
    public string? Comment { get; set; }
}

public class JsonMangosWaypointsPathName : IMangosWaypointsPathName
{
    public uint PathId { get; set; }
    public string Name { get; set; } = "";
}