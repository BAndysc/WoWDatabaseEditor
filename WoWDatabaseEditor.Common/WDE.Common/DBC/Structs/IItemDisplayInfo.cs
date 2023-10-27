namespace WDE.Common.DBC.Structs;

public interface IItemDisplayInfo
{
    uint Id { get; }
    string? InventoryIconPath { get; } // Pre legion

    bool UsesFileId => InventoryIconPath == null;
    bool UsesFilePath => InventoryIconPath != null;
}

public class ItemDisplayInfoEntry : IItemDisplayInfo
{
    public uint Id { get; init; }
    public uint? InventoryIconFileDataId { get; init; }
    public string? InventoryIconPath { get; init; }
}