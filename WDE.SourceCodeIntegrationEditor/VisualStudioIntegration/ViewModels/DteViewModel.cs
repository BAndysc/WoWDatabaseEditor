using System.Threading.Tasks;
using System.Windows.Input;
using EnvDTE;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Debugging;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.ViewModels;

internal partial class DteViewModel : ObservableBase
{
    private readonly IDTE model;

    [Notify] private bool isRunning;
    [Notify] private bool isBreakpoint;

    [Notify] private string ideName = "(...loading)";
    public AsyncAutoCommand DebugUnpauseCommand { get; }
    public AsyncAutoCommand DebugPauseCommand { get; }

    [Notify] private IdeBreakpointHitEventArgs? currentBreakpoint;

    public DteViewModel(IDTE model)
    {
        this.model = model;
        DebugUnpauseCommand = new AsyncAutoCommand(model.DebugUnpause, () => IsBreakpoint);
        DebugPauseCommand = new AsyncAutoCommand(model.DebugPause, () => IsRunning && !IsBreakpoint);

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
    }

    private async Task UpdateData()
    {
        IdeName = await model.GetIdeName();
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