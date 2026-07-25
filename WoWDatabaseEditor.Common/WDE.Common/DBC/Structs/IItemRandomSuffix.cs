namespace WDE.Common.DBC.Structs;

public interface IItemRandomSuffix
{
    uint Id { get; }
    string Name { get; }
    EnchanmentArray Enchantments { get; }
}