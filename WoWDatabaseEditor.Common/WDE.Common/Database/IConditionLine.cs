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

        long ConditionTarget { get; set; }

        long ConditionValue1 { get; set; }

        long ConditionValue2 { get; set; }

        long ConditionValue3 { get; set; }

        long NegativeCondition { get; set; }

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

        public long ConditionTarget { get; set; }

        public long ConditionValue1 { get; set; }

        public long ConditionValue2 { get; set; }

        public long ConditionValue3 { get; set; }

        public long NegativeCondition { get; set; }

        public string Comment { get; set; }
    }
}