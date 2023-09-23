using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IGarrMissionStore
{
    IReadOnlyList<IGarrMission> Missions { get; }
    IGarrMission? GetMissionById(int id);
}