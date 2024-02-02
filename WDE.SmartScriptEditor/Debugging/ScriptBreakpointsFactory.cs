using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Debugging;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Debugging;

[AutoRegister]
public class ScriptBreakpointsFactory : IScriptBreakpointsFactory
{
    private readonly IScriptDebugger scriptDebugger;
    private readonly IDebuggerService debuggerService;

    public ScriptBreakpointsFactory(IScriptDebugger scriptDebugger,
        IDebuggerService debuggerService)
    {
        this.scriptDebugger = scriptDebugger;
        this.debuggerService = debuggerService;
    }

    public IScriptBreakpoints? Create(SmartScript script)
    {
        if (!scriptDebugger.IsEnabled)
            return null;

        return new ScriptBreakpoints(script, scriptDebugger, debuggerService);
    }
}

[FallbackAutoRegister]
internal class FallbackScriptDebugger : IScriptDebugger
{
    public bool IsEnabled => false;

    public DebugPointId CreateDebugPoint(SmartBaseElement element) => DebugPointId.Empty;

    public DebugPointId CreateDebugPoint(SmartBaseElement element, SmartBreakpointFlags flags, string? log) => DebugPointId.Empty;

    public Task RemoveBreakpoints(int entryOrGuid, SmartScriptType type) => Task.CompletedTask;

    public IReadOnlyList<(SavedScriptBreakpoint, DebugPointId)> GetDebugPoints(int entryOrGuid, SmartScriptType type) => Array.Empty<(SavedScriptBreakpoint, DebugPointId)>();

    public void SetPayloadElement(DebugPointId pointId, SmartBaseElement element) { }
}