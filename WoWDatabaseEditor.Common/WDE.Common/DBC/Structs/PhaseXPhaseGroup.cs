using System.Collections.Generic;
using WDE.Common.Utils;

namespace WDE.Common.DBC.Structs;

public readonly struct PhaseXPhaseGroupRow
{
    public readonly int Id;
    public readonly ushort PhaseId;
    public readonly int PhaseGroupId;

    public PhaseXPhaseGroupRow(int id, ushort phaseId, int phaseGroupId)
    {
        Id = id;
        PhaseId = phaseId;
        PhaseGroupId = phaseGroupId;
    }
}

public class PhaseXPhaseGroup
{
    public readonly int PhaseGroupId;
    public readonly SmallList<ushort> Phases;

    public PhaseXPhaseGroup(int phaseGroupId, IEnumerable<ushort> phases)
    {
        PhaseGroupId = phaseGroupId;
        Phases = new SmallList<ushort>(phases);
    }
}