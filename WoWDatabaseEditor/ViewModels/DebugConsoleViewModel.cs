using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WoWDatabaseEditorCore.Services.DebugConsole;

namespace WoWDatabaseEditorCore.ViewModels
{
    [AutoRegister]
    public class DebugConsoleViewModel : ObservableBase, IDocument
    {
        private readonly IDebugConsole debugConsole;
        public INativeTextDocument ConsoleLog { get; }

        public DebugConsoleViewModel(IDebugConsole debugConsole, INativeTextDocument textDocument)
        {
            this.debugConsole = debugConsole;
            ConsoleLog = textDocument;
            textDocument.FromString(debugConsole.Log);
            debugConsole.WrittenText += DebugConsoleOnWrittenText;
            CloseCommand = new AsyncCommand(async () =>
            {
                debugConsole.WrittenText -= DebugConsoleOnWrittenText;
            });
        }

        private void DebugConsoleOnWrittenText()
        {
            ConsoleLog.FromString(debugConsole.Log);
        }

        public string Title => "Debug console";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }
}