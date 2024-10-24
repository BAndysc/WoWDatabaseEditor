namespace WDE.Common.DBC.Structs;

public interface IRewardPack
{
    uint Id { get; }
    uint TreasurePickerID { get; }
}

public class RewardPack : IRewardPack
{
    public uint Id { get; init; }
    public uint TreasurePickerID { get; init; }
}