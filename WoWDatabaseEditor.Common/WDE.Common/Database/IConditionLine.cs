using System;
using System.Linq;
using System.Collections.Generic;
using Generator.Equals;

namespace WDE.Common.Database
{
    public interface ICondition
    {
        int ElseGroup { get; }

        int ConditionIndex { get; }

        int ConditionParent { get; }

        int ConditionType { get; }

        byte ConditionTarget { get; }

        long ConditionValue1 { get; }

        long ConditionValue2 { get; }

        long ConditionValue3 { get; }

        long ConditionValue4 { get; }

        string ConditionStringValue1 { get; }

        int NegativeCondition { get; }

        string? Comment { get; }
    }

    public interface IConditionLine : ICondition
    {
        int SourceType { get; }

        int SourceGroup { get; }

        int SourceEntry { get; }

        int SourceId { get; }
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
    [Equatable] // keep equatable only for primary key columns
    public partial class AbstractConditionLine : IConditionLine
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

        [DefaultEquality]
        public int SourceType { get; init; }

        [DefaultEquality]
        public int SourceGroup { get; init; }

        [DefaultEquality]
        public int SourceEntry { get; init; }

        [DefaultEquality]
        public int SourceId { get; init; }

        [DefaultEquality]
        public int ElseGroup { get; init; }

        [DefaultEquality]
        public int ConditionIndex { get; init; }

        [DefaultEquality]
        public int ConditionParent { get; init; }

        [DefaultEquality]
        public int ConditionType { get; init; }

        [DefaultEquality]
        public byte ConditionTarget { get; init; }

        [DefaultEquality]
        public long ConditionValue1 { get; init; }

        [DefaultEquality]
        public long ConditionValue2 { get; init; }

        [DefaultEquality]
        public long ConditionValue3 { get; init; }

        [DefaultEquality]
        public long ConditionValue4 { get; init; }

        [IgnoreEquality]
        public string ConditionStringValue1 { get; init; } = "";

        [DefaultEquality]
        public int NegativeCondition { get; init; }

        [IgnoreEquality]
        public string? Comment { get; init; }
    }
    
    // KEEP it POCO
    [Equatable] // keep equatable for primary key columns only
    public partial class AbstractCondition : ICondition
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
        
        [DefaultEquality] public int ElseGroup { get; init; }
        
        [DefaultEquality] public int ConditionIndex { get; init; }
        
        [DefaultEquality] public int ConditionParent { get; init; }

        [DefaultEquality] public int ConditionType { get; init; }

        [DefaultEquality] public byte ConditionTarget { get; init; }

        [DefaultEquality] public long ConditionValue1 { get; init; }

        [DefaultEquality] public long ConditionValue2 { get; init; }

        [DefaultEquality] public long ConditionValue3 { get; init; }
        
        [DefaultEquality] public long ConditionValue4 { get; init; }

        [DefaultEquality] public string ConditionStringValue1 { get; init; } = "";

        [DefaultEquality] public int NegativeCondition { get; init; }

        [IgnoreEquality] public string? Comment { get; init; }

        public override string ToString()
        {
            return $"Condition[Else={ElseGroup}, Index={ConditionIndex}, Parent={ConditionParent}, Type={ConditionType}, Target={ConditionTarget}, Value1={ConditionValue1}, Value2={ConditionValue2}, Value3={ConditionValue3}, Value4={ConditionValue4}, Negative={NegativeCondition}, Comment={Comment}]";
        }

        public AbstractCondition WithConditionIndex(int newConditionIndex)
        {
            return new AbstractCondition
            {
                ElseGroup = ElseGroup,
                ConditionIndex = newConditionIndex,
                ConditionParent = ConditionParent,
                ConditionType = ConditionType,
                ConditionTarget = ConditionTarget,
                ConditionValue1 = ConditionValue1,
                ConditionValue2 = ConditionValue2,
                ConditionValue3 = ConditionValue3,
                ConditionValue4 = ConditionValue4,
                ConditionStringValue1 = ConditionStringValue1,
                NegativeCondition = NegativeCondition,
                Comment = Comment
            };
        }

        public AbstractCondition WithConditionParent(int newConditionParent)
        {
            return new AbstractCondition
            {
                ElseGroup = ElseGroup,
                ConditionIndex = ConditionIndex,
                ConditionParent = newConditionParent,
                ConditionType = ConditionType,
                ConditionTarget = ConditionTarget,
                ConditionValue1 = ConditionValue1,
                ConditionValue2 = ConditionValue2,
                ConditionValue3 = ConditionValue3,
                ConditionValue4 = ConditionValue4,
                ConditionStringValue1 = ConditionStringValue1,
                NegativeCondition = NegativeCondition,
                Comment = Comment
            };
        }
    }
}