using System;
using System.Threading.Tasks;
using WDE.Common.Debugging;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Debugging;

public interface IScriptBreakpoints : IAsyncDisposable
{
    bool IsConnected { get; }
    Task<DebugPointId> AddBreakpoint(SmartBaseElement element, SmartBreakpointFlags flags, string? log, bool dontSynchronize = false);
    Task RemoveBreakpoint(SmartBaseElement element);
    DebugPointId? GetBreakpoint(SmartBaseElement element);
    event Action BreakpointsModified;
    bool IsDisabled(DebugPointId breakpoint);
    bool IsHit(DebugPointId breakpoint);
    bool IsDeactivated(DebugPointId breakpoint);
    bool IsSuspendExecution(DebugPointId breakpoint);
    SmartBaseElement? GetElement(DebugPointId breakpoint);
    BreakpointState GetState(DebugPointId breakpoint);
    Task ResyncBreakpoints();
    Task FetchBreakpoints();
    Task RemoveAll();
    Task Synchronize(SmartBaseElement element);
}