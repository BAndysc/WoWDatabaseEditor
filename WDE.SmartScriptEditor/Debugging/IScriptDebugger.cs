using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Debugging;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Debugging;

[UniqueProvider]
public interface IScriptDebugger
{
    bool IsEnabled { get; }
    DebugPointId CreateDebugPoint(SmartBaseElement element, SmartBreakpointFlags flags, string? log);
    Task RemoveBreakpoints(int entryOrGuid, SmartScriptType type);
    IReadOnlyList<(SavedScriptBreakpoint, DebugPointId)> GetDebugPoints(int entryOrGuid, SmartScriptType type);
    void SetPayloadElement(DebugPointId pointId, SmartBaseElement element);
}