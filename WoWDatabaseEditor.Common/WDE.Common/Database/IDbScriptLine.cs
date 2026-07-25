namespace WDE.Common.Database
{
    /// <summary>
    /// One row of any dbscripts_on_* table (all 10 share the identical column schema).
    /// </summary>
    public interface IDbScriptLine
    {
        uint Id { get; }
        uint Delay { get; }
        uint Priority { get; }
        uint Command { get; }
        uint DataLong { get; }
        uint DataLong2 { get; }
        uint DataLong3 { get; }
        uint BuddyEntry { get; }
        uint SearchRadius { get; }
        uint DataFlags { get; }
        int DataInt { get; }
        int DataInt2 { get; }
        int DataInt3 { get; }
        int DataInt4 { get; }
        float DataFloat { get; }
        float X { get; }
        float Y { get; }
        float Z { get; }
        float O { get; }
        float Speed { get; }
        uint ConditionId { get; }
        string? Comments { get; }
    }
}
