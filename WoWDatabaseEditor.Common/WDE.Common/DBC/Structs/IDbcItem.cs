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