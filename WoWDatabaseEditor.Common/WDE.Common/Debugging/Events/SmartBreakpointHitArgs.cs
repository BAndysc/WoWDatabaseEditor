namespace WDE.Common.Debugging;

public class SmartBreakpointHitArgs
{
    public int EntryOrGuid { get; init; }
    public int SourceScriptType { get; init; }
    public int EventId { get; init; }
    public bool IsSource { get; init; }
    public string[]? Arguments { get; init; }
    public string[]? StringArguments { get; init; }
    public string? Source { get; init; }
    public string? Invoker { get; init; }
}