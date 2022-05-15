using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class Emote
{
    public readonly uint Id;
    public readonly string Name;
    public readonly uint AnimId;
    public readonly uint Flags;
    public readonly EmoteType Type;
    public readonly uint Param;
    public readonly uint Sound;

    public Emote(IDbcIterator iterator)
    {
        Id = iterator.GetUInt(0);
        Name = iterator.GetString(1);
        AnimId = iterator.GetUInt(2);
        Flags = iterator.GetUInt(3);
        Type = (EmoteType)iterator.GetUInt(4);
        Param = iterator.GetUInt(5);
        Sound = iterator.GetUInt(6);
    }
}