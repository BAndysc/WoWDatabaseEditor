using System;

namespace WDE.DbcStore.FastReader;

// these functions are 2-3 times faster than BitConverter, really
internal static class SpanUtils
{
    public static unsafe int ReadInt(this ReadOnlySpan<byte> span)
    {
        fixed (byte* ptr = span)
            return *(int*)ptr;
    }
    
    public static unsafe uint ReadUInt(this ReadOnlySpan<byte> span)
    {
        fixed (byte* ptr = span)
            return *(uint*)ptr;
    }
    
    public static unsafe short ReadShort(this ReadOnlySpan<byte> span)
    {
        fixed (byte* ptr = span)
            return *(short*)ptr;
    }
    
    public static unsafe ushort ReadUShort(this ReadOnlySpan<byte> span)
    {
        fixed (byte* ptr = span)
            return *(ushort*)ptr;
    }
}