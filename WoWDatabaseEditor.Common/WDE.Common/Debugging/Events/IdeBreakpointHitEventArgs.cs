using System;

namespace WDE.Common.Debugging;

public class IdeBreakpointHitEventArgs : EventArgs
{
    public string? Language { get; init; }
    public string? FileName { get; init; }
    public uint LineNumber { get; init; }
    public string? ModuleName { get; init; }
    public string? FunctionName { get; init; }
    public string? ReturnType { get; init; }
    public string? Line { get; init; }
    public SmartBreakpointHitArgs? SmartBreakpointHitArgs { get; init; }
    public ConditionBreakpointHitArgs? ConditionBreakpointHitArgs { get; init; }

    // mutable
    public DebugPointId HitDebugPoint { get; set; }
}