using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Debugging;

[UniqueProvider]
public interface ISmartScriptDebugCommandsService
{
    bool IsConnected { get; }
    Task AddBreakpoint(int entryOrGuid, SmartScriptType type, int lineId, SmartBreakpointType smartBreakpointType, SmartBreakpointFlags flags);
    Task RemoveBreakpoint(int entryOrGuid, SmartScriptType type, int lineId);
    Task RemoveBreakpoints(int entryOrGuid, SmartScriptType type);
    Task ClearAllBreakpoints();
    Task<List<SavedScriptBreakpoint>> GetBreakpoints(IEnumerable<(int entryOrGuid, SmartScriptType type)> scripts);
    Task<List<SavedScriptBreakpoint>> GetBreakpoints();
    event Action DebuggerConnected;
    event Action DebuggerDisconnected;
}