using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SQLEditor.Solutions;
using WDE.SqlQueryGenerator;

namespace WDE.SQLEditor.ViewModels
{
    [AutoRegister]
    public class CustomQueryEditorViewModel : ObservableBase, ISolutionItemDocument
    {
        private INativeTextDocument code;
        public ImageUri? Icon { get; } = new("Icons/document_sql.png");

        public CustomQueryEditorViewModel(IMySqlExecutor mySqlExecutor, 
            IStatusBar statusBar, 
            IDatabaseProvider databaseProvider,
            ITaskRunner taskRunner, 
            INativeTextDocument sql,
            ISolutionManager solutionManager,
            CustomSqlSolutionItem solutionItem)
        {
            sql.FromString(solutionItem.Query ?? "");
            Code = sql;
            Title = solutionItem.Name;
            AutoDispose(sql.Length.SubscribeAction(_ => IsModified = true));
            IsModified = false;
            SolutionItem = solutionItem;
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        try
                        {
                            await mySqlExecutor.ExecuteSql(Code.ToString());
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
                        }
                        catch (Exception e)
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Failure during query execution"));
                            Console.WriteLine(e);
                        }
                    });
            }, () => databaseProvider.IsConnected);
            IsLoading = false;
            Save = new DelegateCommand(() =>
            {
                solutionItem.Query = code.ToString();
                ExecuteSql.Execute(null);
                IsModified = false;
            });
        }

        public INativeTextDocument Code
        {
            get => code;
            set => SetProperty(ref code, value);
        }

        public ICommand ExecuteSql { get; }

        private bool isLoading = false;
        private bool isModified;

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public string Title { get; }
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;

        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }

        public IHistoryManager History { get; } = null;
        public ISolutionItem SolutionItem { get; }
        public Task<IQuery> GenerateQuery()
        {
            return Task.FromResult(Queries.Raw(code.ToString()));
        }
    }
}