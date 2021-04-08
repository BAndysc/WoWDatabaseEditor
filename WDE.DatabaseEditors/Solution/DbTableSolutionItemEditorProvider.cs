using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbTableSolutionItemEditorProvider : ISolutionItemEditorProvider<DbEditorsSolutionItem>
    {
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;
        private readonly Lazy<IContainerProvider> containerRegistry;
        private readonly Lazy<IDbTableDefinitionProvider> tableDefinitionProvider;
        
        public DbTableSolutionItemEditorProvider(Lazy<IDbEditorTableDataProvider> tableDataProvider, 
            Lazy<IContainerProvider> containerRegistry, Lazy<IDbTableDefinitionProvider> tableDefinitionProvider)
        {
            this.tableDataProvider = tableDataProvider;
            this.containerRegistry = containerRegistry;
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public IDocument GetEditor(DbEditorsSolutionItem item)
        {
            var tableName = GetTableName(item.TableContentType);

            return item.IsMultiRecord ? containerRegistry.Value.Resolve<MultiRecordDbTableEditorViewModel>(
                    (typeof(DbEditorsSolutionItem), item), (typeof(string), tableName))
                : containerRegistry.Value.Resolve<TemplateDbTableEditorViewModel>((typeof(DbEditorsSolutionItem), item),
                    (typeof(string), tableName));
        }

        private string GetTableName(DbTableContentType tableContentType)
        {
            switch (tableContentType)
            {
                case DbTableContentType.CreatureTemplate:
                    return tableDefinitionProvider.Value.GetCreatureTemplateDefinition().Name;
                case DbTableContentType.CreatureLootTemplate:
                    return tableDefinitionProvider.Value.GetCreatureLootTemplateDefinition().Name;
                case DbTableContentType.GameObjectTemplate:
                    return tableDefinitionProvider.Value.GetGameobjectTemplateDefinition().Name;
                default:
                    throw new Exception("[DbTableSolutionItemEditorProvider] not defined table content type!");
            }
        }
    }
}