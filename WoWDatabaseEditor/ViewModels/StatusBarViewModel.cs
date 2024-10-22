using System;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common.Debugging;
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

namespace WoWDatabaseEditorCore.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class StatusBarViewModel : ObservableBase
{
    public IConnectionsStatusBarItem Connections { get; }
    private readonly TasksViewModel tasksViewModel;
    private readonly IMainThread mainThread;
    private readonly IPersonalGuidRangeService guidRangeService;
    private readonly IServerExecutableService serverExecutableService;
    private readonly IDebuggerService debuggerService;
    private readonly Lazy<IClipboardService> clipboardService;
    private readonly Lazy<IMessageBoxService> messageBoxService;
    private readonly Lazy<IDebuggerInspectorService> debuggerInspector;
    private readonly IStatusBar statusBar;

    public IServerExecutableService ServerExecutableService => serverExecutableService;

    public StatusBarViewModel(Lazy<IDocumentManager> documentManager,
        TasksViewModel tasksViewModel,
        IEventAggregator eventAggregator,
        IMainThread mainThread,
        IPersonalGuidRangeService guidRangeService,
        IServerExecutableService serverExecutableService,
        IConnectionsStatusBarItem connectionsStatusBarItem,
        IDebuggerService debuggerService,
        Lazy<IClipboardService> clipboardService,
        Lazy<IMessageBoxService> messageBoxService,
        Lazy<IDebuggerInspectorService> debuggerInspector,
        IStatusBar statusBar)
    {
        Connections = connectionsStatusBarItem;
        this.tasksViewModel = tasksViewModel;
        this.mainThread = mainThread;
        this.guidRangeService = guidRangeService;
        this.serverExecutableService = serverExecutableService;
        this.debuggerService = debuggerService;
        this.clipboardService = clipboardService;
        this.messageBoxService = messageBoxService;
        this.debuggerInspector = debuggerInspector;
        this.statusBar = statusBar;

        AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(() =>
        {
            var problemsViewModel = documentManager.Value.GetTool<ProblemsViewModel>();
            Link(problemsViewModel, t => t.TotalProblems, () => TotalProblems);
        }, true));

        OpenProblemTool = new DelegateCommand(() => documentManager.Value.OpenTool<ProblemsViewModel>());

        OpenBreakpointsWindow = new DelegateCommand(() =>
        {
            this.debuggerInspector.Value.OpenInspector();
        });

        SupportsBreakpoints = debuggerService.Sources.Count > 0;
        CopyNextCreatureGuidCommand = GenerateGuidCommand(GuidType.Creature);
        CopyNextGameobjectGuidCommand = GenerateGuidCommand(GuidType.GameObject);
        CopyCreatureGuidRangeCommand = GenerateGuidRangeCommand(GuidType.Creature, () => creatureGuidCount);
        CopyGameobjectGuidRangeCommand = GenerateGuidRangeCommand(GuidType.GameObject, () => gameobjectGuidCount);
        On<uint>(() => CreatureGuidCount, _ => CopyCreatureGuidRangeCommand.RaiseCanExecuteChanged());
        On<uint>(() => GameobjectGuidCount, _ => CopyGameobjectGuidRangeCommand.RaiseCanExecuteChanged());
        On(this.statusBar, x => x.CurrentNotification, _ => RaisePropertyChanged(nameof(CurrentNotification)));

        this.debuggerService.DebugPointAdded += id => RaisePropertyChanged(nameof(TotalBreakpoints));
        this.debuggerService.DebugPointRemoved += id => RaisePropertyChanged(nameof(TotalBreakpoints));
        this.debuggerService.DebugPointChanged += id => RaisePropertyChanged(nameof(TotalBreakpoints));
    }

    private AsyncAutoCommand GenerateGuidCommand(GuidType type)
    {
        return new AsyncAutoCommand(async () =>
        {
            var guid = await guidRangeService.GetNextGuidOrShowError(type, messageBoxService.Value);
            if (guid.HasValue)
            {
                clipboardService.Value.SetText(guid.Value.ToString());
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Copied " + guid.Value + $" to your clipboard ({type})"));
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
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, "Copied " + guid.Value + $" to your clipboard ({type}). You have {count} consecutive guids"));
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

    public ICommand OpenBreakpointsWindow { get; }

    public int TotalProblems { get; set; }

    public int TotalBreakpoints => debuggerService.DebugPoints.Count();

    public bool SupportsBreakpoints { get; }

    public TasksViewModel TasksViewModel => tasksViewModel;

    public INotification? CurrentNotification => statusBar.CurrentNotification;
}