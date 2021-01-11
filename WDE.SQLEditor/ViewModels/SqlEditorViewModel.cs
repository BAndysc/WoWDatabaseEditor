using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Utils;
using IDocument = WDE.Common.Managers.IDocument;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase, IDocument
    {
        private TextDocument code;
        public TextDocument Code
        {
            get => code;
            set => SetProperty(ref code, value);
        }

        public SqlEditorViewModel(IMySqlExecutor mySqlExecutor, IStatusBar statusBar, string sql)
        {
            Code = new TextDocument(sql);
            ExecuteSql = new AsyncAutoCommand(async () =>
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
                await mySqlExecutor.ExecuteSql(Code.Text);
                statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
            }, null, e => statusBar.PublishNotification(new PlainNotification(NotificationType.Error, $"{e.Message} ({e.InnerException?.Message})")));
        }

        public void Dispose()
        {
        }

        public ICommand ExecuteSql { get; }
        
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
