using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.QueryGenerators
{
    public static class MultiRecordTableSqlGenerator
    {
        public static string GenerateSql(DatabaseMultiRecordTableData tableData)
        {
            var builder = new StringBuilder();
            builder.AppendLine(MakeDeleteLine(tableData));
            builder.AppendLine(PrepareInsertLine(tableData));
            foreach (var line in tableData.Rows.Select(MakeInsertValueLine))
                builder.AppendLine(line);

            builder.Remove(builder.Length - 3, 3);
            builder.Append(";");
            return builder.ToString();
        }

        private static string MakeDeleteLine(DatabaseMultiRecordTableData tableData)
        {
            return $"DELETE FROM `{tableData.DbTableName}` WHERE `{tableData.TableIndexFieldName}`= {tableData.TableIndexValue};";
        }
        
        private static string PrepareInsertLine(DatabaseMultiRecordTableData tableData)
        {
            var fieldsNames = tableData.Columns.Select(c => $"`{c.DbColumnName}`");
            return $"INSERT INTO `{tableData.DbTableName}`({string.Join(", ", fieldsNames)}) VALUES";
        }

        private static string MakeInsertValueLine(Dictionary<string, IDatabaseField> row)
        {
            var values = row.Values.Select(f => f.SqlStringValue());
            return $"({string.Join(", ", values)}),";
        }
    }
}