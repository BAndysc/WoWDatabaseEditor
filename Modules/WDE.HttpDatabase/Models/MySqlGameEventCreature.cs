
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonGameEventCreature : IGameEventCreature
{
    
    
    public short Event { get; set; }

    public uint CreatureEntry { get; set; }

    
    
    public uint Guid { get; set; }

    public ushort ActualEventEntry => Event >= 0 ? (ushort)Event : (ushort)-Event;
    public GameEventSpawnMode Mode => Event < 0 ? GameEventSpawnMode.RemoveOnEvent : GameEventSpawnMode.SpawnOnEvent;
}