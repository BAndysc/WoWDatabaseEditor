using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemAppearance
{
    public readonly uint Id;
    public readonly uint DisplayId;

    public ItemAppearance(IWdcIterator row)
    {
        Id = (uint)row.Id;
        DisplayId = row.GetUInt("ItemDisplayInfoID");
    }
}