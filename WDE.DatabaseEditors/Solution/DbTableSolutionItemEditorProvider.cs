using System;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbTableSolutionItemEditorProvider : ISolutionItemEditorProvider<DbEditorsSolutionItem>
    {
        private readonly IDbEditorTableDataProvider tableDataProvider;
        private readonly IContainerProvider containerRegistry;
        private readonly IDbTableDefinitionProvider tableDefinitionProvider;
        
        public DbTableSolutionItemEditorProvider(IDbEditorTableDataProvider tableDataProvider, 
            IContainerProvider containerRegistry, IDbTableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDataProvider = tableDataProvider;
            this.containerRegistry = containerRegistry;
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public IDocument GetEditor(DbEditorsSolutionItem item)
        {
            var definition = tableDefinitionProvider.GetDefinition(item.TableId);
            if (definition == null)
                throw new Exception("Cannot find table editor for table: " + item.TableId);

            if (definition.IsMultiRecord)
                return containerRegistry.Resolve<MultiRecordDbTableEditorViewModel>(
                    (typeof(DbEditorsSolutionItem), item));
            
            return containerRegistry.Resolve<TemplateDbTableEditorViewModel>((typeof(DbEditorsSolutionItem), item));
        }
    }
}