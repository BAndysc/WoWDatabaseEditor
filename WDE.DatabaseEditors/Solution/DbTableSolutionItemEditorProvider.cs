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
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Func<IHistoryManager> historyCreator;
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;
        private readonly Lazy<ITaskRunner> taskRunner;
        private readonly Lazy<IContainerProvider> containerRegistry;
        
        public DbTableSolutionItemEditorProvider(Lazy<IItemFromListProvider> itemFromListProvider,
            Func<IHistoryManager> historyCreator, Lazy<IDbEditorTableDataProvider> tableDataProvider,
            Lazy<ITaskRunner> taskRunner, Lazy<IContainerProvider> containerRegistry)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.historyCreator = historyCreator;
            this.tableDataProvider = tableDataProvider;
            this.taskRunner = taskRunner;
            this.containerRegistry = containerRegistry;
        }
        
        public IDocument GetEditor(DbEditorsSolutionItem item)
        {
            Func<uint, Task<IDbTableData>>? tableDataLoader = null;
            if (item.TableData == null)
                tableDataLoader = FindTableDataLoader(item.TableDataLoaderMethodName);

            return item.IsMultiRecord ? containerRegistry.Value.Resolve<MultiRecordDbTableEditorViewModel>(
                    (typeof(DbEditorsSolutionItem), item), (typeof(Func<uint, Task<IDbTableData>>), tableDataLoader))
                : containerRegistry.Value.Resolve<TemplateDbTableEditorViewModel>((typeof(DbEditorsSolutionItem), item),
                (typeof(Func<uint, Task<IDbTableData>>), tableDataLoader));
        }

        private Func<uint, Task<IDbTableData>>? FindTableDataLoader(string methodName)
        {
            // use reflection to get tableDataProvider method from solution item
            var method = tableDataProvider.Value.GetType().GetMethod(methodName);
            if (method == null)
                return null;

            return method.CreateDelegate<Func<uint, Task<IDbTableData>>>(tableDataProvider.Value);
        }
    }
}