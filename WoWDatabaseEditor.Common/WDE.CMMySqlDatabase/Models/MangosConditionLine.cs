using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "conditions")]
public class MangosConditionLine : IMangosConditionLine
{
    [Column("condition_entry")] public uint ConditionEntry { get; set; }
    [Column("type")] public int ConditionType { get; set; }
    [Column("value1")] public uint Value1 { get; set; }
    [Column("value2")] public uint Value2 { get; set; }
    [Column("value3")] public uint Value3 { get; set; }
    [Column("value4")] public uint Value4 { get; set; }
    [Column("flags")] public uint Flags { get; set; }
    [Column("comments")] public string? Comments { get; set; }
}
