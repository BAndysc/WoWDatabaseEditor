using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Debugging;
using WDE.Common.Services;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Debugging;

public class ScriptBreakpoints : IScriptBreakpoints, System.IAsyncDisposable
{
    private readonly SmartScriptType type;
    private readonly SmartScript script;
    private readonly IScriptDebugger scriptDebugger;
    private readonly IDebuggerService debugger;
    private readonly int entryOrGuid;

    private Dictionary<SmartBaseElement, DebugPointId> breakpoints = new();
    private Dictionary<DebugPointId, SmartBaseElement> breakpointToElement = new();

    public ScriptBreakpoints(SmartScript script,
        IScriptDebugger scriptDebugger,
        IDebuggerService debugger)
    {
        this.script = script;
        this.scriptDebugger = scriptDebugger;
        this.entryOrGuid = script.EntryOrGuid;
        this.type = script.SourceType;
        this.debugger = debugger;
        debugger.DebugPointChanged += OnDebugPointChanged;
        debugger.DebugPointRemoving += OnDebugPointRemoved;
    }

    public async ValueTask DisposeAsync()
    {
        debugger.DebugPointChanged -= OnDebugPointChanged;
        debugger.DebugPointRemoving -= OnDebugPointRemoved;
        List<Task> tasks = new();
        foreach (var (element, pointId) in breakpoints)
        {
            debugger.SetActivated(pointId, false);
            tasks.Add(debugger.Synchronize(pointId));
        }
        await Task.WhenAll(tasks);
        breakpoints.Clear();
        breakpointToElement.Clear();
    }

    public async Task FetchBreakpoints()
    {
        IEnumerable<(SavedScriptBreakpoint, DebugPointId)> debugPoints
            = scriptDebugger.GetDebugPoints(entryOrGuid, type);
        List<int> savedTimedActionListIds = script.Events
            .SelectMany(e => e.Actions)
            .Where(a => a.SavedTimedActionListId.HasValue)
            .Select(a => a.SavedTimedActionListId!.Value)
            .Distinct()
            .ToList();

        foreach (var savedTimedActionListId in savedTimedActionListIds)
        {
            debugPoints = debugPoints.Concat(scriptDebugger.GetDebugPoints(savedTimedActionListId, SmartScriptType.TimedActionList));
        }

        var debugPointsDict = debugPoints
            .Where(x => x.Item1.EventId >= 0)
            .ToDictionary(p => (p.Item1.EntryOrGuid, p.Item1.ScriptType, p.Item1.EventId, p.Item1.BreakpointType), p => p.Item2);

        void TryMatchBreakpoint(SmartBaseElement element, SmartBreakpointType breakpointType)
        {
            int entryOrGuid = this.entryOrGuid;
            SmartScriptType scriptType = this.type;
            if (element.SavedTimedActionListId.HasValue)
            {
                entryOrGuid = element.SavedTimedActionListId.Value;
                scriptType = SmartScriptType.TimedActionList;
            }

            if (element.SavedDatabaseEventId.HasValue)
            {
                if (debugPointsDict.TryGetValue((entryOrGuid, scriptType, element.SavedDatabaseEventId.Value, breakpointType),
                        out var pointId))
                {
                    breakpoints[element] = pointId;
                    breakpointToElement[pointId] = element;
                    scriptDebugger.SetPayloadElement(pointId, element);
                    debugger.SetActivated(pointId, true);
                }
            }
        }

        foreach (var e in script.Events)
        {
            TryMatchBreakpoint(e, SmartBreakpointType.Event);

            foreach (var a in e.Actions)
            {
                TryMatchBreakpoint(a, SmartBreakpointType.Action);
                TryMatchBreakpoint(a.Source, SmartBreakpointType.Source);
                TryMatchBreakpoint(a.Target, SmartBreakpointType.Target);
            }
        }

        BreakpointsModified?.Invoke();

        await Task.WhenAll(breakpointToElement.Keys.Select(breakpoint => debugger.Synchronize(breakpoint)));
    }

    private void OnDebugPointRemoved(DebugPointId obj)
    {
        if (breakpointToElement.TryGetValue(obj, out var element))
        {
            breakpoints.Remove(element);
            breakpointToElement.Remove(obj);
            BreakpointsModified?.Invoke();
        }
    }

    private void OnDebugPointChanged(DebugPointId id)
    {
        if (breakpoints.ContainsValue(id))
            BreakpointsModified?.Invoke();
    }

    public bool IsConnected => debugger.IsConnected;

    public async Task<DebugPointId> AddBreakpoint(SmartBaseElement element, SmartBreakpointFlags flags, string? log, bool dontSynchronize)
    {
        var debugId = scriptDebugger.CreateDebugPoint(element, flags, log);
        breakpoints[element] = debugId;
        breakpointToElement[debugId] = element;
        BreakpointsModified?.Invoke();
        if (!dontSynchronize)
            await debugger.Synchronize(debugId);
        return debugId;
    }

    public async Task RemoveBreakpoint(SmartBaseElement element)
    {
        if (breakpoints.TryGetValue(element, out var debugId))
        {
            await debugger.RemoveDebugPointAsync(debugId);
        }
    }

    public DebugPointId? GetBreakpoint(SmartBaseElement element)
    {
        if (breakpoints.TryGetValue(element, out var debugPointId))
            return debugPointId;
        return null;
    }

    public event Action? BreakpointsModified;

    public bool IsDisabled(DebugPointId breakpoint) => !debugger.GetEnabled(breakpoint);

    public bool IsHit(DebugPointId breakpoint) => debugger.IsBreakpointHit(breakpoint);

    public bool IsDeactivated(DebugPointId breakpoint) => !debugger.GetActivated(breakpoint);

    public bool IsSuspendExecution(DebugPointId breakpoint)
    {
        return debugger.GetSuspendExecution(breakpoint);
    }

    public SmartBaseElement? GetElement(DebugPointId breakpoint)
    {
        if (breakpointToElement.TryGetValue(breakpoint, out var element))
            return element;
        return null;
    }

    public BreakpointState GetState(DebugPointId breakpoint)
    {
        return debugger.GetState(breakpoint);
    }

    public async Task ResyncBreakpoints()
    {
        try
        {
            await scriptDebugger.RemoveBreakpoints(entryOrGuid, type);
            foreach (var (element, pointId) in breakpoints)
                debugger.ForceSetPending(pointId);
            await Task.WhenAll(breakpoints.Select(pair => debugger.Synchronize(pair.Value)));
        }
        catch (DebuggingFeaturesDisabledException)
        {
            if (breakpoints.Count > 0)
                throw;
        }
    }

    public async Task RemoveAll()
    {
        await Task.WhenAll(breakpoints.Select(pair => debugger.RemoveDebugPointAsync(pair.Value)));

        if (breakpoints.Count != 0)
        {
            LOG.LogError($"This is not expected? Still has {breakpoints.Count} breakpoints, but they were expected to be deleted in the callback from RemoveDebugPointAsync");
        }
        breakpoints.Clear();
    }

    public async Task Synchronize(SmartBaseElement element)
    {
        if (breakpoints.TryGetValue(element, out var pointId))
        {
            await debugger.Synchronize(pointId);
        }
    }
}