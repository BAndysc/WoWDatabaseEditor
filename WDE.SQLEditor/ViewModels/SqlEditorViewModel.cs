using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase, IDocument
    {
        private TextDocument code;

        public SqlEditorViewModel(IMySqlExecutor mySqlExecutor, IStatusBar statusBar, ITaskRunner taskRunner, string sql)
        {
            Code = new TextDocument(sql);
            ExecuteSql = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Executing query",
                    async () =>
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                        await mySqlExecutor.ExecuteSql(Code.Text);
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
                    });
            });
        }

        public TextDocument Code
        {
            get => code;
            set => SetProperty(ref code, value);
        }

        public ICommand ExecuteSql { get; }

        public void Dispose()
        {
        }

        public string Title { get; } = "SQL Output";
        public ICommand Undo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; } = AlwaysDisabledCommand.Command;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; } = AlwaysDisabledCommand.Command;
        public ICommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public IHistoryManager History { get; } = null;
    }
}