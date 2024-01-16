using System;
using System.Linq;
using System.Collections.Generic;

namespace WDE.Common.Database
{
    public interface ICondition
    {
        int ElseGroup { get; set; }
        
        int ConditionIndex { get; set; }
        
        int ConditionParent { get; set; }

        int ConditionType { get; set; }

        byte ConditionTarget { get; set; }

        long ConditionValue1 { get; set; }

        long ConditionValue2 { get; set; }

        long ConditionValue3 { get; set; }

        long ConditionValue4 { get; set; }

        string ConditionStringValue1 { get; set; }
        
        int NegativeCondition { get; set; }

        string? Comment { get; set; }
    }

    public interface IConditionLine : ICondition
    {
        int SourceType { get; set; }

        int SourceGroup { get; set; }

        int SourceEntry { get; set; }

        int SourceId { get; set; }
    }

    public static class ConditionLineExtensions
    {
        public static bool IsParentCondition(this ICondition condition)
        {
            return condition.ConditionIndex > 0;
        }
        
        public static bool IsActualCondition(this ICondition condition)
        {
            return !condition.IsParentCondition();
        }

        public static int CountActualConditions(this IReadOnlyList<ICondition>? list)
        {
            return list?.Count(cond => cond.IsActualCondition()) ?? 0;
        }
        
        public static long GetConditionValue(this ICondition line, int i)
        {
            switch (i)
            {
                case 0: return line.ConditionValue1;
                case 1: return line.ConditionValue2;
                case 2: return line.ConditionValue3;
                case 3: return line.ConditionValue4;
            }

            throw new ArgumentOutOfRangeException();
        }
        
        public static string GetConditionValueString(this ICondition line, int i)
        {
            switch (i)
            {
                case 0: return line.ConditionStringValue1.ToString();
            }

            throw new ArgumentOutOfRangeException();
        }
    }
    
    // KEEP IT POCO!
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
            ConditionIndex = line.ConditionIndex;
            ConditionParent = line.ConditionParent;
            ConditionType = line.ConditionType;
            ConditionTarget = line.ConditionTarget;
            ConditionValue1 = line.ConditionValue1;
            ConditionValue2 = line.ConditionValue2;
            ConditionValue3 = line.ConditionValue3;
            ConditionValue4 = line.ConditionValue4;
            ConditionStringValue1 = line.ConditionStringValue1;
            NegativeCondition = line.NegativeCondition;
            Comment = line.Comment;
        }
        
        public AbstractConditionLine(IDatabaseProvider.ConditionKey key, ICondition condition) 
            : this(key.SourceType, key.SourceGroup ?? 0, key.SourceEntry ?? 0, key.SourceId ?? 0, condition) {}

        public AbstractConditionLine(int sourceType, int sourceGroup, int sourceEntry, int sourceId, ICondition condition)
        {
            SourceType = sourceType;
            SourceGroup = sourceGroup;
            SourceEntry = sourceEntry;
            SourceId = sourceId;
            ElseGroup = condition.ElseGroup;
            ConditionIndex = condition.ConditionIndex;
            ConditionParent = condition.ConditionParent;
            ConditionType = condition.ConditionType;
            ConditionTarget = condition.ConditionTarget;
            ConditionValue1 = condition.ConditionValue1;
            ConditionValue2 = condition.ConditionValue2;
            ConditionValue3 = condition.ConditionValue3;
            ConditionValue4 = condition.ConditionValue4;
            ConditionStringValue1 = condition.ConditionStringValue1;
            NegativeCondition = condition.NegativeCondition;
            Comment = condition.Comment;
        }
        
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

        public string ConditionStringValue1 { get; set; } = "";

        public int NegativeCondition { get; set; }

        public string? Comment { get; set; }
    }
    
    // KEEP it POCO
    public class AbstractCondition : ICondition, IEquatable<AbstractCondition>
    {
        public AbstractCondition() { }

        public AbstractCondition(ICondition line)
        {
            ElseGroup = line.ElseGroup;
            ConditionIndex = line.ConditionIndex;
            ConditionParent = line.ConditionParent;
            ConditionType = line.ConditionType;
            ConditionTarget = line.ConditionTarget;
            ConditionValue1 = line.ConditionValue1;
            ConditionValue2 = line.ConditionValue2;
            ConditionValue3 = line.ConditionValue3;
            ConditionValue4 = line.ConditionValue4;
            ConditionStringValue1 = line.ConditionStringValue1;
            NegativeCondition = line.NegativeCondition;
            Comment = line.Comment;
        }
        
        public int ElseGroup { get; set; }
        
        public int ConditionIndex { get; set; }
        
        public int ConditionParent { get; set; }

        public int ConditionType { get; set; }

        public byte ConditionTarget { get; set; }

        public long ConditionValue1 { get; set; }

        public long ConditionValue2 { get; set; }

        public long ConditionValue3 { get; set; }
        
        public long ConditionValue4 { get; set; }

        public string ConditionStringValue1 { get; set; } = "";

        public int NegativeCondition { get; set; }

        public string? Comment { get; set; }

        public override string ToString()
        {
            return $"Condition[Else={ElseGroup}, Index={ConditionIndex}, Parent={ConditionParent}, Type={ConditionType}, Target={ConditionTarget}, Value1={ConditionValue1}, Value2={ConditionValue2}, Value3={ConditionValue3}, Value4={ConditionValue4}, Negative={NegativeCondition}, Comment={Comment}]";
        }
        
        public bool Equals(AbstractCondition? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ElseGroup == other.ElseGroup && ConditionIndex == other.ConditionIndex && ConditionParent == other.ConditionParent && ConditionType == other.ConditionType && ConditionTarget == other.ConditionTarget && ConditionValue1 == other.ConditionValue1 && ConditionValue2 == other.ConditionValue2 && ConditionValue3 == other.ConditionValue3 && ConditionValue4 == other.ConditionValue4 && ConditionStringValue1 == other.ConditionStringValue1 && NegativeCondition == other.NegativeCondition && Comment == other.Comment;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AbstractCondition)obj);
        }

        public override int GetHashCode() => 0;

        public static bool operator ==(AbstractCondition? left, AbstractCondition? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AbstractCondition? left, AbstractCondition? right)
        {
            return !Equals(left, right);
        }
    }
}