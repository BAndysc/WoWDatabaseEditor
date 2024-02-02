using System.Diagnostics.CodeAnalysis;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
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
        public int ElseGroup { get; init; }

        public int ConditionIndex { get; set; }

        public int ConditionParent { get; set; }

        [Column(Name = "ConditionTypeOrReference")]
        [PrimaryKey]
        public int ConditionType { get; set; }

        [Column(Name = "ConditionTarget")]
        [PrimaryKey]
        public byte ConditionTarget { get; set; }

        [Column(Name = "ConditionValue1")]
        [PrimaryKey]
        public long ConditionValue1 { get; set; }

        [Column(Name = "ConditionValue2")]
        [PrimaryKey]
        public long ConditionValue2 { get; set; }

        [Column(Name = "ConditionValue3")]
        [PrimaryKey]
        public long ConditionValue3 { get; set; }
        
        public long ConditionValue4 { get; set; }

        public virtual string ConditionStringValue1 { get; set; } = "";

        [Column(Name = "NegativeCondition")]
        [PrimaryKey]
        public int NegativeCondition { get; set; }

        [Column(Name = "Comment")]
        public string? Comment { get; set; } = "";

        public MySqlConditionLine() { }
    }

    [ExcludeFromCodeCoverage]
    [Table(Name = "conditions")]
    public class MySqlConditionLineMaster : MySqlConditionLine
    {
        [Column(Name = "ConditionStringValue1")]
        public override string ConditionStringValue1
        {
            get;
            set;
        } = "";
    }
}