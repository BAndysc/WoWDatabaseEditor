using System;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemEditorProvider : ISolutionItemEditorProvider<DatabaseTableSolutionItem>
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IContainerProvider containerRegistry;
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        
        public DatabaseTableSolutionItemEditorProvider(IDatabaseTableDataProvider tableDataProvider, 
            IContainerProvider containerRegistry, ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDataProvider = tableDataProvider;
            this.containerRegistry = containerRegistry;
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public IDocument GetEditor(DatabaseTableSolutionItem item)
        {
            var definition = tableDefinitionProvider.GetDefinition(item.TableName);
            if (definition == null)
            {
                if (tableDefinitionProvider.CoreCompatibility(item.TableName) is { } compatibility)
                    throw new Exception("This item was created with different core compatibility mode and cannot be opened now. If you want to open the item, switch to any of those core compatibility modes: " + string.Join(", ", compatibility));

                throw new Exception("Cannot find table editor for definition " + item.TableName + ". If you think this is a bug, please report it via Help -> Report a bug");
            }
            
            if (definition.RecordMode == RecordMode.MultiRecord)
                return  containerRegistry.Resolve<MultiRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), item));
            if (definition.RecordMode == RecordMode.SingleRow)
                return  containerRegistry.Resolve<SingleRowDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), item));
            return containerRegistry.Resolve<TemplateDbTableEditorViewModel>((typeof(DatabaseTableSolutionItem), item));
        }
    }
}