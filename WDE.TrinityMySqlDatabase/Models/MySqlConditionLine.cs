using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shaolinq;
using WDE.Common.Database;
using System.Diagnostics.CodeAnalysis;

namespace WDE.TrinityMySqlDatabase.Models
{
    [ExcludeFromCodeCoverage]
    [DataAccessObject(Name = "conditions")]
    public abstract class MySqlConditionLine : DataAccessObject, IConditionLine
    {
        [PersistedMember(Name = "SourceTypeOrReferenceId")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int SourceType { get; set; }

        [PersistedMember(Name = "SourceGroup")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int SourceGroup { get; set; }

        [PersistedMember(Name = "SourceEntry")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int SourceEntry { get; set; }

        [PersistedMember(Name = "SourceId")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int SourceId { get; set; }

        [PersistedMember(Name = "ElseGroup")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ElseGroup { get; set; }

        [PersistedMember(Name = "ConditionTypeOrReference")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ConditionType { get; set; }

        [PersistedMember(Name = "ConditionTarget")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ConditionTarget { get; set; }

        [PersistedMember(Name = "ConditionValue1")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ConditionValue1 { get; set; }

        [PersistedMember(Name = "ConditionValue2")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ConditionValue2 { get; set; }

        [PersistedMember(Name = "ConditionValue3")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int ConditionValue3 { get; set; }

        [PersistedMember(Name = "NegativeCondition")]
        [PrimaryKey(IsPrimaryKey = true)]
        public abstract int NegativeCondition { get; set; }

        [PersistedMember(Name = "Comment")]
        public abstract string Comment { get; set; }
    }
}
