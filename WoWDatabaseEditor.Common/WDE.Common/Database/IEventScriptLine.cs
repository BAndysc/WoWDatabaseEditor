namespace WDE.Common.Database;

public enum EventScriptType
{
    Event,
    Spell,
    Waypoint
}

public interface IEventScriptLine
{
    EventScriptType Type { get; }
    uint Id { get; }
    uint? EffectIndex { get; }
    uint Delay { get; }
    uint Command { get; }
    uint DataLong1 { get; }
    uint DataLong2 { get; }
    int DataInt { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
    float O { get; }
    string Comment { get; }
}