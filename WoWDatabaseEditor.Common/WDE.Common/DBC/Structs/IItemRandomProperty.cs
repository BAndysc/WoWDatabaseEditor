namespace WDE.Common.DBC.Structs;

public interface IItemRandomProperty
{
    uint Id { get; }
    string Name { get; }
    EnchanmentArray Enchantments { get; }
}

[System.Runtime.CompilerServices.InlineArray(5)]
public struct EnchanmentArray
{
    private int id;
}