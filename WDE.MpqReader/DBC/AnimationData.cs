using WDE.Common.DBC;
using WDE.Common.MPQ;

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
        
    public AnimationData(IDbcIterator iterator, GameFilesVersion version)
    {
        Id = iterator.GetUInt(0);
        if (version == GameFilesVersion.Wrath_3_3_5a)
        {
            Flags = (AnimationDataFlags)iterator.GetUInt(4);
            Fallback = iterator.GetUInt(5);
        }
        else if (version == GameFilesVersion.Cataclysm_4_3_4 ||
                 version == GameFilesVersion.Mop_5_4_8)
        {
            Flags = (AnimationDataFlags)iterator.GetUInt(2);
            Fallback = iterator.GetUInt(3);
        }
    }
    
    public AnimationData(IWdcIterator iterator, GameFilesVersion version)
    {
        Id = (uint)iterator.Id;
        Flags = (AnimationDataFlags)iterator.GetInt("Flags");
        Fallback = iterator.GetUShort("Fallback");
    }
}