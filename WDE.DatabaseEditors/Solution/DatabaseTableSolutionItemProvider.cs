using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
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

        public DatabaseTableSolutionItemProviderProvider(ITableDefinitionProvider definitionProvider,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider,
            IMessageBoxService messageBoxService,
            IParameterFactory parameterFactory)
        {
            this.definitionProvider = definitionProvider;
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
        }
        
        public IEnumerable<ISolutionItemProvider> Provide()
        {
            foreach (var definition in definitionProvider.Definitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, tableDataProvider, itemFromListProvider, messageBoxService, parameterFactory);
            }
        }
    }

    internal class DatabaseTableSolutionItemProvider : ISolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly DatabaseTableDefinitionJson definition;
        private readonly ImageUri itemIcon;
        
        internal DatabaseTableSolutionItemProvider(DatabaseTableDefinitionJson definition,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider, 
            IMessageBoxService messageBoxService,
            IParameterFactory parameterFactory)
        {
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.definition = definition;
            this.itemIcon = new ImageUri($"Resources/SmartScriptGeneric.png");
        }

        public string GetName() => definition.Name;

        public ImageUri GetImage() => itemIcon;

        public string GetDescription() => definition.Description;
        
        public string GetGroupName() => definition.GroupName ?? "Database tables";

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var parameter = parameterFactory.Factory(definition.Picker);
            var key = await itemFromListProvider.GetItemFromList(parameter.HasItems ? parameter.Items : new Dictionary<long, SelectOption>(), false);
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load(definition.Id, (uint)key.Value);
                
                if (data == null)
                    return null;
                
                if (data.TableDefinition.IsMultiRecord)
                    return new DatabaseTableSolutionItem((uint)key.Value, false, definition.Id);
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

            return null;
        }
    }
}