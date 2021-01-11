using Prism.Commands;
using System;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.SQLEditor.ViewModels;
using WDE.SQLEditor.Views;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSqlSolutionItemEditorProvider : ISolutionItemEditorProvider<MetaSolutionSQL>
    {
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry;
        private readonly IMySqlExecutor _mySqlExecutor;
        private readonly IStatusBar _statusBar;

        public MetaSqlSolutionItemEditorProvider(
            Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry,
            IMySqlExecutor mySqlExecutor,
            IStatusBar statusBar)
        {
            this.sqlGeneratorsRegistry = sqlGeneratorsRegistry;
            _mySqlExecutor = mySqlExecutor;
            _statusBar = statusBar;
        }

        public IDocument GetEditor(MetaSolutionSQL item)
        {
            return new SqlEditorViewModel(_mySqlExecutor, _statusBar, sqlGeneratorsRegistry.Value.GenerateSql(item as MetaSolutionSQL));
        }
    }
}
