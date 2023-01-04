using System.Diagnostics.CodeAnalysis;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.HttpDatabase.Models
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseSqlSmartScriptLine : ISmartScriptLine
    {
        public uint? CreatureEntry => null;
        
        
        public int EntryOrGuid { get; set; }

        
        public int ScriptSourceType { get; set; }

        
        public int Id { get; set; }

        
        public int Link { get; set; }

        public abstract SmallReadOnlyList<uint>? Difficulties { get; }

        
        public int EventType { get; set; }

        
        public int EventPhaseMask { get; set; }

        
        public int EventChance { get; set; }

        public int TimerId { get; set; }

        
        public int EventFlags { get; set; }

        
        public long EventParam1 { get; set; }

        
        public long EventParam2 { get; set; }

        
        public long EventParam3 { get; set; }

        
        public long EventParam4 { get; set; }

        public long EventParam5 { get; set; }

        public long EventParam6 => 0;

        public long EventParam7 => 0;

        public long EventParam8 => 0;

        public float EventFloatParam1 { get; set; }
        public float EventFloatParam2 { get; set; }
        public string? EventStringParam { get; set; }

        
        public int ActionType { get; set; }

        
        public long ActionParam1 { get; set; }

        
        public long ActionParam2 { get; set; }

        
        public long ActionParam3 { get; set; }

        
        public long ActionParam4 { get; set; }

        
        public long ActionParam5 { get; set; }

        
        public long ActionParam6 { get; set; }

        public long ActionParam7 { get; set; }

        public long ActionParam8 => 0;

        public float ActionFloatParam1 { get; set; }
        public float ActionFloatParam2 { get; set; }

        public float SourceO { get; set; }

        
        public int TargetType { get; set; }

        
        public long TargetParam1 { get; set; }

        
        public long TargetParam2 { get; set; }

        
        public long TargetParam3 { get; set; }

        
        public float TargetX { get; set; }

        
        public float TargetY { get; set; }

        
        public float TargetZ { get; set; }

        
        public float TargetO { get; set; }

        public string Comment { get; set; } = "";

        public BaseSqlSmartScriptLine() { }

        public BaseSqlSmartScriptLine(ISmartScriptLine line)
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
    

    public class MasterJsonSmartScriptLine : BaseSqlSmartScriptLine
    {
        
        public string difficulties { get; set; } = "";

        public override SmallReadOnlyList<uint>? Difficulties
        {
            get
            {
                if (string.IsNullOrWhiteSpace(difficulties))
                    return null;

                return new SmallReadOnlyList<uint>(difficulties.Split(',')
                    .Where(x => uint.TryParse(x, out _))
                    .Select(uint.Parse));
            }
        }
    }
    

    public class JsonSmartScriptLine : BaseSqlSmartScriptLine
    {
        public override SmallReadOnlyList<uint>? Difficulties => default;
    }
}