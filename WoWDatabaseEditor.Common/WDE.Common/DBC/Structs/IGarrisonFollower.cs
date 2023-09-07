namespace WDE.Common.DBC.Structs;

public interface IGarrisonFollower
{
    uint Id { get; }
    uint HordeCreatureEntry { get; }
    uint AllianceCreatureEntry { get; }
}