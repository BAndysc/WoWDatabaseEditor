using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ProtoZeroSharp;

namespace WDE.PacketViewer.Utils;

public static class UnmanagedExtensions
{
    public static unsafe Pointer<T> Alloc<T>(this ref ArenaAllocator allocator) where T : unmanaged
    {
        var data = allocator.TakeContiguousSpan(sizeof(T));
        return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
    }

    public static bool TryGetValue<T>(this UnmanagedMap<Utf8String, T> map, string key, out T found) where T : unmanaged
    {
        map.GetUnderlyingArrays(out var keys, out var values);
        for (int i = 0; i < map.Length; ++i)
        {
            if (AsciiStringEquals(keys[i], key))
            {
                found = values[i];
                return true;
            }
        }

        found = default;
        return false;
    }

    private static bool AsciiStringEquals(Utf8String utf8, string s)
    {
        var utf8Bytes = utf8.Span;
        if (utf8.BytesLength == s.Length)
        {
            for (int i = 0; i < utf8.BytesLength; ++i)
            {
                if (!char.IsAscii((char)utf8Bytes[i]) || !char.IsAscii(s[i]))
                    return false;

                if (utf8Bytes[i] != s[i])
                    return false;
            }

            return true;
        }
        return false;
    }

    private static bool Utf8StringEquals(Utf8String utf8, string s)
    {
        var utf8Bytes = utf8.Span;
        if (utf8.BytesLength == s.Length)
        {
            for (int i = 0; i < utf8.BytesLength; ++i)
            {
                if (!char.IsAscii((char)utf8Bytes[i]) || !char.IsAscii(s[i]))
                    goto slowpath;

                if (utf8Bytes[i] != s[i])
                    return false;
            }

            return true;
        }

        slowpath:
        Span<byte> sAsUtf8 = stackalloc byte[utf8.BytesLength];
        try
        {
            if (Encoding.UTF8.GetBytes(s, sAsUtf8) != sAsUtf8.Length)
                return false;
        }
        catch (Exception)
        {
            return false; // can happen when buffer to small, i.e. strings not equal.
        }

        return utf8Bytes.SequenceEqual(sAsUtf8); //this is not precisely correct. Technically same char can be encoded by different code points. But this method is used only for ascii anyways.
    }
}