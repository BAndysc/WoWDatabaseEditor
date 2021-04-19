using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase, IDocument
    {
        private INativeTextDocument code;

        private SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IStatusBar statusBar, 
            IDatabaseProvider databaseProvider,
            ITaskRunner taskRunner, 
            INativeTextDocument sql)
        {
            Code = sql;
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        await mySqlExecutor.ExecuteSql(Code.ToString());
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
                    });
            }, () => databaseProvider.IsConnected);
            IsLoading = false;
            Save = new DelegateCommand(() =>
            {
                ExecuteSql.Execute(null);
            });
        }

        public SqlEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IStatusBar statusBar,
            IDatabaseProvider databaseProvider, 
            ITaskRunner taskRunner, 
            ISolutionItemSqlGeneratorRegistry sqlGeneratorsRegistry,
            INativeTextDocument sql,
            MetaSolutionSQL item) : this(mySqlExecutor, statusBar, databaseProvider, taskRunner, sql)
        {
            IsLoading = true;
            taskRunner.ScheduleTask("Generating SQL",
                async () =>
                {
                    string sql = await sqlGeneratorsRegistry.GenerateSql(item.ItemToGenerate);
                    Code.FromString(sql);
                    IsLoading = false;
                });
        }

        public INativeTextDocument Code
        {
            get => code;
            set => SetProperty(ref code, value);
        }

        public ICommand ExecuteSql { get; }

        public void Dispose()
        {
        }

        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }
        public string Title { get; } = "SQL Output";
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified { get; } = false;
        public IHistoryManager History { get; } = null;
    }
}
