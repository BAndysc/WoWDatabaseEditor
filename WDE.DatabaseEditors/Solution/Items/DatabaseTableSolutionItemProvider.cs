using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution.Items
{
    [AutoRegister]
    public class DatabaseTableSolutionItemProviderProvider : ISolutionItemProviderProvider
    {
        private readonly ITableDefinitionProvider definitionProvider;
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;

        public DatabaseTableSolutionItemProviderProvider(ITableDefinitionProvider definitionProvider,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider,
            IParameterFactory parameterFactory)
        {
            this.definitionProvider = definitionProvider;
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
        }
        
        public IEnumerable<ISolutionItemProvider> Provide()
        {
            foreach (var definition in definitionProvider.Definitions)
            {
                yield return new DatabaseTableSolutionItemProvider(definition, tableDataProvider, itemFromListProvider, parameterFactory);
            }
        }
    }

    internal class DatabaseTableSolutionItemProvider : ISolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;
        private readonly DatabaseTableDefinitionJson definition;
        private readonly ImageUri itemIcon;


        internal DatabaseTableSolutionItemProvider(DatabaseTableDefinitionJson definition,
            IDatabaseTableDataProvider tableDataProvider, 
            IItemFromListProvider itemFromListProvider, 
            IParameterFactory parameterFactory)
        {
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
            this.definition = definition;
            this.itemIcon = new ImageUri($"Resources/SmartScriptGeneric.png");
        }

        public string GetName() => definition.Name;

        public ImageUri GetImage() => itemIcon;

        public string GetDescription() => definition.Description;
        
        public string GetGroupName() => "Database tables";

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var parameter = parameterFactory.Factory(definition.Picker);
            var key = await itemFromListProvider.GetItemFromList(parameter.HasItems ? parameter.Items : new Dictionary<long, SelectOption>(), false);
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load(definition.TableName, (uint)key.Value);
                if (data != null)
                    return new DatabaseTableSolutionItem((uint)key.Value, definition.TableName);
            }

            return null;
        }
    }
}