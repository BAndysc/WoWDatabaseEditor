using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Mvvm;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseDefinitionEditor.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;

#pragma warning disable 4014

namespace WDE.DatabaseDefinitionEditor.ViewModels
{
    [AutoRegister]
    public class DefinitionGeneratorViewModel : BindableBase
    {
        private readonly IDatabaseQueryExecutor mySqlExecutor;
        private readonly IDefinitionGeneratorService generatorService;

        public AsyncAutoCommand SaveAllDefinitions { get; }
        public ObservableCollection<DatabaseTable> Tables { get; } = new();

        private bool isLoading;

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private DatabaseTable? selectedTable;

        public DatabaseTable? SelectedTable
        {
            get => selectedTable;
            set
            {
                SetProperty(ref selectedTable, value);
                if (value != null)
                    UpdateDefinition(value.Value);
            }
        }

        public INativeTextDocument Definition { get; }

        public DefinitionGeneratorViewModel(IDatabaseQueryExecutor mySqlExecutor,
            IDefinitionGeneratorService generatorService,
            INativeTextDocument nativeTextDocument,
            IWindowManager windowManager)
        {
            this.mySqlExecutor = mySqlExecutor;
            this.generatorService = generatorService;
            Definition = nativeTextDocument;

            SaveAllDefinitions = new AsyncAutoCommand(async () =>
            {
                var folder = await windowManager.ShowFolderPickerDialog("");
                if (folder == null)
                    return;

                foreach (var table in Tables)
                {
                    var path = Path.Join(folder, table + ".json");
                    await File.WriteAllTextAsync(path, await GenerateDefinition(table));
                }
            });
        }

        public async Task PopulateTables()
        {
            IsLoading = true;
            Tables.AddRange(await mySqlExecutor.GetTables(DataDatabaseType.World));
            Tables.AddRange(await mySqlExecutor.GetTables(DataDatabaseType.Hotfix));
            IsLoading = false;
        }

        private async Task UpdateDefinition(DatabaseTable tableName)
        {
            IsLoading = true;
            Definition.FromString(await GenerateDefinition(tableName));
            IsLoading = false;
        }

        private async Task<string> GenerateDefinition(DatabaseTable tableName)
        {
            var definition = await generatorService.GenerateDefinition(tableName);
            return SerializeDefinition(definition);
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
}