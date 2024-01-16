using System.Text.RegularExpressions;
using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.Conditions.Exporter
{
    public static class ConditionsSerializer
    {
        public static object ToSqlObject(this IConditionLine line, bool escapeComment = false)
        {
            return new
            {
                SourceTypeOrReferenceId = line.SourceType,
                SourceGroup = line.SourceGroup,
                SourceEntry = line.SourceEntry,
                SourceId = line.SourceId,
                ElseGroup = line.ElseGroup,
                ConditionTypeOrReference = line.ConditionType,
                ConditionTarget = line.ConditionTarget,
                ConditionValue1 = line.ConditionValue1,
                ConditionValue2 = line.ConditionValue2,
                ConditionValue3 = line.ConditionValue3,
                NegativeCondition = line.NegativeCondition,
                Comment = escapeComment ? line.Comment?.ToSqlEscapeString() : line.Comment,
            };
        }
        
        public static object ToSqlObjectMaster(this IConditionLine line, bool escapeComment = false)
        {
            return new
            {
                SourceTypeOrReferenceId = line.SourceType,
                SourceGroup = line.SourceGroup,
                SourceEntry = line.SourceEntry,
                SourceId = line.SourceId,
                ElseGroup = line.ElseGroup,
                ConditionTypeOrReference = line.ConditionType,
                ConditionTarget = line.ConditionTarget,
                ConditionValue1 = line.ConditionValue1,
                ConditionValue2 = line.ConditionValue2,
                ConditionValue3 = line.ConditionValue3,
                ConditionStringValue1 = line.ConditionStringValue1,
                NegativeCondition = line.NegativeCondition,
                Comment = escapeComment ? line.Comment?.ToSqlEscapeString() : line.Comment,
            };
        }
    }
    
}