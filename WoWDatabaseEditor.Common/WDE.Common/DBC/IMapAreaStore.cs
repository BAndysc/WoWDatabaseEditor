using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IMapAreaStore
{
    IReadOnlyList<IArea> Areas { get; }
    IArea? GetAreaById(uint id);
    
    IReadOnlyList<IMap> Maps { get; }
    IMap? GetMapById(uint id);
}