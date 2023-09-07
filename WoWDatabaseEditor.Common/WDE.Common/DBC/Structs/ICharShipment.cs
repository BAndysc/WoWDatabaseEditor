namespace WDE.Common.DBC.Structs;

public interface ICharShipment
{
    uint Id { get; }
    uint TreasureId { get; }
    uint SpellId { get; }
    uint DummyItemId { get; }
    uint OnCompleteSpellId { get; }
    uint ContainerId { get; }
    uint GarrisonFollowerId { get; }
    
    ICharShipmentContainer? Container { get; }
    IGarrisonFollower? Follower { get; }
}