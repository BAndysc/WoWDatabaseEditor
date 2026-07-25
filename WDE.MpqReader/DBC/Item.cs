using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public ref struct Item
{
    public readonly uint Id;
    public readonly FileId? InventoryIcon;
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
    public unsafe Item(IWdcIterator row, ItemModifiedAppearanceStore itemModifiedAppearanceStore, ItemAppearanceStore appearanceStore)
    {
        Id = (uint)row.Id;
        InventoryIcon = row.GetUInt("IconFileDataID");
        ClassId = row.GetByte("ClassID");
        if (itemModifiedAppearanceStore.TryGetByItem(Id, out var modifiedAppearancesId) &&
            itemModifiedAppearanceStore.TryGetValue(modifiedAppearancesId, out var modifiedAppearances))
        {
            if (appearanceStore.TryGetValue(modifiedAppearances->ItemAppearanceId, out var itemAppearance))
            {
                DisplayId = itemAppearance->DisplayId;
                if (itemAppearance->InventoryIcon.HasValue &&
                    itemAppearance->InventoryIcon.Value.FileDataId > 0)
                    InventoryIcon = itemAppearance->InventoryIcon;
            }
        }
        InventoryType = row.GetByte("InventoryType");
        SheatheType = row.GetByte("SheatheType");
    }
}