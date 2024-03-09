using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Debugging;
using WDE.Module.Attributes;

namespace LoaderAvalonia.Web;

[AutoRegister]
[SingleInstance]
public class NullDebuggerService : IDebuggerService
{
    public DebugPointId CreateDebugPoint(IDebugPointSource source, IDebugPointPayload payload) => default;

    public void ScheduleRemoveDebugPoint(DebugPointId id) { }

    public async Task RemoveDebugPointAsync(DebugPointId id, bool remoteAlreadyRemoved = false) { }

    public async Task ClearAllDebugPoints() { }

    public bool TryFindDebugPoint<T>(Func<T, bool> matcher, out DebugPointId id) where T : IDebugPointPayload
    {
        id = default;
        return false;
    }

    public async Task RemoveIfAsync<T>(Func<T, bool> matcher, bool remoteAlreadyRemoved = false) where T : IDebugPointPayload { }

    public bool TryGetPayload<T>(DebugPointId id, out T payload) where T : IDebugPointPayload
    {
        payload = default!;
        return false;
    }

    public void SetLog(DebugPointId point, string? path) { }

    public void SetEnabled(DebugPointId point, bool enabled) { }

    public void SetActivated(DebugPointId point, bool activated) { }

    public void SetSuspendExecution(DebugPointId point, bool suspend) { }

    public void SetGenerateStacktrace(DebugPointId point, bool generate) { }

    public void SetPayload(DebugPointId id, IDebugPointPayload payload) { }

    public void SetLogCondition(DebugPointId point, string? condition) { }

    public void ForceSetSynchronized(DebugPointId point) { }

    public void ForceSetPending(DebugPointId point) { }

    public string? GetLogFormat(DebugPointId point) => null;

    public string? GetLogCondition(DebugPointId point) => null;

    public bool GetActivated(DebugPointId point) => false;

    public bool GetEnabled(DebugPointId point) => false;

    public bool GetSuspendExecution(DebugPointId point) => false;

    public bool GetGenerateStacktrace(DebugPointId point) => false;

    public bool IsBreakpointHit(DebugPointId point) => false;

    public IDebugPointSource GetSource(DebugPointId point) => null!;

    public bool HasDebugPoint(DebugPointId id) => false;

    public async Task Synchronize(DebugPointId id) { }

    public BreakpointState GetState(DebugPointId breakpoint) => BreakpointState.Pending;

    public bool IsConnected => false;
    public IReadOnlyList<IDebugPointSource> Sources { get; set; } = Array.Empty<IDebugPointSource>();
    public IEnumerable<DebugPointId> DebugPoints { get; set; } = Array.Empty<DebugPointId>();
    public event Action<DebugPointId>? DebugPointAdded;
    public event Action<DebugPointId>? DebugPointRemoving;
    public event Action<DebugPointId>? DebugPointRemoved;
    public event Action<DebugPointId>? DebugPointChanged;
}