using System;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.SQLEditor.ViewModels;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSqlSolutionItemEditorProvider : ISolutionItemEditorProvider<MetaSolutionSQL>
    {
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IStatusBar statusBar;
        private readonly Func<INativeTextDocument> document;
        private readonly ITaskRunner taskRunner;
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry;

        public MetaSqlSolutionItemEditorProvider(Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorsRegistry,
            Func<INativeTextDocument> document,
            IMySqlExecutor mySqlExecutor,
            IStatusBar statusBar,
            ITaskRunner taskRunner)
        {
            this.sqlGeneratorsRegistry = sqlGeneratorsRegistry;
            this.document = document;
            this.mySqlExecutor = mySqlExecutor;
            this.statusBar = statusBar;
            this.taskRunner = taskRunner;
        }

        public IDocument GetEditor(MetaSolutionSQL item)
        {
            var vm = new SqlEditorViewModel(mySqlExecutor, statusBar, taskRunner, null);
            LoadSqlDocument(vm, item);
            return vm;
        }

        private async void LoadSqlDocument(SqlEditorViewModel vm, MetaSolutionSQL item)
        {
            var doc = document();
            doc.FromString(await sqlGeneratorsRegistry.Value.GenerateSql(item));
            vm.Code = doc;
        }
    }
}