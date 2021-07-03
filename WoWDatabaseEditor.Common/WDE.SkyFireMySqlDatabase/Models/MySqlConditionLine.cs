using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]
    [Table(Name = "conditions")]
    public class MySqlConditionLine : IConditionLine
    {
        [Column(Name = "SourceTypeOrReferenceId")]
        [PrimaryKey]
        public int SourceType { get; set; }

        [Column(Name = "SourceGroup")]
        [PrimaryKey]
        public int SourceGroup { get; set; }

        [Column(Name = "SourceEntry")]
        [PrimaryKey]
        public int SourceEntry { get; set; }

        [Column(Name = "SourceId")]
        [PrimaryKey]
        public int SourceId { get; set; }

        [Column(Name = "ElseGroup")]
        [PrimaryKey]
        public int ElseGroup { get; set; }

        [Column(Name = "ConditionTypeOrReference")]
        [PrimaryKey]
        public int ConditionType { get; set; }

        [Column(Name = "ConditionTarget")]
        [PrimaryKey]
        public byte ConditionTarget { get; set; }

        [Column(Name = "ConditionValue1")]
        [PrimaryKey]
        public int ConditionValue1 { get; set; }

        [Column(Name = "ConditionValue2")]
        [PrimaryKey]
        public int ConditionValue2 { get; set; }

        [Column(Name = "ConditionValue3")]
        [PrimaryKey]
        public int ConditionValue3 { get; set; }

        [Column(Name = "NegativeCondition")]
        [PrimaryKey]
        public int NegativeCondition { get; set; }

        [Column(Name = "Comment")]
        public string? Comment { get; set; } = "";

        public MySqlConditionLine() { }

        public MySqlConditionLine(IConditionLine otherLine)
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