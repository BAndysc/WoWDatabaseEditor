using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "game_event_creature")]
public class MySqlGameEventCreature : IGameEventCreature
{
    [PrimaryKey]
    [Column(Name = "eventEntry")]
    public short Event { get; set; }

    public uint CreatureEntry { get; set; }

    [PrimaryKey]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    public ushort ActualEventEntry => Event >= 0 ? (ushort)Event : (ushort)-Event;
    public GameEventSpawnMode Mode => Event < 0 ? GameEventSpawnMode.RemoveOnEvent : GameEventSpawnMode.SpawnOnEvent;
}