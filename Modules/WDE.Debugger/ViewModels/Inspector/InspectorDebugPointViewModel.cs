using WDE.Common.Debugging;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.Debugger.ViewModels.Inspector;

internal partial class InspectorDebugPointViewModel : ObservableBase, IChildType
{
    private readonly DebugPointId id;
    private readonly IDebuggerService debuggerService;
    private readonly IDebugPointSource source;

    public string Header { get; private set; }
    public DebugPointId Id => id;
    public bool IsConnected => debuggerService.IsConnected;
    public bool IsDeactivated => !debuggerService.GetActivated(id);
    public bool SuspendExecution => debuggerService.GetSuspendExecution(id);
    public BreakpointState State => debuggerService.GetState(id);

    public bool IsEnabled
    {
        get => debuggerService.GetEnabled(id);
        set
        {
            debuggerService.SetEnabled(id, value);
            debuggerService.Synchronize(id).ListenErrors();
        }
    }

    public InspectorDebugPointViewModel(DebugPointId id, IDebuggerService debuggerService)
    {
        this.id = id;
        this.debuggerService = debuggerService;
        source = this.debuggerService.GetSource(id);
        Header = source.GenerateName(id);
    }

    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }

    public void RaiseChanged()
    {
        RaisePropertyChanged(nameof(SuspendExecution));
        RaisePropertyChanged(nameof(State));
        RaisePropertyChanged(nameof(IsConnected));
        RaisePropertyChanged(nameof(IsEnabled));
        RaisePropertyChanged(nameof(IsDeactivated));
        Header = source.GenerateName(id);
        RaisePropertyChanged(nameof(Header));
    }
}