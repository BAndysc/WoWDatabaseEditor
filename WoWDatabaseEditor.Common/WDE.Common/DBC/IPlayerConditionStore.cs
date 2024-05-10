using System.Collections.Generic;
using WDE.Common.DBC.Structs;
using WDE.Module.Attributes;

namespace WDE.Common.DBC;

[UniqueProvider]
public interface IPlayerConditionStore
{
    IReadOnlyList<IPlayerCondition> PlayerConditions { get; }
    IPlayerCondition? GetPlayerConditionById(int id);
}