using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemProviderProvider : ISolutionItemProviderProvider
    {
        private readonly ITableDefinitionProvider definitionProvider;
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly IDatabaseProvider databaseProvider;

        public DatabaseTableSolutionItemProviderProvider(ITableDefinitionProvider definitionProvider,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider,
            IMessageBoxService messageBoxService,
            IParameterFactory parameterFactory,
            IDatabaseProvider databaseProvider)
        {
            this.definitionProvider = definitionProvider;
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.databaseProvider = databaseProvider;
        }
        
        public IEnumerable<ISolutionItemProvider> Provide()
        {
            foreach (var definition in definitionProvider.Definitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, 
                    tableDataProvider,
                    itemFromListProvider,
                    messageBoxService, 
                    parameterFactory,
                    databaseProvider,
                    true);
            }
            foreach (var definition in definitionProvider.IncompatibleDefinitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, 
                    tableDataProvider,
                    itemFromListProvider,
                    messageBoxService, 
                    parameterFactory,
                    databaseProvider,
                    false);
            }
        }
    }

    internal class DatabaseTableSolutionItemProvider : ISolutionItemProvider, IRelatedSolutionItemCreator, INumberSolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly IDatabaseProvider databaseProvider;
        private readonly DatabaseTableDefinitionJson definition;
        private readonly ImageUri itemIcon;
        private bool isCompatible;
        
        internal DatabaseTableSolutionItemProvider(DatabaseTableDefinitionJson definition,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider, 
            IMessageBoxService messageBoxService,
            IParameterFactory parameterFactory,
            IDatabaseProvider databaseProvider,
            bool isCompatible)
        {
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.databaseProvider = databaseProvider;
            this.definition = definition;
            this.itemIcon = new ImageUri($"Icons/document_big.png");
            this.isCompatible = isCompatible;
        }

        public string GetName() => definition.Name;

        public ImageUri GetImage()
        {
            var icon = definition.IconPath?.Replace(".png", "_big.png");
            icon ??= "Icons/document_big.png";
            return new ImageUri(icon);
        }

        public string GetDescription() => definition.Description;
        
        public string GetGroupName() => definition.GroupName ?? "Database tables";

        public bool IsCompatibleWithCore(ICoreVersion core) => isCompatible;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var parameter = parameterFactory.Factory(definition.Picker);
            var key = await itemFromListProvider.GetItemFromList(parameter.HasItems ? parameter.Items! : new Dictionary<long, SelectOption>(), false);
            if (key.HasValue)
            {
                return await Create((uint)key.Value);
            }
            return null;
        }

        private async Task<ISolutionItem?> Create(uint key)
        {
            var data = await tableDataProvider.Load(definition.Id, key);
                
            if (data == null)
                return null;
                
            if (data.TableDefinition.IsMultiRecord)
                return new DatabaseTableSolutionItem(key, false, definition.Id);
            else
            {
                if (data.Entities.Count == 0)
                    return null; 
                    
                if (!data.Entities[0].ExistInDatabase)
                {
                    if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Entity doesn't exist in database")
                        .SetMainInstruction($"Entity {data.Entities[0].Key} doesn't exist in the database")
                        .SetContent(
                            "WoW Database Editor will be generating DELETE/INSERT query instead of UPDATE. Do you want to continue?")
                        .WithYesButton(true)
                        .WithNoButton(false).Build()))
                        return null;
                }
                return new DatabaseTableSolutionItem(data.Entities[0].Key, data.Entities[0].ExistInDatabase, definition.Id);
            }
        }

        public Task<ISolutionItem?> CreateRelatedSolutionItem(RelatedSolutionItem related)
        {
            if (definition.Picker == "GossipMenuParameter" &&
                related.Type == RelatedSolutionItem.RelatedType.CreatureEntry)
            {
                var template = databaseProvider.GetCreatureTemplate((uint)related.Entry);
                if (template == null || template.GossipMenuId == 0)
                    return Task.FromResult<ISolutionItem?>(null);
                return Create(template.GossipMenuId);
            }
            return Create((uint)related.Entry);
        }

        public bool CanCreatedRelatedSolutionItem(RelatedSolutionItem related)
        {
            if (related.Type == RelatedSolutionItem.RelatedType.CreatureEntry &&
                definition.Picker == "GossipMenuParameter")
            {
                var template = databaseProvider.GetCreatureTemplate((uint)related.Entry);
                return template != null && template.GossipMenuId != 0;
            }
            
            return related.Type == RelatedSolutionItem.RelatedType.CreatureEntry &&
                   definition.Picker == "CreatureParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.GameobjectEntry &&
                   definition.Picker == "GameobjectParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.GossipMenu &&
                   definition.Picker == "GossipMenuParameter" ||
                   related.Type == RelatedSolutionItem.RelatedType.QuestEntry &&
                   definition.Picker == "QuestParameter";
        }

        public Task<ISolutionItem?> CreateSolutionItem(long number)
        {
            return Create((uint)number);
        }

        public string ParameterName => definition.Picker;
    }
}