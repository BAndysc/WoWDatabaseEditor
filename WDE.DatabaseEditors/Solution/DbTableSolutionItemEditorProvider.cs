using System;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Solution;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DbTableSolutionItemEditorProvider : ISolutionItemEditorProvider<DbEditorsSolutionItem>
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IParameterFactory> parameterFactory;
        private readonly Func<IHistoryManager> historyCreator;
        
        public DbTableSolutionItemEditorProvider(Lazy<IItemFromListProvider> itemFromListProvider, Lazy<IParameterFactory> parameterFactory, Func<IHistoryManager> historyCreator)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
            this.historyCreator = historyCreator;
        }
        
        public IDocument GetEditor(DbEditorsSolutionItem item)
        {
            return new TemplateDbTableEditorViewModel(item, itemFromListProvider.Value, parameterFactory.Value,
                historyCreator);
        }
    }
}