using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Debugging;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Utils;
using WDE.Debugger.ViewModels.Inspector;
using WDE.Module.Attributes;

namespace WDE.Debugger.Services;

[SingleInstance]
[AutoRegister]
internal class DebuggerInspectorService : IDebuggerInspectorService, IToolMenuItem
{
    private readonly Func<DebugPointsInspectorViewModel> viewModelFactory;
    private readonly IWindowManager windowManager;
    private IAbstractWindowView? currentWindow;

    public DebuggerInspectorService(Func<DebugPointsInspectorViewModel> viewModelFactory,
        IWindowManager windowManager)
    {
        this.viewModelFactory = viewModelFactory;
        this.windowManager = windowManager;

        ItemCommand = new DelegateCommand(OpenInspector);
    }

    public void OpenInspector()
    {
        if (currentWindow != null)
        {
            currentWindow.Activate();
        }
        else
        {
            async Task ShowWindow()
            {
                var vm = viewModelFactory();
                currentWindow = windowManager.ShowWindow(vm, out var task);
                await task;
                currentWindow = null;
            }

            ShowWindow().ListenErrors();
        }
    }

    public string ItemName => "Breakpoints";
    public ICommand ItemCommand { get; }
    public MenuShortcut? Shortcut => new MenuShortcut("Control+B");
}