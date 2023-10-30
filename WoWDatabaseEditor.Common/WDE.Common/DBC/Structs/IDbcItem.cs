namespace WDE.Common.DBC.Structs;

public interface IDbcItem
{
    uint Id { get; }
    uint? InventoryIconFileDataId { get; } // Legion+
    uint DisplayInfoId { get; }
    
    IItemDisplayInfo? DisplayInfo { get; set; }
    
    bool UsesFileId => InventoryIconFileDataId.HasValue;
    bool UsesFilePath => !InventoryIconFileDataId.HasValue;
}

public class DbcItemEntry : IDbcItem
{
    public uint Id { get; init; }
    public uint? InventoryIconFileDataId { get; init; }
    public uint DisplayInfoId { get; init; }
    
    public IItemDisplayInfo? DisplayInfo { get; set; }
}

public interface IItemAppearance
{
    public uint Id { get; }
    
    public uint InventoryIconFileDataId { get; }
}

public class ItemAppearanceEntry : IItemAppearance
{
    public uint Id { get; init; }
    public uint InventoryIconFileDataId { get; init; }
}

public interface IItemModifiedAppearance
{
    public uint ItemId { get; }
    
    public uint ItemAppearanceId { get; }
    
    public IItemAppearance? ItemAppearance { get; set; }
}

public class ItemModifiedAppearanceEntry : IItemModifiedAppearance
{
    public uint ItemId { get; init; }
    public uint ItemAppearanceId { get; init; }
    
    public IItemAppearance? ItemAppearance { get; set; }
}