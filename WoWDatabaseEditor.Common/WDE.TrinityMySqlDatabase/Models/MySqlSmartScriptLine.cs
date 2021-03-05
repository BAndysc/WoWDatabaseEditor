﻿using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
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
        public long EventPhaseMask { get; set; }

        [Column(Name = "event_chance")]
        public long EventChance { get; set; }

        [Column(Name = "event_flags")]
        public long EventFlags { get; set; }

        [Column(Name = "event_param1")]
        public long EventParam1 { get; set; }

        [Column(Name = "event_param2")]
        public long EventParam2 { get; set; }

        [Column(Name = "event_param3")]
        public long EventParam3 { get; set; }

        [Column(Name = "event_param4")]
        public long EventParam4 { get; set; }

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

        public int TargetConditionId
        {
            get => 0;
            set { }
        }

        public long EventCooldownMin
        {
            get => 0;
            set { }
        }

        public long EventCooldownMax
        {
            get => 0;
            set { }
        }
    }
}