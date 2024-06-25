using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Modules;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class TableDefinitionProvider : ITableDefinitionProvider, IGlobalAsyncInitializer
    {
        private readonly ITableDefinitionDeserializer serializationProvider;
        private readonly Lazy<ITableDefinitionJsonProvider> jsonProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IMessageBoxService messageBoxService;
        private readonly List<DatabaseTableDefinitionJson> incompatibleDefinitionList = new();
        private readonly Dictionary<DatabaseTable, DatabaseTableDefinitionJson> incompatibleDefinitions = new();
        private readonly Dictionary<DatabaseTable, DatabaseTableDefinitionJson> definitions = new();
        private readonly Dictionary<DatabaseTable, DatabaseTableDefinitionJson> definitionsByTableName = new();
        private readonly Dictionary<DatabaseTable, DatabaseTableDefinitionJson> definitionsByForeignTableName = new();

        private CancellationTokenSource? cancellationTokenSource;

        public TableDefinitionProvider(ITableDefinitionDeserializer serializationProvider,
            Lazy<ITableDefinitionJsonProvider> jsonProvider,
            ICurrentCoreVersion currentCoreVersion,
            IMessageBoxService messageBoxService)
        {
            this.serializationProvider = serializationProvider;
            this.jsonProvider = jsonProvider;
            this.currentCoreVersion = currentCoreVersion;
            this.messageBoxService = messageBoxService;
        }

        private async Task LoadDefinitions(bool invokeEvent, CancellationToken token)
        {
            var references = await jsonProvider.Value.GetDefinitionReferences();
            var definitionSources = await jsonProvider.Value.GetDefinitionSources();

            if (token.IsCancellationRequested)
                return;

            incompatibleDefinitionList.Clear();
            incompatibleDefinitions.Clear();
            definitions.Clear();
            definitionsByTableName.Clear();
            definitionsByForeignTableName.Clear();

            Dictionary<string, List<DatabaseTableReferenceJson>> fileToExtraCompatibility = new();
            foreach (var source in references)
            {
                try
                {
                    var reference =
                        serializationProvider.DeserializeTableDefinition<DatabaseTableReferenceJson>(source.content);
                    if (!fileToExtraCompatibility.ContainsKey(reference.File))
                        fileToExtraCompatibility[reference.File] = new List<DatabaseTableReferenceJson>();
                    fileToExtraCompatibility[reference.File].Add(reference);
                }
                catch (Exception e)
                {
                    ShowLoadingError(source.file, e);
                }
            }

            foreach (var source in definitionSources)
            {
                try
                {
                    if (string.IsNullOrEmpty(source.content))
                        continue;

                    var definition =
                        serializationProvider.DeserializeTableDefinition<DatabaseTableDefinitionJson>(source.content);

                    if (string.IsNullOrEmpty(definition.MultiSolutionName))
                        definition.MultiSolutionName = definition.Name;

                    if (string.IsNullOrEmpty(definition.SingleSolutionName))
                        definition.SingleSolutionName = definition.Name;

                    if (string.IsNullOrEmpty(definition.IconPath))
                        definition.IconPath = "Icons/document_table.png";

                    definition.TableColumns = new Dictionary<ColumnFullName, DatabaseColumnJson>();
                    foreach (var group in definition.Groups)
                    {
                        foreach (var column in group.Fields)
                        {
                            definition.TableColumns[column.DbColumnFullName] = column;
                        }
                    }

                    definition.FileName = source.file;
                    definition.AbsoluteFileName = new FileInfo(source.file).FullName;

                    if (definition.ForeignTable != null)
                    {
                        definition.ForeignTableByName = new Dictionary<string, DatabaseForeignTableJson>();
                        foreach (var foreign in definition.ForeignTable)
                        {
                            definition.ForeignTableByName[foreign.TableName] = foreign;
                        }
                    }

                    if (definition.Compatibility.Contains(currentCoreVersion.Current.Tag) ||
                        fileToExtraCompatibility.TryGetValue(source.file, out var reference) &&
                        reference.Any(r => r.Compatibility == currentCoreVersion.Current.Tag))
                    {
                        definitions[new DatabaseTable(definition.DataDatabaseType, definition.TableName)] = definition;
                        definitionsByTableName[new DatabaseTable(definition.DataDatabaseType, definition.TableName)] = definition;
                        if (definition.ForeignTable != null)
                        {
                            foreach (var foreign in definition.ForeignTable)
                                definitionsByForeignTableName[new DatabaseTable(definition.DataDatabaseType, foreign.TableName)] = definition;
                        }
                    }
                    else
                    {
                        incompatibleDefinitions[new DatabaseTable(definition.DataDatabaseType, definition.TableName)] = definition;
                        incompatibleDefinitionList.Add(definition);
                    }
                }
                catch (Exception e)
                {
                    ShowLoadingError(source.file, e);
                }
            }
            if (invokeEvent)
                DefinitionsChanged?.Invoke();
        }

        public async Task Initialize()
        {
            await LoadDefinitions(false, default);
            jsonProvider.Value.FilesChanged += OnSourceFileChanged;
        }

        private void OnSourceFileChanged()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new();
            LoadDefinitions(true, cancellationTokenSource.Token).ListenErrors();
        }

        private void ShowLoadingError(string sourceFile, Exception e)
        {
            LOG.LogError(e);
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Cannot load table definition")
                    .SetContent("Can't load table definition: " + sourceFile+".\n\n" + e.ToString())
                    .WithOkButton(true)
                    .Build())
                .ListenErrors();
        }

        public IEnumerable<string>? CoreCompatibility(DatabaseTable definitionId)
        {
            if (incompatibleDefinitions.TryGetValue(definitionId, out var definition))
                return definition.Compatibility;
            return null;
        }

        public DatabaseTableDefinitionJson? GetDefinitionByTableName(DatabaseTable? tableName)
        {
            if (tableName == null)
                return null;
            
            if (definitionsByTableName.TryGetValue(tableName.Value, out var definition))
                return definition;
            
            return null;
        }
        
        public DatabaseTableDefinitionJson? GetDefinitionByForeignTableName(DatabaseTable? tableName)
        {
            if (tableName == null)
                return null;
            
            if (definitionsByForeignTableName.TryGetValue(tableName.Value, out var definition))
                return definition;
            
            return null;
        }

        public IEnumerable<DatabaseTableDefinitionJson> IncompatibleDefinitions => incompatibleDefinitions.Values;
        public IEnumerable<DatabaseTableDefinitionJson> AllDefinitions =>
            definitions.Values.Concat(incompatibleDefinitionList);
        public IEnumerable<DatabaseTableDefinitionJson> Definitions => definitions.Values;
        public event Action? DefinitionsChanged;
    }
}
