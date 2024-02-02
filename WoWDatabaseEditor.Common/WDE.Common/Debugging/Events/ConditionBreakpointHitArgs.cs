using WDE.Common.Database;

namespace WDE.Common.Debugging;

public class ConditionBreakpointHitArgs
{
    public int SourceType { get; init; }
    public int SourceEntry { get; init; }
    public int SourceGroup { get; init; }
    public int SourceId { get; init; }
    public AbstractCondition Condition { get; init; } = new();
}