using System.Text.RegularExpressions;
using SmartFormat;
using WDE.Common.Database;

namespace WDE.Conditions.Exporter
{
    public static class ConditionsSerializer
    {
        private static readonly Regex ConditionLineRegex = new(
            @"\(\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+),\s*"".*?""\s*\)");

        private static readonly string ConditionSql =
            @"({sourceType}, {sourceGroup}, {sourceEntry}, {sourceId}, {elseGroup}, {conditionType}, {conditionTarget}, {conditionValue1}, {conditionValue2}, {conditionValue3}, {negativeCondition}, ""{comment}"")";
        
        public static string ToSqlString(this IConditionLine line)
        {
            object l = new
            {
                sourceType = line.SourceType,
                sourceGroup = line.SourceGroup,
                sourceEntry = line.SourceEntry,
                sourceId = line.SourceId,
                elseGroup = line.ElseGroup,
                conditionType = line.ConditionType,
                conditionTarget = line.ConditionTarget,
                conditionValue1 = line.ConditionValue1,
                conditionValue2 = line.ConditionValue2,
                conditionValue3 = line.ConditionValue3,
                negativeCondition = line.NegativeCondition,
                comment = line.Comment,
            };
            return Smart.Format(ConditionSql, l);
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