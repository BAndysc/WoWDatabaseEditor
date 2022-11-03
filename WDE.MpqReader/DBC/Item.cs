using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class Item
{
    public readonly uint Id;
    public readonly uint ClassId;
    public readonly uint DisplayId;
    public readonly uint InventoryType;
    public readonly uint SheatheType;

    public Item(IDbcIterator row)
    {
        Id = row.GetUInt(0);
        ClassId = row.GetUInt(1);
        DisplayId = row.GetUInt(5);
        InventoryType = row.GetUInt(6);
        SheatheType = row.GetUInt(7);
    }
    public Item(IWdcIterator row, ItemAppearanceStore appearanceStore)
    {
        Id = (uint)row.Id;
        ClassId = row.GetByte("ClassID");
        if (appearanceStore.TryGetValue(Id, out var displayId)) 
            DisplayId = displayId.DisplayId;  
        InventoryType = row.GetByte("InventoryType");
        SheatheType = row.GetByte("SheatheType");
    }
}