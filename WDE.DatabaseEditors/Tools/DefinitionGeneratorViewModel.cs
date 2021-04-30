using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Newtonsoft.Json;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Extensions;
using WDE.Module.Attributes;

#pragma warning disable 4014

namespace WDE.DatabaseEditors.Tools
{
    #if DEBUGAVALONIA
    [AutoRegister]
    public class DefinitionGeneratorViewModel : BindableBase
    {
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly ICurrentCoreVersion currentCoreVersion;

        public ObservableCollection<string> Tables { get; } = new();

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }
        
        private string? selectedTable;
        public string? SelectedTable
        {
            get => selectedTable;
            set
            {
                SetProperty(ref selectedTable, value);
                if (value != null)
                    GenerateDefinition(value);
            }
        }
        
        public INativeTextDocument Definition { get; }
        
        public DefinitionGeneratorViewModel(IMySqlExecutor mySqlExecutor, 
            ICurrentCoreVersion currentCoreVersion,
            INativeTextDocument nativeTextDocument)
        {
            this.mySqlExecutor = mySqlExecutor;
            this.currentCoreVersion = currentCoreVersion;
            Definition = nativeTextDocument;
        }

        public async Task PopulateTables()
        {
            IsLoading = true;
            Tables.AddRange(await mySqlExecutor.GetTables());
            IsLoading = false;
        }

        private async Task GenerateDefinition(string tableName)
        {
            IsLoading = true;
            var columns = await mySqlExecutor.GetTableColumns(tableName);

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
                    ValueType = isInt ? "int" : (isUInt ? "uint" : (isFloat ? "float" : "string"))
                });
            }

            DatabaseTableDefinitionJson tableDefinition = new();
            tableDefinition.Id = tableName;
            tableDefinition.Compatibility = new List<string>() {currentCoreVersion.Current.Tag};
            tableDefinition.Name = tableName.ToTitleCase();
            tableDefinition.TableName = tableName;
            tableDefinition.SingleSolutionName = "{name} " + tableName + " editor";
            tableDefinition.MultiSolutionName = $"multiple {tableName} editor";
            tableDefinition.Description = $"Here insert short description what is {tableName} for";
            tableDefinition.IsMultiRecord = primaryKeys.Count != 1;
            tableDefinition.ReloadCommand = $"reload {tableName}";
            tableDefinition.TablePrimaryKeyColumnName = primaryKeys.Count > 0
                ? primaryKeys[0].ColumnName
                : (columns.Count > 0 ? columns[0].ColumnName : "");
            tableDefinition.Picker = "Parameter";
            tableDefinition.PrimaryKey = primaryKeys.Select(c => c.ColumnName).ToList();
            tableDefinition.Groups = new List<DatabaseColumnsGroupJson>()
            {
                new()
                {
                    Name = "group",
                    Fields = columnsJson
                }
            };

            Definition.FromString(SerializeDefinition(tableDefinition));

            IsLoading = false;
        }

        public string SerializeDefinition(DatabaseTableDefinitionJson json)
        {
            var settings = CreateJsonSerializationSettings();
            return JsonConvert.SerializeObject(json, Formatting.Indented, settings);
        }

        private JsonSerializerSettings CreateJsonSerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            return settings;
        }
    }
    #endif
}