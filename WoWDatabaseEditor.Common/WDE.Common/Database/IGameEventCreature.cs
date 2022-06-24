namespace WDE.Common.Database;

public interface IGameEventCreature
{
    public short Event { get; }
    public uint Guid { get; }

    public ushort ActualEventEntry { get; }
    public GameEventSpawnMode Mode { get; }
}

public interface IGameEventGameObject
{
    public short Event { get; set; }
    public uint Guid { get; }
    
    public ushort ActualEventEntry { get; }
    public GameEventSpawnMode Mode { get; }
}

public enum GameEventSpawnMode
{
    SpawnOnEvent,
    RemoveOnEvent
}