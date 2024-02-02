using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Debugging;

[UniqueProvider]
public interface IDebuggerService
{
    DebugPointId CreateDebugPoint(IDebugPointSource source, IDebugPointPayload payload);
    void ScheduleRemoveDebugPoint(DebugPointId id);
    Task RemoveDebugPointAsync(DebugPointId id, bool remoteAlreadyRemoved = false);
    Task ClearAllDebugPoints();

    // todo: this searches linearly, it's not efficient, but I don't expect more than ~100 debug points at this pooint
    bool TryFindDebugPoint<T>(Func<T, bool> matcher, out DebugPointId id) where T : IDebugPointPayload;
    Task RemoveIfAsync<T>(Func<T, bool> matcher, bool remoteAlreadyRemoved = false) where T : IDebugPointPayload;

    bool TryGetPayload<T>(DebugPointId id, out T payload) where T : IDebugPointPayload;
    void SetLog(DebugPointId point, string? path);
    /// <summary>
    /// Enabled is a state controlled by the user
    /// </summary>
    void SetEnabled(DebugPointId point, bool enabled);
    /// <summary>
    /// Activated is a state controlled by the editor - when there is no document associated with the debug point,
    /// it's not activated
    /// </summary>
    void SetActivated(DebugPointId point, bool activated);
    void SetSuspendExecution(DebugPointId point, bool suspend);
    void SetGenerateStacktrace(DebugPointId point, bool generate);
    void SetPayload(DebugPointId id, IDebugPointPayload payload);
    void SetLogCondition(DebugPointId point, string? condition);
    void ForceSetSynchronized(DebugPointId point);
    void ForceSetPending(DebugPointId point);

    string? GetLogFormat(DebugPointId point);
    string? GetLogCondition(DebugPointId point);
    bool GetActivated(DebugPointId point);
    bool GetEnabled(DebugPointId point);
    bool GetSuspendExecution(DebugPointId point);
    bool GetGenerateStacktrace(DebugPointId point);
    bool IsBreakpointHit(DebugPointId point);
    IDebugPointSource GetSource(DebugPointId point);

    bool HasDebugPoint(DebugPointId id);
    Task Synchronize(DebugPointId id);
    BreakpointState GetState(DebugPointId breakpoint);
    bool IsConnected { get; }

    IReadOnlyList<IDebugPointSource> Sources { get; }
    IEnumerable<DebugPointId> DebugPoints { get; }

    event Action<DebugPointId> DebugPointAdded;
    event Action<DebugPointId> DebugPointRemoving;
    event Action<DebugPointId> DebugPointRemoved;
    event Action<DebugPointId> DebugPointChanged;
}