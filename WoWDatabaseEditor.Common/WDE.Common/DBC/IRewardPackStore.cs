using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IRewardPackStore
{
    IReadOnlyList<IRewardPack> RewardPacks { get; }
    IRewardPack? GetRewardPack(uint id);
}