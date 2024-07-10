using System;
using System.Runtime.CompilerServices;

namespace ProtoZeroSharp;

internal static class VarInt
{
    internal const int MaxBytesCount = 10;

    internal static int ReadVarint(Span<byte> input, out ulong value)
    {
        if ((input[0] & 0x80) == 0)
        {
            value = input[0];
            return 1;
        }
        value = 0;
        int shift = 0;
        int index = 0;
        while (index < input.Length)
        {
            byte b = input[index++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return index;
            }
            shift += 7;
        }
        throw new FormatException("Invalid varint");
    }

    /// <summary>
    /// Writes a variable-length integer to the given output span.
    /// </summary>
    /// <param name="output">Buffer to write varint to</param>
    /// <param name="value">Value to write</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarint(Span<byte> output, ulong value)
    {
#if DEBUG
        if (output.Length < MaxBytesCount)
            throw new ArgumentException($"Output buffer must be at least {MaxBytesCount} bytes long to fit any varint");
#endif

        int index = 0;
        while (value >= 0x80)
        {
            output[index++] = (byte)(value | 0x80);
            value >>= 7;
        }
        output[index++] = (byte)value;

        return index;
    }

    /// <summary>
    /// Writes a variable-length integer to the given output span in a fixed size.
    /// It might be redundant, if the value is small, but it's useful for writing fixed-size varints.
    /// </summary>
    /// <param name="output">Buffer to write varint to</param>
    /// <param name="value">Value to write</param>
    /// <param name="bytes">Number of bytes to write</param>
    /// <returns>Number of bytes written.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarintFixedSize(Span<byte> output, ulong value, int bytes)
    {
#if DEBUG
        if (output.Length < bytes)
            throw new ArgumentException($"Output buffer must be at least {bytes} bytes long to fit the value");
#endif
        int index = 0;

        for (int i = 0; i < bytes - 1; ++i)
        {
            output[index++] = (byte)(value | 0x80);
            value >>= 7;
        }
        output[index++] = (byte)value;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarint(Span<byte> output, int value)
    {
        return WriteVarint(output, (ulong)value);
    }
}
