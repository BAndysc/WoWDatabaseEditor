using System;

namespace WDE.DbcStore.FastReader;

public static class BitStreamUtils
{
    public static int ReadInt32(ReadOnlySpan<byte> bytes, int bitOffset, int bitSize)
    {
        if (bitSize > 32)
            throw new Exception("Out of range bit size");

        var result = 0;
        var totalBitsRead = 0;

        while (bitSize > 0)
        {
            var b = bytes[bitOffset / 8];
            var skipBits = bitOffset % 8;
            var readBits = Math.Min(8 - skipBits, bitSize);

            var mask = (1 << readBits) - 1;
            var value = (b >> skipBits) & mask;

            result |= value << totalBitsRead;
            totalBitsRead += readBits;
            bitOffset += readBits;
            bitSize -= readBits;
        }

        return result;
    }
}