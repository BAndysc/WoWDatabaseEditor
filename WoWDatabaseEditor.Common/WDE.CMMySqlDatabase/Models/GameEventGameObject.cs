using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "game_event_gameobject")]
public class GameEventGameObject : IGameEventGameObject
{
    [PrimaryKey]
    [Column(Name = "event")]
    public short Event { get; set; }

    public uint GameObjectEntry { get; set; }

    [PrimaryKey]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    public ushort ActualEventEntry => Event >= 0 ? (ushort)Event : (ushort)-Event;
    public GameEventSpawnMode Mode => Event < 0 ? GameEventSpawnMode.RemoveOnEvent : GameEventSpawnMode.SpawnOnEvent;
}