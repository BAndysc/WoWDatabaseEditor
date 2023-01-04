using System.Diagnostics.CodeAnalysis;
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{
    [ExcludeFromCodeCoverage]

    public class JsonConditionLine : IConditionLine
    {
        
        public int SourceType { get; set; }

        
        public int SourceGroup { get; set; }

        
        public int SourceEntry { get; set; }

        
        public int SourceId { get; set; }

        
        public int ElseGroup { get; set; }

        public int ConditionIndex { get; set; }

        public int ConditionParent { get; set; }

        
        public int ConditionType { get; set; }

        
        public byte ConditionTarget { get; set; }

        
        public long ConditionValue1 { get; set; }

        
        public long ConditionValue2 { get; set; }

        
        public long ConditionValue3 { get; set; }
        
        public long ConditionValue4 { get; set; }

        public virtual string ConditionStringValue1 { get; set; } = "";

        
        public int NegativeCondition { get; set; }

        
        public string? Comment { get; set; } = "";

        public JsonConditionLine() { }
    }

    [ExcludeFromCodeCoverage]

    public class JsonConditionLineMaster : JsonConditionLine
    {
        
        public override string ConditionStringValue1
        {
            get;
            set;
        } = "";
    }
}