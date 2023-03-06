using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class MapEntry : IMap
{
    public uint Id { get; init; }
    public string Directory { get; init; } = "";
    public string Name { get; init; } = "";
    public InstanceType Type { get; init; }
}