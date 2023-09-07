using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class CharShipmentEntry : ICharShipment
{
    public uint Id { get; init; }
    public uint TreasureId { get; init; }
    public uint SpellId { get; init; }
    public uint DummyItemId { get; init; }
    public uint OnCompleteSpellId { get; init; }
    public uint ContainerId { get; init; }
    public uint GarrisonFollowerId { get; init; }
    
    public ICharShipmentContainer? Container { get; set; }
    public IGarrisonFollower? Follower { get; set; }
}