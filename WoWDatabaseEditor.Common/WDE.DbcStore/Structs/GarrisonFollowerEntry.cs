using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class GarrisonFollowerEntry : IGarrisonFollower
{
    public uint Id { get; init; }
    public uint HordeCreatureEntry { get; init; }
    public uint AllianceCreatureEntry { get; init; }
}