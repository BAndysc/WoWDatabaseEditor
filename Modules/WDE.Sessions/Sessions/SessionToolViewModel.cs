using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Disposables;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.Sessions.Sessions
{
    [AutoRegister]
    [SingleInstance]
    public class SessionToolViewModel : ObservableBase, IFocusableTool, IUndoRedoWindow
    {
        private readonly ISessionServiceViewModel vm;
        public ISessionService SessionService { get; }

        // ReSharper disable once CollectionNeverQueried.Global
        public ObservableCollection<SolutionItemViewModel> CurrentSolutionItems { get; } = new();
        public ObservableCollection<IEditorSessionStub> Sessions { get; } = new();

        public IEditorSessionStub? SelectedSession
        {
            get => selectedSession;
            set => SetProperty(ref selectedSession, value);
        }
        
        public SolutionItemViewModel? SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem, value);
        }
        
        public ICommand NewSessionCommand { get; }
        public ICommand SaveCurrentCurrent { get; }
        public ICommand ForgetCurrentCurrent { get; }
        public ICommand PreviewCurrentCommand { get; }
        public ICommand DeleteSelectedItem { get; }
        public ICommand OpenHelpCommand { get; }
        
        private System.IDisposable? currentSessionStream;
        
        public SessionToolViewModel(ISessionService sessionService,
            IEventAggregator eventAggregator, ISolutionManager solutionManager,
            ISolutionItemIconRegistry iconRegistry, ISolutionItemNameRegistry nameRegistry,
            ISessionServiceViewModel vm, IWindowManager windowManager)
        {
            this.vm = vm;
            SessionService = sessionService;
            NewSessionCommand = vm.NewSessionCommand;
            SaveCurrentCurrent = vm.SaveCurrentCurrent;
            ForgetCurrentCurrent = vm.ForgetCurrentCurrent;
            PreviewCurrentCommand = vm.PreviewCurrentCommand;
            Undo = vm.Undo;
            Redo = vm.Redo;
            History = vm.History;

            OpenHelpCommand = new DelegateCommand(() =>
            {
                windowManager.OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/wiki/Sessions");
            });
            
            DeleteSelectedItem = new DelegateCommand(() =>
            {
                if (SelectedItem?.Item == null)
                    return;
                
                if (!vm.DeleteItem.CanExecute(SelectedItem?.Item!))
                    return;
                
                vm.DeleteItem.Execute(SelectedItem?.Item!);
            }, () => SelectedItem != null && sessionService.IsOpened)
                .ObservesProperty(() => SelectedItem)
                .ObservesProperty(() => SessionService.IsOpened);
            
            RequestOpenItemCommand = new DelegateCommand<SolutionItemViewModel>(vm =>
            {
                if (vm is { IsContainer: false })
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(vm.Item.Clone());
            });

            AutoDispose(this.ToObservable(() => SelectedSession).Skip(1)
                .SubscribeAction(session =>
                {
                    if (inHandler)
                        return;
                    
                    sessionService.Open(session);
                }));

            AutoDispose(sessionService.ToStream(sessionService)
                .SubscribeAction(ev =>
                {
                    if (ev.Type == CollectionEventType.Add)
                    {
                        Sessions.Add(ev.Item);
                    }
                    else if (ev.Type == CollectionEventType.Remove)
                    {
                        Sessions.Remove(ev.Item);
                    }
                }));
            
            AutoDispose(sessionService
                .ToObservable(o => o.CurrentSession)
                .SubscribeAction(session =>
                {
                    inHandler = true;
                    
                    CurrentSolutionItems.Clear();
                    SelectedItem = null;
                    currentSessionStream?.Dispose();
                    currentSessionStream = null;

                    if (session == null)
                    {
                        SelectedSession = null;
                        inHandler = false;
                        return;
                    }

                    SelectedSession = Sessions.FirstOrDefault(s => s.FileName == session.FileName);

                    currentSessionStream = session
                        .ToStream(session.Select(t => t.Item1))
                        .SubscribeAction(ev =>
                        {
                            if (ev.Type == CollectionEventType.Add)
                            {
                                CurrentSolutionItems.Add(new SolutionItemViewModel(iconRegistry, nameRegistry, ev.Item));
                            }
                            else if (ev.Type == CollectionEventType.Remove)
                            {
                                CurrentSolutionItems.RemoveAt(ev.Index);
                            }
                        });
                    inHandler = false;
                }));
            
            solutionManager.RefreshRequest += SolutionManagerOnRefreshRequest;
            
            AutoDispose(new ActionDisposable(() => solutionManager.RefreshRequest -= SolutionManagerOnRefreshRequest));
            AutoDispose(new ActionDisposable(() => currentSessionStream?.Dispose()));
            Watch(Sessions, s => s.Count, nameof(HasAnySessions));
            Watch(History, h => h.IsSaved, nameof(IsModified));
        }

        private void SolutionManagerOnRefreshRequest(ISolutionItem? obj)
        {
            foreach (var root in CurrentSolutionItems)
                root.Refresh();
        }

        public string Title => "Sessions";
        public string UniqueId => "tool_sessions";
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
        public bool OpenOnStart => true;
        public bool IsSelected { get; set; }

        private bool inHandler;
        private bool visibility;
        private IEditorSessionStub? selectedSession;
        private SolutionItemViewModel? selectedItem;

        public bool Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        public DelegateCommand<SolutionItemViewModel> RequestOpenItemCommand { get; set; }
        public ICommand Undo { get; }
        public ICommand Redo { get; }
        public IHistoryManager? History { get; }
        public bool IsModified => vm.IsModified;
        public bool HasAnySessions => Sessions.Count > 0;
    }
}