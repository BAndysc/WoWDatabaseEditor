using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class CharShipmentContainerEntry : ICharShipmentContainer
{
    public uint Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
}