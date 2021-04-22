using System.Linq;
using System.Text;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.QueryGenerators
{
    [SingleInstance]
    [AutoRegister]
    public class QueryGenerator : IQueryGenerator
    {       
        private readonly ITableDefinitionProvider tableDefinitionProvider;

        public QueryGenerator(ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public string GenerateQuery(IDatabaseTableData tableData)
        {
            return GenerateUpdateQuery(tableData);
        }

        private string GenerateUpdateQuery(IDatabaseTableData tableData)
        {
            StringBuilder query = new();
            
            foreach (var entity in tableData.Entities)
            {
                if (entity.ExistInDatabase)
                {
                    var updates = string.Join(", ",
                        entity.Fields
                            .Where(f => f.IsModified)
                            .Select(f => $"`{f.FieldName}` = {f.ToQueryString()}"));
                
                    if (string.IsNullOrEmpty(updates))
                        continue;
                
                    var updateQuery = $"UPDATE `{tableData.TableDefinition.TableName}` SET {updates} WHERE `{tableData.TableDefinition.TablePrimaryKeyColumnName}`= {entity.Key}";
                    query.AppendLine(updateQuery);
                }
                else
                {
                    query.AppendLine(
                        $"DELETE FROM {tableData.TableDefinition.TableName} WHERE `{tableData.TableDefinition.TablePrimaryKeyColumnName}` = {entity.Key};");
                    var columns = string.Join(", ", entity.Fields.Select(f => $"`{f.FieldName}`"));
                    query.AppendLine($"INSERT INTO {tableData.TableDefinition.TableName} ({columns}) VALUES");
                    var values = string.Join(", ", entity.Fields.Select(f => f.ToQueryString()));
                    query.AppendLine($"({values});");
                }
            }

            return query.ToString();
        }
    }
    
    public interface IQueryGenerator
    {
        public string GenerateQuery(IDatabaseTableData tableData);
    }
}