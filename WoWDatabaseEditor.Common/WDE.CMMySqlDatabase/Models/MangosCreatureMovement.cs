using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_movement")]
public class MangosCreatureMovement : IMangosCreatureMovement
{
    [Column(Name = "PositionX")]
    public float X { get; set; }
    
    [Column(Name = "PositionY")]
    public float Y { get; set; }
    
    [Column(Name = "PositionZ")]
    public float Z { get; set; }

    [PrimaryKey]
    [Column(Name = "Id")]
    public uint Guid { get; set; }
    
    [PrimaryKey]
    [Column(Name = "Point")]
    public uint PointId { get; set; }

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
            PathId = Guid,
            PointId = PointId,
            X = X,
            Y = Y,
            Z = Z,
            Delay = WaitTime,
            Orientation = Orientation,
            ScriptId = ScriptId,
            Comment = Comment
        };
    }
}


[Table(Name = "creature_movement_template")]
public class MangosCreatureMovementTemplate : IMangosCreatureMovementTemplate
{
    [Column(Name = "PositionX")]
    public float X { get; set; }
    
    [Column(Name = "PositionY")]
    public float Y { get; set; }
    
    [Column(Name = "PositionZ")]
    public float Z { get; set; }

    [PrimaryKey]
    [Column(Name = "Entry")]
    public uint Entry { get; set; }
    
    [PrimaryKey]
    [Column(Name = "PathId")]
    public uint PathId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "Point")]
    public uint PointId { get; set; }

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
            PathId = Entry,
            PathId2 = PathId,
            PointId = PointId,
            X = X,
            Y = Y,
            Z = Z,
            Delay = WaitTime,
            Orientation = Orientation,
            ScriptId = ScriptId,
            Comment = Comment
        };
    }
}