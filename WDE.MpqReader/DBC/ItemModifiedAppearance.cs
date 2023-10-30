using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemModifiedAppearance
{
    public readonly uint Id;
    public readonly uint ItemId;
    public readonly uint ItemAppearanceId;

    public ItemModifiedAppearance(IWdcIterator row)
    {
        Id = (uint)row.Id;
        ItemId = row.GetUInt("ItemID");
        ItemAppearanceId = row.GetUInt("ItemAppearanceID");
    }
}