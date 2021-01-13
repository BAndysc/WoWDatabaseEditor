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

        int ConditionTarget { get; set; }

        int ConditionValue1 { get; set; }

        int ConditionValue2 { get; set; }

        int ConditionValue3 { get; set; }

        int NegativeCondition { get; set; }

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

        public int ConditionTarget { get; set; }

        public int ConditionValue1 { get; set; }

        public int ConditionValue2 { get; set; }

        public int ConditionValue3 { get; set; }

        public int NegativeCondition { get; set; }

        public string Comment { get; set; }
    }
}