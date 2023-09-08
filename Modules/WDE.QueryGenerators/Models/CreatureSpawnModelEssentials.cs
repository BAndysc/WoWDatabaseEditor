using WDE.Common.Utils;

namespace WDE.QueryGenerators.Models;

public struct CreatureSpawnModelEssentials
{
    public uint Guid { get; set; }
    public uint Entry { get; set; }
    public int Map { get; set; }
    public uint SpawnMask { get; set; }
    public uint PhaseMask { get; set; }
    public SmallReadOnlyList<uint> PhaseId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
}