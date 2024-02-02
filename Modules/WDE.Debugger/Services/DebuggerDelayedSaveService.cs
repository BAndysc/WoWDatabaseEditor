using System;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.Debugger.Services;

[AutoRegister]
[SingleInstance]
internal class DebuggerDelayedSaveService : IDebuggerDelayedSaveService
{
    private readonly IMainThread mainThread;

    public DebuggerDelayedSaveService(IMainThread mainThread)
    {
        this.mainThread = mainThread;
    }

    private IDisposable? saveDisposable;

    public void ScheduleSave(Action save)
    {
        saveDisposable?.Dispose();
        saveDisposable = mainThread.Delay(save, TimeSpan.FromSeconds(5));
    }
}