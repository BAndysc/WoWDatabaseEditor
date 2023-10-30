using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class ItemAppearance
{
    public readonly uint Id;
    public readonly uint DisplayId;
    public readonly FileId? InventoryIcon;

    public ItemAppearance(IWdcIterator row)
    {
        Id = (uint)row.Id;
        DisplayId = row.GetUInt("ItemDisplayInfoID");
        InventoryIcon = row.GetUInt("DefaultIconFileDataID");
    }
}