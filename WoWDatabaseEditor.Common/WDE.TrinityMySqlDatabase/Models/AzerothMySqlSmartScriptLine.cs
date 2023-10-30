using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.TrinityMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]
    [Table(Name = "smart_scripts")]
    public class AzerothMySqlSmartScriptLine : ISmartScriptLine
    {
        public uint? CreatureEntry => null;
        
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

        public int TimerId { get; set; }

        [Column(Name = "event_flags")]
        public int EventFlags { get; set; }

        [Column(Name = "event_param1")]
        public long EventParam1 { get; set; }

        [Column(Name = "event_param2")]
        public long EventParam2 { get; set; }

        [Column(Name = "event_param3")]
        public long EventParam3 { get; set; }

        [Column(Name = "event_param4")]
        public long EventParam4 { get; set; }

        [Column(Name = "event_param5")]
        public long EventParam5 { get; set; }

        [Column(Name = "event_param6")]
        public long EventParam6 { get; set; }

        public long EventParam7 => 0;

        public long EventParam8 => 0;

        public float EventFloatParam1 { get; set; }
        public float EventFloatParam2 { get; set; }
        public string? EventStringParam { get; set; }

        [Column(Name = "action_type")]
        public int ActionType { get; set; }

        [Column(Name = "action_param1")]
        public long ActionParam1 { get; set; }

        [Column(Name = "action_param2")]
        public long ActionParam2 { get; set; }

        [Column(Name = "action_param3")]
        public long ActionParam3 { get; set; }

        [Column(Name = "action_param4")]
        public long ActionParam4 { get; set; }

        [Column(Name = "action_param5")]
        public long ActionParam5 { get; set; }

        [Column(Name = "action_param6")]
        public long ActionParam6 { get; set; }

        public long ActionParam7 { get; set; }

        public long ActionParam8 => 0;

        public float ActionFloatParam1 { get; set; }
        public float ActionFloatParam2 { get; set; }

        public float SourceO { get; set; }

        [Column(Name = "target_type")]
        public int TargetType { get; set; }

        [Column(Name = "target_param1")]
        public long TargetParam1 { get; set; }

        [Column(Name = "target_param2")]
        public long TargetParam2 { get; set; }

        [Column(Name = "target_param3")]
        public long TargetParam3 { get; set; }

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
        
        public SmallReadOnlyList<uint>? Difficulties => null;
        
        public AzerothMySqlSmartScriptLine() { }

        public AzerothMySqlSmartScriptLine(ISmartScriptLine line)
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
            EventParam5 = line.EventParam5;
            EventParam6 = line.EventParam6;
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

        public long SourceParam1
        {
            get => 0;
            set { }
        }

        public long SourceParam2
        {
            get => 0;
            set { }
        }

        public long SourceParam3
        {
            get => 0;
            set { }
        }

        public int SourceConditionId
        {
            get => 0;
            set { }
        }

        public float SourceX { get; set; }
        public float SourceY { get; set; }
        public float SourceZ { get; set; }

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