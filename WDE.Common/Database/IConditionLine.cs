using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
