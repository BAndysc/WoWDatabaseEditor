using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public readonly struct CurrencyType
{
    public readonly uint Id;
    public readonly FileId? InventoryIcon;

    public CurrencyType(IDbcIterator row)
    {
        Id = row.GetUInt(0);
    }
    
    public CurrencyType(IWdcIterator row)
    {
        Id = (uint)row.Id;
        InventoryIcon = row.GetUInt("InventoryIconFileID");
    }
}