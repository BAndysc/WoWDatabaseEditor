using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.Services;

[AutoRegister]
[SingleInstance]
public class DefinitionGeneratorService : IDefinitionGeneratorService
{
    private readonly IDatabaseQueryExecutor mySqlExecutor;
    private readonly ICurrentCoreVersion currentCoreVersion;

    public DefinitionGeneratorService(IDatabaseQueryExecutor mySqlExecutor,
        ICurrentCoreVersion currentCoreVersion)
    {
        this.mySqlExecutor = mySqlExecutor;
        this.currentCoreVersion = currentCoreVersion;
    }
    
    public async Task<DatabaseTableDefinitionJson> GenerateDefinition(DatabaseTable tableName)
    {
        var columns = await mySqlExecutor.GetTableColumns(tableName.Database, tableName.Table);

        var primaryKeys = columns.Where(c => c.PrimaryKey).ToList();

        List<DatabaseColumnJson> columnsJson = new();
        foreach (var column in columns)
        {
            var isInt = column.ManagedType == typeof(sbyte) || column.ManagedType == typeof(short) ||
                        column.ManagedType == typeof(int) || column.ManagedType == typeof(long);
            var isUInt = column.ManagedType == typeof(byte) || column.ManagedType == typeof(ushort) ||
                         column.ManagedType == typeof(uint) || column.ManagedType == typeof(ulong);
            var isFloat = column.ManagedType == typeof(float);

            var defaultIsZero = column.DefaultValue != null &&
                                int.TryParse(column.DefaultValue.ToString(), out var asInt) && asInt == 0;
            
            columnsJson.Add(new DatabaseColumnJson()
            {
                Name = column.ColumnName.ToTitleCase(),
                DbColumnName = column.ColumnName,
                CanBeNull = column.Nullable,
                Default = defaultIsZero ? null : column.DefaultValue,
                ValueType = isInt ? "int" : (isUInt ? "uint" : (isFloat ? "float" : "string")),
                IsReadOnly = primaryKeys.Count > 0 && column.ColumnName == primaryKeys[0].ColumnName
            });
        }

        DatabaseTableDefinitionJson tableDefinition = new();
        tableDefinition.DataDatabaseType = tableName.Database;
        tableDefinition.Compatibility = new List<string>() {currentCoreVersion.Current.Tag};
        tableDefinition.Name = tableName.Table.ToTitleCase();
        tableDefinition.TableName = tableName.Table;
        tableDefinition.GroupName = "CATEGORY";
        tableDefinition.SkipQuickLoad = true;
        tableDefinition.RecordMode = RecordMode.SingleRow;//primaryKeys.Count != 1 ? RecordMode.MultiRecord : RecordMode.SingleRow;
        if (tableDefinition.RecordMode == RecordMode.SingleRow)
            tableDefinition.SingleSolutionName = tableDefinition.MultiSolutionName = $"{tableName.Table.ToTitleCase()} Table";
        else
        {
            tableDefinition.SingleSolutionName = "{name} " + tableName + " editor";
            tableDefinition.MultiSolutionName = $"multiple {tableName} editor";
        }
        tableDefinition.Description = $"Here insert short description what is {tableName} for";
        tableDefinition.IconPath = "Icons/document_.png";
        tableDefinition.ReloadCommand = $"reload {tableName.Table}";
        tableDefinition.TablePrimaryKeyColumnName = new ColumnFullName(null, primaryKeys.Count > 0
            ? primaryKeys[0].ColumnName
            : (columns.Count > 0 ? columns[0].ColumnName : ""));
        if (tableDefinition.RecordMode != RecordMode.SingleRow)
            tableDefinition.Picker = "Parameter";
        tableDefinition.PrimaryKey = primaryKeys.Select(c => new ColumnFullName(null, c.ColumnName)).ToList();
        tableDefinition.Groups = new List<DatabaseColumnsGroupJson>()
        {
            new()
            {
                Name = "group",
                Fields = columnsJson
            }
        };
        return tableDefinition;
    }
}