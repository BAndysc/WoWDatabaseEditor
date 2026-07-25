using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

// All dbscripts_on_* tables share the identical column schema, so a single mapped
// class is reused for all 10 of them via a per-query .TableName(...) override.
[Table(Name = "dbscripts_on_relay")]
public class DbScriptLine : IDbScriptLine
{
    [Column("id")] public uint Id { get; set; }
    [Column("delay")] public uint Delay { get; set; }
    [Column("priority")] public uint Priority { get; set; }
    [Column("command")] public uint Command { get; set; }
    [Column("datalong")] public uint DataLong { get; set; }
    [Column("datalong2")] public uint DataLong2 { get; set; }
    [Column("datalong3")] public uint DataLong3 { get; set; }
    [Column("buddy_entry")] public uint BuddyEntry { get; set; }
    [Column("search_radius")] public uint SearchRadius { get; set; }
    [Column("data_flags")] public uint DataFlags { get; set; }
    [Column("dataint")] public int DataInt { get; set; }
    [Column("dataint2")] public int DataInt2 { get; set; }
    [Column("dataint3")] public int DataInt3 { get; set; }
    [Column("dataint4")] public int DataInt4 { get; set; }
    [Column("datafloat")] public float DataFloat { get; set; }
    [Column("x")] public float X { get; set; }
    [Column("y")] public float Y { get; set; }
    [Column("z")] public float Z { get; set; }
    [Column("o")] public float O { get; set; }
    [Column("speed")] public float Speed { get; set; }
    [Column("condition_id")] public uint ConditionId { get; set; }
    [Column("comments")] public string? Comments { get; set; }
}
