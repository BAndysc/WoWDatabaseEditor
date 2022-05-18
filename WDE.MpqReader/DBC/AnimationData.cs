using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

[Flags]
public enum AnimationDataFlags
{
    None = 0,
    FallbackPlayBackwards = 0x10,
    FallbackHoldsLastFrame = 0x20	
}

public class AnimationData
{
    public readonly uint Id;
    public readonly AnimationDataFlags Flags;
    public readonly uint Fallback;
        
    public AnimationData(IDbcIterator iterator)
    {
        Id = iterator.GetUInt(0);
        Flags = (AnimationDataFlags)iterator.GetUInt(4);
        Fallback = iterator.GetUInt(5);
    }
}