using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shaolinq;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]
    [DataAccessObject(Name = "smart_scripts")]
    public abstract class MySqlSmartScriptLine : DataAccessObject, ISmartScriptLine
    {
        [PersistedMember(Name = "entryorguid")]
        [PrimaryKey(IsPrimaryKey=true)]
        public abstract int EntryOrGuid { get; set; }
        
        [PersistedMember(Name = "source_type")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ScriptSourceType { get; set; }

        [PersistedMember(Name = "id")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int Id { get; set; }

        [PersistedMember(Name = "link")]
        public abstract int Link { get; set; }

        [PersistedMember(Name = "event_type")]
        public abstract int EventType { get; set; }

        [PersistedMember(Name = "event_phase_mask")]
        public abstract int EventPhaseMask { get; set; }

        [PersistedMember(Name = "event_chance")]
        public abstract int EventChance { get; set; }

        [PersistedMember(Name = "event_flags")]
        public abstract int EventFlags { get; set; }

        [PersistedMember(Name = "event_param1")]
        public abstract int EventParam1 { get; set; }

        [PersistedMember(Name = "event_param2")]
        public abstract int EventParam2 { get; set; }

        [PersistedMember(Name = "event_param3")]
        public abstract int EventParam3 { get; set; }

        [PersistedMember(Name = "event_param4")]
        public abstract int EventParam4 { get; set; }
        
        [PersistedMember(Name = "action_type")]
        public abstract int ActionType { get; set; }

        [PersistedMember(Name = "action_param1")]
        public abstract int ActionParam1 { get; set; }

        [PersistedMember(Name = "action_param2")]
        public abstract int ActionParam2 { get; set; }

        [PersistedMember(Name = "action_param3")]
        public abstract int ActionParam3 { get; set; }

        [PersistedMember(Name = "action_param4")]
        public abstract int ActionParam4 { get; set; }

        [PersistedMember(Name = "action_param5")]
        public abstract int ActionParam5 { get; set; }

        [PersistedMember(Name = "action_param6")]
        public abstract int ActionParam6 { get; set; }

        [PersistedMember(Name = "target_type")]
        public abstract int TargetType { get; set; }

        [PersistedMember(Name = "target_param1")]
        public abstract int TargetParam1 { get; set; }

        [PersistedMember(Name = "target_param2")]
        public abstract int TargetParam2 { get; set; }

        [PersistedMember(Name = "target_param3")]
        public abstract int TargetParam3 { get; set; }
        
        [PersistedMember(Name = "target_x")]
        public abstract float TargetX { get; set; }

        [PersistedMember(Name = "target_y")]
        public abstract float TargetY { get; set; }

        [PersistedMember(Name = "target_z")]
        public abstract float TargetZ { get; set; }

        [PersistedMember(Name = "target_o")]
        public abstract float TargetO { get; set; }

        [PersistedMember]
        public abstract string Comment { get; set; }

        // those are not used on TC
        public int SourceType { get => 0; set => throw new NotImplementedException(); }

        public int SourceParam1 { get => 0; set => throw new NotImplementedException(); }

        public int SourceParam2 { get => 0; set => throw new NotImplementedException(); }

        public int SourceParam3 { get => 0; set => throw new NotImplementedException(); }

        public int SourceConditionId { get => 0; set => throw new NotImplementedException(); }

        public int TargetConditionId { get => 0; set => throw new NotImplementedException(); }
        
        public int EventCooldownMin { get => 0; set => throw new NotImplementedException(); }

        public int EventCooldownMax { get => 0; set => throw new NotImplementedException(); }
    }
}