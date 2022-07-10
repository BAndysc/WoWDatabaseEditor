using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WoWDatabaseEditorCore.Services.ProblemsTool;
using WoWDatabaseEditorCore.Services.ServerExecutable;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public partial class StatusBar : ObservableBase, IStatusBar
    {
        private readonly TasksViewModel tasksViewModel;
        private readonly IMainThread mainThread;
        private readonly IPersonalGuidRangeService guidRangeService;
        private readonly IServerExecutableService serverExecutableService;
        private readonly Lazy<IClipboardService> clipboardService;
        private readonly Lazy<IMessageBoxService> messageBoxService;

        public IServerExecutableService ServerExecutableService => serverExecutableService;
        
        public StatusBar(Lazy<IDocumentManager> documentManager,
            TasksViewModel tasksViewModel, 
            IEventAggregator eventAggregator,
            IMainThread mainThread,
            IPersonalGuidRangeService guidRangeService,
            IServerExecutableService serverExecutableService,
            Lazy<IClipboardService> clipboardService,
            Lazy<IMessageBoxService> messageBoxService)
        {
            this.tasksViewModel = tasksViewModel;
            this.mainThread = mainThread;
            this.guidRangeService = guidRangeService;
            this.serverExecutableService = serverExecutableService;
            this.clipboardService = clipboardService;
            this.messageBoxService = messageBoxService;

            AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(() =>
            {
                var problemsViewModel = documentManager.Value.GetTool<ProblemsViewModel>();
                Link(problemsViewModel, t => t.TotalProblems, () => TotalProblems);
            }, true));
            
            OpenProblemTool = new DelegateCommand(() => documentManager.Value.OpenTool<ProblemsViewModel>());

            CopyNextCreatureGuidCommand = GenerateGuidCommand(GuidType.Creature);
            CopyNextGameobjectGuidCommand = GenerateGuidCommand(GuidType.GameObject);
            CopyCreatureGuidRangeCommand = GenerateGuidRangeCommand(GuidType.Creature, () => creatureGuidCount);
            CopyGameobjectGuidRangeCommand = GenerateGuidRangeCommand(GuidType.GameObject, () => gameobjectGuidCount);
            On(() => CreatureGuidCount, _ => CopyCreatureGuidRangeCommand.RaiseCanExecuteChanged());
            On(() => GameobjectGuidCount, _ => CopyGameobjectGuidRangeCommand.RaiseCanExecuteChanged());
        }
        
        private AsyncAutoCommand GenerateGuidCommand(GuidType type)
        {
            return new AsyncAutoCommand(async () =>
            {
                var guid = await guidRangeService.GetNextGuidOrShowError(type, messageBoxService.Value);
                if (guid.HasValue)
                {
                    clipboardService.Value.SetText(guid.Value.ToString());
                    PublishNotification(new PlainNotification(NotificationType.Info, "Copied " + guid.Value + $" to your clipboard ({type})"));
                }
            });
        }
        
        private AsyncAutoCommand GenerateGuidRangeCommand(GuidType type, Func<uint> getter)
        {
            return new AsyncAutoCommand(async () =>
            {
                var count = getter();
                var guid = await guidRangeService.GetNextGuidRangeOrShowError(type, count, messageBoxService.Value);
                if (guid.HasValue)
                {
                    clipboardService.Value.SetText(guid.Value.ToString());
                    PublishNotification(new PlainNotification(NotificationType.Info, "Copied " + guid.Value + $" to your clipboard ({type}). You have {count} consecutive guids"));
                }
            }, () => getter() > 0);
        }

        public ICommand CopyNextCreatureGuidCommand { get; }
        
        public ICommand CopyNextGameobjectGuidCommand { get; }
        
        public AsyncAutoCommand CopyCreatureGuidRangeCommand { get; }
        
        public AsyncAutoCommand CopyGameobjectGuidRangeCommand { get; }

        [Notify] private uint creatureGuidCount = 1;
        
        [Notify] private uint gameobjectGuidCount = 1;
        
        public ICommand OpenProblemTool { get; }

        public int TotalProblems { get; set; }
        
        public TasksViewModel TasksViewModel => tasksViewModel;
        
        private INotification? currentNotification;

        public INotification? CurrentNotification
        {
            get => currentNotification;
            private set => SetProperty(ref currentNotification, value);
        }

        public void PublishNotification(INotification notification)
        {
            mainThread.Dispatch(() =>
            {
                var time = DateTime.Now.ToString("T");
                CurrentNotification = new PlainNotification(notification.Type,
                    $"[{time}] {notification.Message}",
                    notification.ClickCommand);
            });
        }
    }
}