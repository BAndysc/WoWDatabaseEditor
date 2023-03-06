namespace WDE.Common.DBC.Structs;

public interface IMap
{
    uint Id { get; }
    string Directory { get; }
    string Name { get; }
    InstanceType Type { get; }
}

public enum InstanceType
{
    RegularMap = 0,
    Dungeon = 1,
    Raid = 2,
    PvP_Battlefield = 2,
    Arena = 3,
    Scenario = 5
}