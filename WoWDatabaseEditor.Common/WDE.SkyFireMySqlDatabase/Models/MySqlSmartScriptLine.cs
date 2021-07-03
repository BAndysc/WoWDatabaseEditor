using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]
    [Table(Name = "smart_scripts")]
    public class MySqlSmartScriptLine : ISmartScriptLine
    {
        [Column(Name = "entryorguid")]
        [PrimaryKey]
        public int EntryOrGuid { get; set; }

        [Column(Name = "source_type")]
        [PrimaryKey]
        public int ScriptSourceType { get; set; }

        [Column(Name = "id")]
        [PrimaryKey]
        public int Id { get; set; }

        [Column(Name = "link")]
        public int Link { get; set; }

        [Column(Name = "event_type")]
        public int EventType { get; set; }

        [Column(Name = "event_phase_mask")]
        public int EventPhaseMask { get; set; }

        [Column(Name = "event_chance")]
        public int EventChance { get; set; }

        [Column(Name = "event_flags")]
        public int EventFlags { get; set; }

        [Column(Name = "event_param1")]
        public int EventParam1 { get; set; }

        [Column(Name = "event_param2")]
        public int EventParam2 { get; set; }

        [Column(Name = "event_param3")]
        public int EventParam3 { get; set; }

        [Column(Name = "event_param4")]
        public int EventParam4 { get; set; }

        [Column(Name = "action_type")]
        public int ActionType { get; set; }

        [Column(Name = "action_param1")]
        public int ActionParam1 { get; set; }

        [Column(Name = "action_param2")]
        public int ActionParam2 { get; set; }

        [Column(Name = "action_param3")]
        public int ActionParam3 { get; set; }

        [Column(Name = "action_param4")]
        public int ActionParam4 { get; set; }

        [Column(Name = "action_param5")]
        public int ActionParam5 { get; set; }

        [Column(Name = "action_param6")]
        public int ActionParam6 { get; set; }

        [Column(Name = "target_type")]
        public int TargetType { get; set; }

        [Column(Name = "target_param1")]
        public int TargetParam1 { get; set; }

        [Column(Name = "target_param2")]
        public int TargetParam2 { get; set; }

        [Column(Name = "target_param3")]
        public int TargetParam3 { get; set; }

        [Column(Name = "target_x")]
        public float TargetX { get; set; }

        [Column(Name = "target_y")]
        public float TargetY { get; set; }

        [Column(Name = "target_z")]
        public float TargetZ { get; set; }

        [Column(Name = "target_o")]
        public float TargetO { get; set; }

        [Column]
        public string Comment { get; set; } = "";

        public MySqlSmartScriptLine() { }

        public MySqlSmartScriptLine(ISmartScriptLine line)
        {
            EntryOrGuid = line.EntryOrGuid;
            ScriptSourceType = line.ScriptSourceType;
            Id = line.Id;
            Link = line.Link;
            EventType = line.EventType;
            EventPhaseMask = line.EventPhaseMask;
            EventChance = line.EventChance;
            EventFlags = line.EventFlags;
            EventParam1 = line.EventParam1;
            EventParam2 = line.EventParam2;
            EventParam3 = line.EventParam3;
            EventParam4 = line.EventParam4;
            EventCooldownMin = line.EventCooldownMin;
            EventCooldownMax = line.EventCooldownMax;
            ActionType = line.ActionType;
            ActionParam1 = line.ActionParam1;
            ActionParam2 = line.ActionParam2;
            ActionParam3 = line.ActionParam3;
            ActionParam4 = line.ActionParam4;
            ActionParam5 = line.ActionParam5;
            ActionParam6 = line.ActionParam6;
            TargetType = line.TargetType;
            TargetParam1 = line.TargetParam1;
            TargetParam2 = line.TargetParam2;
            TargetParam3 = line.TargetParam3;
            TargetX = line.TargetX;
            TargetY = line.TargetY;
            TargetZ = line.TargetZ;
            TargetO = line.TargetO;
            Comment = line.Comment;
        }

        // those are not used on TC
        public int LineId
        {
            get => 0;
            set { }
        }
        
        public int SourceType
        {
            get => 0;
            set { }
        }

        public int SourceParam1
        {
            get => 0;
            set { }
        }

        public int SourceParam2
        {
            get => 0;
            set { }
        }

        public int SourceParam3
        {
            get => 0;
            set { }
        }

        public int SourceConditionId
        {
            get => 0;
            set { }
        }

        public int TargetConditionId
        {
            get => 0;
            set { }
        }

        public int EventCooldownMin
        {
            get => 0;
            set { }
        }

        public int EventCooldownMax
        {
            get => 0;
            set { }
        }
    }
}