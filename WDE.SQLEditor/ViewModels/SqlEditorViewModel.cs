using System.Windows.Input;
using Prism.Mvvm;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Utils;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase, IDocument
    {
        public string Code { get; set; }

        public SqlEditorViewModel(string sql)
        {
            Code = sql;
        }

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
