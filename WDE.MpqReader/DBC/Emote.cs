using ProtoZeroSharp;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public ref struct Emote
{
    public readonly uint Id;
    public readonly Utf8NativeString Name;
    public readonly uint AnimId;
    public readonly uint Flags;
    public readonly EmoteType Type;
    public readonly uint Param;
    public readonly uint Sound;

    public Emote(IDbcIterator iterator, ref ArenaAllocator allocator)
    {
        Id = iterator.GetUInt(0);
        Name = allocator.AllocString(iterator.GetUtf8String(1));
        AnimId = iterator.GetUInt(2);
        Flags = iterator.GetUInt(3);
        Type = (EmoteType)iterator.GetUInt(4);
        Param = iterator.GetUInt(5);
        Sound = iterator.GetUInt(6);
    }

    public Emote(IWdcIterator iterator, ref ArenaAllocator allocator)
    {
        Id = (uint)iterator.Id;
        Name = allocator.AllocString(iterator.GetString("EmoteSlashCommand"));
        AnimId = iterator.GetUShort("AnimID");
        Flags = iterator.GetUInt("EmoteFlags");
        Type = (EmoteType)iterator.GetByte("EmoteSpecProc");
        Param = iterator.GetUInt("EmoteSpecProcParam");
        Sound = iterator.GetUInt("EventSoundID");
    }
}
