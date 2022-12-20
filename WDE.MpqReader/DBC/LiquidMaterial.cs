using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidMaterial
{
    public readonly int Id;
    public readonly int LVF;
    
    public LiquidMaterial(IDbcIterator row)
    {
        Id = row.GetInt(0);
        LVF = row.GetInt(1);
    }
    
    public LiquidMaterial(IWdcIterator row)
    {
        Id = row.Id;
        LVF = row.GetByte("LVF");
    }
}