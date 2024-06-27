using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoZeroSharp;

public static class ReaderExtensions
{
    public static unsafe Utf8String ReadUtf8String(this ref ProtoReader reader, ref ArenaAllocator memory)
    {
        var utf8Bytes = reader.ReadBytes();
        var outputMemory = memory.TakeContiguousSpan(utf8Bytes.Length);
        utf8Bytes.CopyTo(outputMemory);
        var data = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(outputMemory));
        return new Utf8String(utf8Bytes.Length, data);
    }

    public static unsafe UnmanagedArray<byte> ReadBytesArray(this ref ProtoReader reader, ref ArenaAllocator memory)
    {
        var bytes = reader.ReadBytes();
        var outputMemory = memory.TakeContiguousSpan(bytes.Length);
        bytes.CopyTo(outputMemory);
        var data = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(outputMemory));
        return new UnmanagedArray<byte>(bytes.Length, data);
    }
}