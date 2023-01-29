using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Tasks;
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
        private readonly IMainThread mainThread;
        public INativeTextDocument ConsoleLog { get; }
        public ICommand CrashCommand { get; }
        public ICommand CrashAsyncCommand { get; }

        public DebugConsoleViewModel(IDebugConsole debugConsole, 
            INativeTextDocument textDocument,
            IMainThread mainThread)
        {
            this.debugConsole = debugConsole;
            this.mainThread = mainThread;
            ConsoleLog = textDocument;
            textDocument.FromString(debugConsole.Log);
            debugConsole.WrittenText += DebugConsoleOnWrittenText;
            CloseCommand = new AsyncCommand(async () =>
            {
                debugConsole.WrittenText -= DebugConsoleOnWrittenText;
            });
            CrashCommand = new DelegateCommand(() =>
            {
                string x = null!;
                x.Clone();
            });
            CrashAsyncCommand = new AsyncAutoCommand(async () =>
            {
                await Task.Run(() => Thread.Sleep(500));
                string x = null!;
                x.Clone();
            });
        }

        private void DebugConsoleOnWrittenText()
        {
            mainThread.Dispatch(() =>
            {
                ConsoleLog.FromString(debugConsole.Log);
            });
        }

        public string Title => "Debug console";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
        public ImageUri? Icon => new ImageUri("Icons/document_console.png");
    }
}