using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class ItemSparse : IItemSparse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public ushort RandomSelect { get; init; }
    public ushort ItemRandomSuffixGroupId { get; init; }
}