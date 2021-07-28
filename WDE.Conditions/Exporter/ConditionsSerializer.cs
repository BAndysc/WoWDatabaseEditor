using System.Text.RegularExpressions;
using SmartFormat;
using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.Conditions.Exporter
{
    public static class ConditionsSerializer
    {
        private static readonly Regex ConditionLineRegex = new(
            @"\(\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*"".*?""\s*\)");

        private static readonly string ConditionSql =
            @"({SourceTypeOrReferenceId}, {SourceGroup}, {SourceEntry}, {SourceId}, {ElseGroup}, {ConditionTypeOrReference}, {ConditionTarget}, {ConditionValue1}, {ConditionValue2}, {ConditionValue3}, {NegativeCondition}, {Comment})";
        
        public static object ToSqlObject(this IConditionLine line)
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
                Comment = line.Comment,
            };
        }
        
        public static string ToSqlString(this IConditionLine line)
        {
            var obj = line.ToSqlObject();
            ((dynamic)obj).Comment = ((string)((dynamic)obj).Comment).ToSqlEscapeString();
            return Smart.Format(ConditionSql, obj);
        }
        
        public static bool TryToConditionLine(this string str, out IConditionLine line)
        {
            line = new AbstractConditionLine();

            Match m = ConditionLineRegex.Match(str);
            if (!m.Success)
                return false;

            line.SourceType = int.Parse(m.Groups[1].ToString());
            line.SourceGroup = int.Parse(m.Groups[2].ToString());
            line.SourceEntry = int.Parse(m.Groups[3].ToString());
            line.SourceId = int.Parse(m.Groups[4].ToString());
            line.ElseGroup = int.Parse(m.Groups[5].ToString());
            line.ConditionType = int.Parse(m.Groups[6].ToString());
            line.ConditionTarget = byte.Parse(m.Groups[7].ToString());
            line.ConditionValue1 = int.Parse(m.Groups[8].ToString());
            line.ConditionValue2 = int.Parse(m.Groups[9].ToString());
            line.ConditionValue3 = int.Parse(m.Groups[10].ToString());
            line.NegativeCondition = int.Parse(m.Groups[11].ToString());
            line.Comment = m.Groups[11].ToString();

            return true;
        }
    }
    
}