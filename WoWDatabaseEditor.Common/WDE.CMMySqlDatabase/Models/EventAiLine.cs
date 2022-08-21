using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_ai_scripts")]
public class EventAiLine : IEventAiLine
{
    [PrimaryKey]
    [Identity]
    [Column("id")]
    public uint Id { get; set; }
    
    [Column("creature_id")] public int CreatureIdOrGuid  { get; set; }
    [Column("event_type")] public byte EventType { get; set; }
    [Column("event_inverse_phase_mask")] public int EventInversePhaseMask { get; set; }
    [Column("event_chance")] public uint EventChance { get; set; }
    [Column("event_flags")] public uint EventFlags { get; set; }
    [Column("event_param1")] public int EventParam1 { get; set; }
    [Column("event_param2")] public int EventParam2 { get; set; }
    [Column("event_param3")] public int EventParam3 { get; set; }
    [Column("event_param4")] public int EventParam4 { get; set; }
    [Column("event_param5")] public int EventParam5 { get; set; }
    [Column("event_param6")] public int EventParam6 { get; set; }
    [Column("action1_type")] public uint Action1Type { get; set; }
    [Column("action1_param1")] public int Action1Param1 { get; set; }
    [Column("action1_param2")] public int Action1Param2 { get; set; }
    [Column("action1_param3")] public int Action1Param3 { get; set; }
    [Column("action2_type")] public uint Action2Type { get; set; }
    [Column("action2_param1")] public int Action2Param1 { get; set; }
    [Column("action2_param2")] public int Action2Param2 { get; set; }
    [Column("action2_param3")] public int Action2Param3 { get; set; }
    [Column("action3_type")] public uint Action3Type { get; set; }
    [Column("action3_param1")] public int Action3Param1 { get; set; }
    [Column("action3_param2")] public int Action3Param2 { get; set; }
    [Column("action3_param3")] public int Action3Param3 { get; set; }
    [Column("comment")] public string Comment { get; set; } = "";
}