using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidObject
{
    public readonly int Id;
    public readonly short LiquidTypeId;
    
    public LiquidObject(IWdcIterator dbcIterator)
    {
        Id = dbcIterator.Id;
        LiquidTypeId = dbcIterator.GetShort("LiquidTypeID");
    }
    
    public LiquidObject(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetInt(0);
        LiquidTypeId = (short)dbcIterator.GetInt(3);
    }
}