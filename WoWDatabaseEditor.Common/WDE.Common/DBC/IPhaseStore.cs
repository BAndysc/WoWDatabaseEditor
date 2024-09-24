using System.Collections.Generic;
using WDE.Common.DBC.Structs;
using WDE.Module.Attributes;

namespace WDE.Common.DBC;

[UniqueProvider]
public interface IPhaseStore
{
    IReadOnlyList<PhaseXPhaseGroup> PhaseXPhaseGroups { get; }
    PhaseXPhaseGroup? GetPhaseXPhaseGroupById(int id);
}