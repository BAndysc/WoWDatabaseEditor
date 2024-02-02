using System;

namespace WDE.Debugger.Services;

internal interface IDebuggerDelayedSaveService
{
    void ScheduleSave(Action save);
}