using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "waypoint_data")]
public class WaypointData : IWaypointData, ISmartScriptWaypoint, IScriptWaypoint
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint PathId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "point")]
    public uint PointId { get; set; }

    public uint WaitTime => Delay;

    [Column(Name = "position_x")]
    public float X { get; set; }
    
    [Column(Name = "position_y")]
    public float Y { get; set; }
    
    [Column(Name = "position_z")]
    public float Z { get; set; }
    
    [Column(Name = "orientation")]
    public float? Orientation { get; set; }

    public virtual float? Velocity { get; set; }

    [Column(Name = "delay")]
    public uint Delay { get; set; }

    public virtual bool? SmoothTransition { get; set; }

    public string? Comment { get; }

    [Column(Name = "move_type")]
    public int MoveType { get; set; }
    
    [Column(Name = "action")]
    public int Action { get; set; }
    
    [Column(Name = "action_chance")]
    public byte ActionChance { get; set; }

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
            SmoothTransition = SmoothTransition,
            MoveType = MoveType,
            Action = Action,
            ActionChance = ActionChance
        };
    }
}

public class WaypointDataCata : WaypointData
{
    [Column(Name = "velocity")]
    public override float? Velocity { get; set; }
    
    [Column(Name = "smoothTransition")]
    public override bool? SmoothTransition { get; set; }
}

[Table(Name = "waypoints")]
public class SmartScriptWaypoint : ISmartScriptWaypoint
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint PathId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "pointid")]
    public uint PointId { get; set; }
    
    [Column(Name = "position_x")]
    public float X { get; set; }
    
    [Column(Name = "position_y")]
    public float Y { get; set; }
    
    [Column(Name = "position_z")]
    public float Z { get; set; }
    
    [Column(Name = "orientation")]
    public float? Orientation { get; set; }

    public virtual float? Velocity { get; set; }

    [Column(Name = "delay")]
    public uint Delay { get; set; }

    public virtual bool? SmoothTransition { get; set; }

    [Column(Name = "point_comment")]
    public string? Comment { get; set; }

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
            SmoothTransition = SmoothTransition,
            Comment = Comment
        };
    }
}

public class SmartScriptWaypointCata : SmartScriptWaypoint
{
    [Column(Name = "velocity")]
    public override float? Velocity { get; set; }
    
    [Column(Name = "smoothTransition")]
    public override bool? SmoothTransition { get; set; }
}

[Table(Name = "script_waypoint")]
public class ScriptWaypoint : IScriptWaypoint
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint PathId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "pointid")]
    public uint PointId { get; set; }
    
    [Column(Name = "location_x")]
    public float X { get; set; }
    
    [Column(Name = "location_y")]
    public float Y { get; set; }
    
    [Column(Name = "location_z")]
    public float Z { get; set; }
    
    [Column(Name = "waittime")]
    public uint WaitTime { get; set; }
    
    [Column(Name = "point_comment")]
    public string? Comment { get; set; }
    
    public UniversalWaypoint ToUniversal()
    {
        return new UniversalWaypoint()
        {
            PathId = PathId,
            PointId = PointId,
            X = X,
            Y = Y,
            Z = Z,
            Delay = WaitTime,
            Comment = Comment
        };
    }
}