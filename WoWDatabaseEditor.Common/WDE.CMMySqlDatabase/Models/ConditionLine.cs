using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]

    /// <summary>
    /// Condition System
    /// </summary>
    [Table("conditions")]
    public class ConditionLineWoTLK : IConditionLine
    {
        /// <summary>
        /// Identifier
        /// </summary>
        [Column("condition_entry", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public uint    ConditionEntry { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// Type of the condition
        /// </summary>
        [Column("type"                                                                                             )] public sbyte   Type           { get; set; } // tinyint(3)
        /// <summary>
        /// data field one for the condition
        /// </summary>
        [Column("value1"                                                                                           )] public uint    Value1         { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// data field two for the condition
        /// </summary>
        [Column("value2"                                                                                           )] public uint    Value2         { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// data field three for the condition
        /// </summary>
        [Column("value3"                                                                                           )] public uint    Value3         { get; set; } // mediumint(8) unsigned
        /// <summary>
        /// data field four for the condition
        /// </summary>
        [Column("value4"                                                                                           )] public uint    Value4         { get; set; } // mediumint(8) unsigned
        [Column("flags"                                                                                            )] public byte    Flags          { get; set; } // tinyint(3) unsigned
        [Column("comments"                                                                                         )] public string? Comment        { get; set; } // varchar(500)

        public int SourceType { get; set; } = 0;
        public int SourceGroup { get; set; } = 0;
        public int SourceEntry { get; set; } = 0;
        public int SourceId { get; set; } = 0;
        public int ElseGroup { get; set; } = 0;
        public int ConditionType { get; set; } = 0;
        public byte ConditionTarget { get; set; } = 0;
        public int ConditionValue1 { get; set; } = 0;
        public int ConditionValue2 { get; set; } = 0;
        public int ConditionValue3 { get; set; } = 0;
        public int NegativeCondition { get; set; } = 0;

        public ConditionLineWoTLK() { }

        public ConditionLineWoTLK(IConditionLine otherLine)
        {
            SourceType = otherLine.SourceType;
            SourceGroup = otherLine.SourceGroup;
            SourceEntry = otherLine.SourceEntry;
            SourceId = otherLine.SourceId;
            ElseGroup = otherLine.ElseGroup;
            ConditionType = otherLine.ConditionType;
            ConditionTarget = otherLine.ConditionTarget;
            ConditionValue1 = otherLine.ConditionValue1;
            ConditionValue2 = otherLine.ConditionValue2;
            ConditionValue3 = otherLine.ConditionValue3;
            NegativeCondition = otherLine.NegativeCondition;
            Comment = otherLine.Comment;
        }
    }


  
}