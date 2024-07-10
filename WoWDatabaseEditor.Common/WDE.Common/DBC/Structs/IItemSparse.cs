namespace WDE.Common.DBC.Structs;

public interface IItemSparse
{
    int Id { get; }
    string Name { get; }
    ushort RandomSelect { get; }
    ushort ItemRandomSuffixGroupId { get; }
}