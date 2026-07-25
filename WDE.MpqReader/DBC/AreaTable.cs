using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ProtoZeroSharp;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public ref struct AreaTable
{
    public readonly uint Id;
    public readonly uint MapId;
    public readonly uint ParentAreaId;
    public readonly uint AreaBit;
    public readonly uint Flags;
    public readonly Utf8NativeString Name;

    public AreaTable(IDbcIterator dbcIterator, GameFilesVersion version, ref ArenaAllocator allocator)
    {
        Id = dbcIterator.GetUInt(0);
        MapId = dbcIterator.GetUInt(1);
        ParentAreaId = dbcIterator.GetUInt(2);
        AreaBit = dbcIterator.GetUInt(3);
        Flags = dbcIterator.GetUInt(4);
        if (version <= GameFilesVersion.Cataclysm_4_3_4)
            Name = allocator.AllocString(dbcIterator.GetUtf8String(11));
        else
            Name = allocator.AllocString(dbcIterator.GetUtf8String(13));
    }
    
    public AreaTable(IWdcIterator dbcIterator, ref ArenaAllocator allocator)
    {
        Id = (uint)dbcIterator.Id;
        MapId = dbcIterator.GetUShort("ContinentID");
        ParentAreaId = dbcIterator.GetUShort("ParentAreaID");
        AreaBit = dbcIterator.GetUShort("AreaBit");
        Flags = (uint)dbcIterator.GetInt("Flags", 0);
        Name = allocator.AllocString(dbcIterator.GetString("AreaName_lang"));
    }
}

internal static class Extensions
{
    public static unsafe Utf8NativeString AllocString(this ref ArenaAllocator arenaAllocator, ReadOnlySpan<byte> str)
    {
        if (str.IsEmpty)
        {
            return Utf8NativeString.Null;
        }
        var span = arenaAllocator.ReserveContiguousSpan(str.Length + 1);
        arenaAllocator.MoveForward(str.Length + 1);
        str.CopyTo(span);
        span[str.Length] = 0;

        return new Utf8NativeString((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), str.Length);
    }

    public static unsafe Utf8NativeString AllocString(this ref ArenaAllocator arenaAllocator, string str)
    {
        if (str.Length == 0)
        {
            return Utf8NativeString.Null;
        }

        var asUtf8 = Encoding.UTF8.GetBytes(str);
        var span = arenaAllocator.ReserveContiguousSpan(asUtf8.Length + 1);
        arenaAllocator.MoveForward(asUtf8.Length + 1);
        asUtf8.CopyTo(span);
        span[asUtf8.Length] = 0;

        return new Utf8NativeString((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), asUtf8.Length);
    }
}