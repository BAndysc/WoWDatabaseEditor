using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Database;

namespace WDE.Conditions.Exporter
{
    public class ConditionsExporter
    {
        private readonly IConditionLine[] conditions;
        private readonly IDatabaseProvider.ConditionKey key;
        private readonly StringBuilder sql = new();

        public ConditionsExporter(IConditionLine[] conditions, IDatabaseProvider.ConditionKey key)
        {
            this.conditions = conditions;
            this.key = key;
        }

        public string GetSql()
        {
            Build();
            return sql.ToString();
        }

        private void Build()
        {
            BuildDelete();
            if (conditions.Length > 0)
            {
                BuildInsert();
                BuildValues();
            }
        }

        private void BuildValues()
        {
            sql.Append(string.Join(",\n", conditions.Select(c => c.ToSqlString())));
            sql.AppendLine(";");
        }

        private void BuildInsert()
        {
            sql.AppendLine(
                "INSERT INTO conditions (SourceTypeOrReferenceId, SourceGroup, SourceEntry, SourceId, ElseGroup, ConditionTypeOrReference, ConditionTarget, ConditionValue1, ConditionValue2, ConditionValue3, NegativeCondition, Comment) VALUES");
        }

        private void BuildDelete()
        {
            List<string> keySql = new List<string>();
            keySql.Add($"SourceTypeOrReferenceId = {key.SourceType}");
            
            if (key.SourceGroup.HasValue)   
                keySql.Add($"SourceGroup = {key.SourceGroup.Value}");
                
            if (key.SourceEntry.HasValue)   
                keySql.Add($"SourceEntry = {key.SourceEntry.Value}");
                
            if (key.SourceId.HasValue)   
                keySql.Add($"SourceId = {key.SourceId.Value}");

            sql.Append("DELETE FROM conditions WHERE ");
            sql.Append(string.Join(" AND ", keySql));
            sql.AppendLine(";");
        }
    }
}