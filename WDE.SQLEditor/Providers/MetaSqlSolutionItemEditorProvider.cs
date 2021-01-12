using System;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SQLEditor.ViewModels;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSqlSolutionItemEditorProvider : ISolutionItemEditorProvider<MetaSolutionSQL>
    {
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IStatusBar statusBar;
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry;

        public MetaSqlSolutionItemEditorProvider(Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry,
            IMySqlExecutor mySqlExecutor,
            IStatusBar statusBar)
        {
            this.sqlGeneratorsRegistry = sqlGeneratorsRegistry;
            this.mySqlExecutor = mySqlExecutor;
            this.statusBar = statusBar;
        }

        public IDocument GetEditor(MetaSolutionSQL item)
        {
            return new SqlEditorViewModel(mySqlExecutor, statusBar, sqlGeneratorsRegistry.Value.GenerateSql(item));
        }
    }
}