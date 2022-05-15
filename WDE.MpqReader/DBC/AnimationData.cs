using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AnimationData
{
    public readonly uint Id;
    public readonly uint Fallback;
        
    public AnimationData(IDbcIterator iterator)
    {
        Id = iterator.GetUInt(0);
        Fallback = iterator.GetUInt(5);
    }
}