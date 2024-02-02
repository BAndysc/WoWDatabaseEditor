using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using EnvDTE;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Debugging;
using WDE.Common.Disposables;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.ViewModels;

internal partial class BreakpointHitPopupViewModel : ObservableBase
{
    private readonly IDTE model;

    [Notify] private bool showTail;

    [Notify] private bool isRunning;
    [Notify] private bool isBreakpoint;

    [Notify] private string ideName = "(...loading)";
    [Notify] private string? solutionName;
    public AsyncAutoCommand DebugUnpauseCommand { get; }
    public AsyncAutoCommand DebugPauseCommand { get; }

    [Notify] private string hitEventArgs = "";
    [Notify] private string hitEventStringArgs = "";
    [Notify] private bool hasEventArgs;
    [Notify] private string? hitModuleName;
    [Notify] private string hitFileName = "";
    [Notify] private string? source;
    [Notify] private string? invoker;

    public IAsyncCommand GoToBreakpointCommand { get; }

    public ICommand CloseCommand { get; }

    public event Action<BreakpointHitPopupViewModel>? CloseRequested;

    private IdeBreakpointHitEventArgs? currentBreakpoint;
    public IdeBreakpointHitEventArgs? CurrentBreakpoint
    {
        get => currentBreakpoint;
        set
        {
            currentBreakpoint = value;
            if (value != null)
            {
                HitModuleName = Path.GetFileName(value.ModuleName?.Replace("\\", "/"));
                HitFileName = Path.GetFileName(value.FileName?.Replace("\\", "/") ?? "") + ":" + value.LineNumber;
                HasEventArgs = value.SmartBreakpointHitArgs?.Arguments != null;
                hitEventArgs = value.SmartBreakpointHitArgs?.Arguments != null ? string.Join(", ", value.SmartBreakpointHitArgs.Arguments) : "";
                hitEventStringArgs = value.SmartBreakpointHitArgs?.StringArguments != null ? string.Join(", ", value.SmartBreakpointHitArgs.StringArguments) : "";
                source = value.SmartBreakpointHitArgs?.Source;
                invoker = value.SmartBreakpointHitArgs?.Invoker;
            }
            RaisePropertyChanged();
        }
    }

    public BreakpointHitPopupViewModel(IDTE model, IdeBreakpointHitEventArgs args)
    {
        this.model = model;
        DebugUnpauseCommand = new AsyncAutoCommand(model.DebugUnpause, () => IsBreakpoint);
        DebugPauseCommand = new AsyncAutoCommand(model.DebugPause, () => IsRunning && !IsBreakpoint);

        GoToBreakpointCommand = new AsyncAutoCommand(async () =>
        {
            if (CurrentBreakpoint != null)
                await model.GoToFile(CurrentBreakpoint.FileName!, (int)CurrentBreakpoint.LineNumber, true);
        }, () => CurrentBreakpoint?.FileName != null);
        On(() => CurrentBreakpoint, _ => GoToBreakpointCommand.RaiseCanExecuteChanged());

        CloseCommand = new DelegateCommand(() => CloseRequested?.Invoke(this));

        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsRunning) || e.PropertyName == nameof(IsBreakpoint))
            {
                DebugUnpauseCommand.RaiseCanExecuteChanged();
                DebugPauseCommand.RaiseCanExecuteChanged();
            }
        };

        UpdateData().ListenErrors();
        UpdateRunningState().ListenErrors();

        model.DebuggingEnded += OnDebuggingEnded;
        model.BreakModeEntered += OnBreakModeEntered;
        model.RunModeEntered += OnRunModeEntered;

        AutoDispose(new ActionDisposable(() =>
        {
            model.DebuggingEnded -= OnDebuggingEnded;
            model.BreakModeEntered -= OnBreakModeEntered;
            model.RunModeEntered -= OnRunModeEntered;
        }));
        CurrentBreakpoint = args;
    }

    private async Task UpdateData()
    {
        IdeName = await model.GetIdeName();
        SolutionName = Path.GetFileNameWithoutExtension((await model.GetSolutionFullPath())?.Replace('\\', '/'));
    }

    private async Task UpdateRunningState()
    {
        var mode = await model.GetDebugModeAsync();
        IsRunning = mode == dbgDebugMode.dbgRunMode || mode == dbgDebugMode.dbgBreakMode;
        IsBreakpoint = mode == dbgDebugMode.dbgBreakMode;
    }

    private void OnRunModeEntered(object? sender, dbgEventReason obj)
    {
        UpdateRunningState().ListenErrors();
        CurrentBreakpoint = null;
    }

    private void OnBreakModeEntered(object? sender, IdeBreakpointHitEventArgs obj)
    {
        UpdateRunningState().ListenErrors();
        CurrentBreakpoint = obj;
    }

    private void OnDebuggingEnded(object? sender, dbgEventReason obj)
    {
        UpdateRunningState().ListenErrors();
        CurrentBreakpoint = null;
    }
}