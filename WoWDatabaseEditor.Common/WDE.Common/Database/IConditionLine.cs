namespace WDE.Common.Database
{
    public interface ICondition
    {
        int ElseGroup { get; set; }

        int ConditionType { get; set; }

        byte ConditionTarget { get; set; }

        int ConditionValue1 { get; set; }

        int ConditionValue2 { get; set; }

        int ConditionValue3 { get; set; }

        int NegativeCondition { get; set; }

        string Comment { get; set; }
    }

    public interface IConditionLine : ICondition
    {
        int SourceType { get; set; }

        int SourceGroup { get; set; }

        int SourceEntry { get; set; }

        int SourceId { get; set; }

        int ElseGroup { get; set; }

        int ConditionType { get; set; }

        byte ConditionTarget { get; set; }

        int ConditionValue1 { get; set; }

        int ConditionValue2 { get; set; }

        int ConditionValue3 { get; set; }

        int NegativeCondition { get; set; }

        string Comment { get; set; }
    }
    
    public class AbstractConditionLine : IConditionLine
    {
        public AbstractConditionLine() { }

        public AbstractConditionLine(IConditionLine line)
        {
            SourceType = line.SourceType;
            SourceGroup = line.SourceGroup;
            SourceEntry = line.SourceEntry;
            SourceId = line.SourceId;
            ElseGroup = line.ElseGroup;
            ConditionType = line.ConditionType;
            ConditionTarget = line.ConditionTarget;
            ConditionValue1 = line.ConditionValue1;
            ConditionValue2 = line.ConditionValue2;
            ConditionValue3 = line.ConditionValue3;
            NegativeCondition = line.NegativeCondition;
            Comment = line.Comment;
        }

        public AbstractConditionLine(int sourceType, int sourceGroup, int sourceEntry, int sourceId, ICondition condition)
        {
            SourceType = sourceType;
            SourceGroup = sourceGroup;
            SourceEntry = sourceEntry;
            SourceId = sourceId;
            ElseGroup = condition.ElseGroup;
            ConditionType = condition.ConditionType;
            ConditionTarget = condition.ConditionTarget;
            ConditionValue1 = condition.ConditionValue1;
            ConditionValue2 = condition.ConditionValue2;
            ConditionValue3 = condition.ConditionValue3;
            NegativeCondition = condition.NegativeCondition;
            Comment = condition.Comment;
        }
        
        public int SourceType { get; set; }

        public int SourceGroup { get; set; }

        public int SourceEntry { get; set; }

        public int SourceId { get; set; }

        public int ElseGroup { get; set; }

        public int ConditionType { get; set; }

        public byte ConditionTarget { get; set; }

        public int ConditionValue1 { get; set; }

        public int ConditionValue2 { get; set; }

        public int ConditionValue3 { get; set; }

        public int NegativeCondition { get; set; }

        public string Comment { get; set; }
    }
    
    public class AbstractCondition : ICondition
    {
        public AbstractCondition() { }

        public AbstractCondition(ICondition line)
        {
            ElseGroup = line.ElseGroup;
            ConditionType = line.ConditionType;
            ConditionTarget = line.ConditionTarget;
            ConditionValue1 = line.ConditionValue1;
            ConditionValue2 = line.ConditionValue2;
            ConditionValue3 = line.ConditionValue3;
            NegativeCondition = line.NegativeCondition;
            Comment = line.Comment;
        }
        
        public int ElseGroup { get; set; }

        public int ConditionType { get; set; }

        public byte ConditionTarget { get; set; }

        public int ConditionValue1 { get; set; }

        public int ConditionValue2 { get; set; }

        public int ConditionValue3 { get; set; }

        public int NegativeCondition { get; set; }

        public string Comment { get; set; }
    }
}