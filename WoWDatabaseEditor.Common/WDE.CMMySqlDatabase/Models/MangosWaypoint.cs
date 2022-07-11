using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "waypoint_path")]
public class MangosWaypoint : IMangosWaypoint
{
    [PrimaryKey]
    [Column(Name = "PathId")]
    public uint PathId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "Point")]
    public uint PointId { get; set; }
    
    [Column(Name = "PositionX")]
    public float X { get; set; }
    
    [Column(Name = "PositionY")]
    public float Y { get; set; }
    
    [Column(Name = "PositionZ")]
    public float Z { get; set; }
    
    [Column(Name = "Orientation")]
    public float Orientation { get; set; }
    
    [Column(Name = "WaitTime")]
    public uint WaitTime { get; set; }
    
    [Column(Name = "ScriptId")]
    public uint ScriptId { get; set; }
    
    [Column(Name = "Comment")]
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
            Delay = WaitTime,
            ScriptId = ScriptId,
            Comment = Comment
        };
    }
}

[Table(Name = "waypoint_path_name")]
public class MangosWaypointPathName : IMangosWaypointsPathName
{
    [PrimaryKey]
    [Column(Name = "PathId")]
    public uint PathId { get; set; }

    [Column(Name = "Name")] 
    public string Name { get; set; } = "";
}