using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using WDE.Common.Avalonia.Debugging;
using WDE.Common.Debugging;
using WDE.Debugger.ViewModels.Inspector;
using WDE.Debugger.Views.Inspector;
using WDE.Module.Attributes;

namespace WDE.Debugger.Services;

[SingleInstance]
[AutoRegister]
internal class EditDebugPointService : IEditDebugPointService
{
    private readonly IDebuggerService debuggerService;

    public EditDebugPointService(IDebuggerService debuggerService)
    {
        this.debuggerService = debuggerService;
    }

    public async Task EditDebugPointInPopup(Control owner, DebugPointId debugPointId)
    {
        await EditDebugPointInPopup(owner, new[] {debugPointId});
    }

    public async Task EditDebugPointInPopup(Control owner, DebugPointId[] debugPoints)
    {
        var viewModel = new SelectedDebugPointViewModel(debuggerService, debugPoints);
        var popup = new EditDebugPointPopup()
        {
            DataContext = viewModel,
        };
        await popup.Open(owner);
        viewModel.Dispose();
    }
}