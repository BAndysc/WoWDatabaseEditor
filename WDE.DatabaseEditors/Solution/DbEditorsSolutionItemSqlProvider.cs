using System.Linq;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbEditorsSolutionItemSqlProvider : ISolutionItemSqlProvider<DbEditorsSolutionItem>
    {
        public string GenerateSql(DbEditorsSolutionItem item)
        {
            var fields = item.TableData.Categories.SelectMany(c => c.Fields).Where(f => f.IsModified)
                .Select(f => f.ToSqlFieldDescription());
            string fieldsString = string.Join(", ", fields);
            return $"UPDATE `{item.TableData.DbTableName}` SET {fieldsString} WHERE `{item.TableData.TableIndexFieldName}`= {item.TableData.TableIndexValue};";
        }
    }
}