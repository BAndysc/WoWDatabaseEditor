using System;
using Newtonsoft.Json.Linq;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Debugger.Services;

[AutoRegister]
[SingleInstance]
internal class DebuggerLogService : IDebuggerLogService
{
    private readonly IDebuggerService debuggerService;

    public DebuggerLogService(IDebuggerService debuggerService)
    {
        this.debuggerService = debuggerService;
    }

    public void AddLog(DebugPointId debugId, string title, JObject log)
    {
        if (!debuggerService.HasDebugPoint(debugId))
            return; // due to async nature of the logs, the debug point might be removed before the log is added, it is fine

        var logFormat = debuggerService.GetLogFormat(debugId);
        var logCondition = debuggerService.GetLogCondition(debugId);

        OnLog?.Invoke((title, logFormat, logCondition, log));
    }

    public event Action<(string title, string? logFormat, string? logCondition, JObject log)>? OnLog;
}