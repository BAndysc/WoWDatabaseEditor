using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.Sessions.Sessions
{
    [AutoRegister]
    [SingleInstance]
    public class SessionServiceViewModel : HistoryHandler, ISessionServiceViewModel, INotifyPropertyChanged
    {
        private ISessionService SessionService { get; }
        private readonly IInputBoxService inputBoxService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IWindowManager windowManager;

        public SessionServiceViewModel(ISessionService sessionService,
            IInputBoxService inputBoxService,
            IMessageBoxService messageBoxService,
            IWindowManager windowManager,
            Lazy<IDocumentManager> documentManager,
            ITextDocumentService textDocumentService,
            IHistoryManager historyManager)
        {
            this.SessionService = sessionService;
            this.inputBoxService = inputBoxService;
            this.messageBoxService = messageBoxService;
            this.windowManager = windowManager;
            this.History = historyManager;
            History.AddHandler(this);

            Undo = new DelegateCommand(() => History.Undo(), () => History.CanUndo).ObservesProperty(() =>
                History.CanUndo);
            Redo = new DelegateCommand(() => History.Redo(), () => History.CanRedo).ObservesProperty(() =>
                History.CanRedo);
            
            NewSessionCommand = new AsyncAutoCommand(NewSession);
            
            var save = new AsyncAutoCommand(SaveCurrent, () => sessionService.IsNonEmpty);
            SaveCurrentCurrent = save;

            var forget = new AsyncAutoCommand(ForgetCurrent, () => sessionService.IsOpened);
            ForgetCurrentCurrent = forget;

            DeleteItem = new DelegateCommand<ISolutionItem>(sessionService.RemoveItem, _ => sessionService.IsOpened).ObservesProperty(() => this.SessionService.IsOpened);

            var preview = new DelegateCommand(() =>
            {
                var sql = sessionService.GenerateCurrentQuery();
                if (sql == null)
                    return;
                documentManager.Value.OpenDocument(textDocumentService.CreateDocument( $"Preview of " + sessionService.CurrentSession!.Name, sql, "sql"));
            }, () => sessionService.IsNonEmpty);
            PreviewCurrentCommand = preview;
            
            sessionService.ToObservable(o => o.IsNonEmpty)
                .SubscribeAction(_ =>
                {
                    preview.RaiseCanExecuteChanged();
                    forget.RaiseCanExecuteChanged();
                    save.RaiseCanExecuteChanged();
                });

            History.ToObservable(h => h.IsSaved)
                .SubscribeAction(_ =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsModified)));
                });
        }

        public ICommand NewSessionCommand { get; }
        public ICommand SaveCurrentCurrent { get; }
        public ICommand ForgetCurrentCurrent { get; }
        public ICommand PreviewCurrentCommand { get; }
        public DelegateCommand<ISolutionItem> DeleteItem { get; }
        
        public async Task NewSession()
        {
            var name = await inputBoxService.GetString("New session name",
                "Please provide session name. It is only used for your developing purposes.", "New session name");
            if (!string.IsNullOrEmpty(name))
                SessionService.BeginSession(name);
        }

        public async Task SaveCurrent()
        {
            if (!SessionService.IsOpened)
                return;
            
            var fileName = await windowManager.ShowSaveFileDialog("Sql file|sql", SessionService.CurrentSession!.Name + ".sql");

            if (!string.IsNullOrEmpty(fileName))
            {
                SessionService.Finalize(fileName);

                if (SessionService.DeleteOnSave == false)
                    return;
                
                if (SessionService.DeleteOnSave == true ||
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Information)
                    .SetTitle("Removing session")
                    .SetMainInstruction("Do you want to forget current session?")
                    .SetContent(
                        "Do you want to delete recently saved session?\n\nTip: You can configure in the settings if you always/never want to forget the session")
                    .WithYesButton(true)
                    .WithNoButton(false)
                    .Build()))
                {
                    SessionService.ForgetCurrent();
                }
            }
        }

        public async Task ForgetCurrent()
        {
            if (!SessionService.IsOpened)
                return;
            
            if (SessionService.IsNonEmpty && await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Removing session")
                .SetMainInstruction("You are about to remove the current session")
                .SetContent("Do you want to continue? Just in case, the session will be kept for 14 days, you can restore it any time in that time.")
                .WithYesButton(false)
                .WithCancelButton(true)
                .Build()))
                return;

            var current = SessionService.CurrentSession;
            if (current != null)
            {
                SessionService.ForgetCurrent();
                PushAction(new HistoryActionForgetCurrent(SessionService, current));   
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ICommand Undo { get; }
        public ICommand Redo { get; }
        public IHistoryManager History { get; }
        public bool IsModified => !History.IsSaved;
    }

    internal class HistoryActionForgetCurrent : IHistoryAction
    {
        private readonly ISessionService sessionService;
        private readonly IEditorSessionStub stub;

        public HistoryActionForgetCurrent(ISessionService sessionService, IEditorSessionStub stub)
        {
            this.sessionService = sessionService;
            this.stub = stub;
        }
        
        public void Undo()
        {
            sessionService.RestoreSession(stub);
        }

        public void Redo()
        {
            sessionService.ForgetSession(stub);
        }

        public string GetDescription() => "Removed session: " + stub.Name;
    }
}