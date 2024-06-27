using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ProtoZeroSharp;

[DebuggerTypeProxy(typeof(Utf8StringDebugView))]
public readonly unsafe struct Utf8String
{
    public readonly int BytesLength;
    private readonly byte* Data;

    public Utf8String(int bytesLength, byte* data)
    {
        BytesLength = bytesLength;
        Data = data;
    }

    public Span<byte> Span => Data == null ? throw new NullReferenceException("Trying to get Span of an unallocated Utf8String") : new Span<byte>(Data, BytesLength);

    public override string? ToString() => Data == null ? null : Encoding.UTF8.GetString(Data, BytesLength);

    public static Utf8String CreateFromString(string s, ref ArenaAllocator memory)
    {
        var bytesLength = Encoding.UTF8.GetByteCount(s);
        var span = memory.TakeContiguousSpan(bytesLength);
        Encoding.UTF8.GetBytes(s, span);
        return new Utf8String(bytesLength, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)));
    }

    public bool Equals(Utf8String other)
    {
        return BytesLength == other.BytesLength && Span.SequenceEqual(other.Span);
    }

    public override bool Equals(object obj) => obj is Utf8String other && Equals(other);

    public override int GetHashCode()
    {
        return HashCode.Combine(BytesLength, BytesLength > 0 ? Data[0] : 0,
            BytesLength > 1 ? Data[1] : 0,
            BytesLength > 2 ? Data[2] : 0,
            BytesLength > 3 ? Data[3] : 0,
            BytesLength > 4 ? Data[4] : 0,
            BytesLength > 5 ? Data[5] : 0);
    }

    public static bool operator ==(Utf8String left, Utf8String right) => left.Equals(right);

    public static bool operator !=(Utf8String left, Utf8String right) => !left.Equals(right);
}