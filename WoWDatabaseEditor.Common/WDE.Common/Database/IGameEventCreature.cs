namespace WDE.Common.Database;

public interface IGameEventCreature
{
    public short Event { get; }
    public uint CreatureEntry { get; }
    public uint Guid { get; }

    public ushort ActualEventEntry { get; }
    public GameEventSpawnMode Mode { get; }
}

public interface IGameEventGameObject
{
    public short Event { get; set; }
    public uint GameObjectEntry { get; }
    public uint Guid { get; }
    
    public ushort ActualEventEntry { get; }
    public GameEventSpawnMode Mode { get; }
}

public static class GameEventExtensions
{
    public static SpawnKey Key(this IGameEventGameObject that)
    {
        return new SpawnKey(that.GameObjectEntry, that.Guid);
    }
    
    public static SpawnKey Key(this IGameEventCreature that)
    {
        return new SpawnKey(that.CreatureEntry, that.Guid);
    }
}

public enum GameEventSpawnMode
{
    SpawnOnEvent,
    RemoveOnEvent
}