namespace WDE.Common.Database
{
    public interface IConditionLine
    {
        int SourceType { get; set; }

        int SourceGroup { get; set; }

        int SourceEntry { get; set; }

        int SourceId { get; set; }

        int ElseGroup { get; set; }

        int ConditionType { get; set; }

        byte ConditionTarget { get; set; }

        uint ConditionValue1 { get; set; }

        uint ConditionValue2 { get; set; }

        uint ConditionValue3 { get; set; }

        uint NegativeCondition { get; set; }

        string Comment { get; set; }
    }
    
    public class AbstractConditionLine : IConditionLine
    {
        public int SourceType { get; set; }

        public int SourceGroup { get; set; }

        public int SourceEntry { get; set; }

        public int SourceId { get; set; }

        public int ElseGroup { get; set; }

        public int ConditionType { get; set; }

        public byte ConditionTarget { get; set; }

        public uint ConditionValue1 { get; set; }

        public uint ConditionValue2 { get; set; }

        public uint ConditionValue3 { get; set; }

        public uint NegativeCondition { get; set; }

        public string Comment { get; set; }
    }
}