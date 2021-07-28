using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Conditions.Exporter
{
    [AutoRegister]
    public class ConditionQueryGenerator : IConditionQueryGenerator
    {
        public string BuildDeleteQuery(IDatabaseProvider.ConditionKey key)
        {
            StringBuilder sql = new();
            List<string> keySql = new List<string>();
            keySql.Add($"`SourceTypeOrReferenceId` = {key.SourceType}");
            
            if (key.SourceGroup.HasValue)   
                keySql.Add($"`SourceGroup` = {key.SourceGroup.Value}");
                
            if (key.SourceEntry.HasValue)   
                keySql.Add($"`SourceEntry` = {key.SourceEntry.Value}");
                
            if (key.SourceId.HasValue)   
                keySql.Add($"`SourceId` = {key.SourceId.Value}");

            sql.Append("DELETE FROM `conditions` WHERE ");
            sql.Append(string.Join(" AND ", keySql));
            sql.AppendLine(";");
            
            return sql.ToString();
        }

        public string BuildInsertQuery(IList<IConditionLine> conditions)
        {
            if (conditions.Count == 0)
                return "";
            
            StringBuilder sql = new();
            sql.AppendLine(
                "INSERT INTO `conditions` (SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, ConditionValue1, ConditionValue2, ConditionValue3, NegativeCondition, Comment) VALUES");
            sql.Append(string.Join(",\n", conditions.Select(c => c.ToSqlString())));
            sql.AppendLine(";");

            return sql.ToString();
        }
    }
}