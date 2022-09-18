using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidType
{
    public readonly uint Id;
    public readonly string Name;
    public readonly float MaxDarkenDepth;
    public readonly uint MaterialId;
    public readonly string Texture0;
    public readonly string Texture1;
    public readonly string Texture2;
    public readonly string Texture3;
    public readonly string Texture4;
    public readonly string Texture5;
    
    public LiquidType(IDbcIterator row)
    {
        Id = row.GetUInt(0);
        Name = row.GetString(1);
        MaxDarkenDepth = row.GetFloat(6);
        MaterialId = row.GetUInt(14);
        Texture0 = row.GetString(15);
        Texture1 = row.GetString(16);
        Texture2 = row.GetString(17);
        Texture3 = row.GetString(18);
        Texture4 = row.GetString(19);
        Texture5 = row.GetString(20);
    }
}

public class LiquidTypeStore : BaseDbcStore<uint, LiquidType>
{
    public LiquidTypeStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LiquidType(row);
            store[o.Id] = o;
        }
    }
}