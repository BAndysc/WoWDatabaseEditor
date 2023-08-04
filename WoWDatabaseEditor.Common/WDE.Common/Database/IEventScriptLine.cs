using System;

namespace WDE.Common.Database;

[Flags]
public enum EventScriptType
{
    Event = 1,
    Spell = 2,
    Waypoint = 4,
    Gossip = 8,
    GameObjectUse = 16,
    QuestStart = 32,
    QuestEnd = 64
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