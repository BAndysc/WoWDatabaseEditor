using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace WDE.Common.Utils;

public static class AsciiUtils
{
    // Define the constant vectors
    private static Vector128<byte> A = Vector128.Create((byte)'A');
    private static Vector128<byte> DIFF = Vector128.Create((byte)('Z' - 'A'));
    static Vector128<byte> zeroX20 = Vector128.Create((byte)0x20);
    private static Vector128<byte> zeroX80 = Vector128.Create((byte)0x80);

    // It is pretty crazy, but it actualy works! https://news.ycombinator.com/item?id=31900872
    private static Vector128<byte> ToLower8_AdvSimd(Vector128<byte> src)
    {
        // Subtract 'A' from each element to get the range shift
        var rangeshift = AdvSimd.Subtract(src, A);

        // Compare less than or equal to 'Z' - 'A' to determine if it is an uppercase letter
        var nomodify = AdvSimd.CompareGreaterThan(rangeshift, DIFF).AsByte();

        // AND NOT to create the flip mask (equivalent to AND NOT using bitwise operations)
        var flip = AdvSimd.And(AdvSimd.Not(nomodify), zeroX20);

        // XOR with the flip mask
        return AdvSimd.Xor(src, flip);
    }

    private static Vector128<byte> ToLower8_SSE2(Vector128<byte> src)
    {
        // Subtract 'A' from each element to get the range shift
        var rangeshift = Sse2.Subtract(src, A);

        // Compare less than or equal to 'Z' - 'A' to determine if it is an uppercase letter
        var nomodify = Sse2.CompareGreaterThan(Sse2.Xor(rangeshift, zeroX80).AsSByte(),
            Sse2.Xor(DIFF, zeroX80).AsSByte()).AsByte();

        // AND NOT to create the flip mask (equivalent to AND NOT using bitwise operations)
        var flip = Sse2.AndNot(nomodify, zeroX20);

        // XOR with the flip mask
        return Sse2.Xor(src, flip);

    }

    /// <summary>
    /// Extremely fast, vectorized ASCII to lowercase transformation.
    /// </summary>
    /// <param name="asciiText"></param>
    /// <param name="length"></param>
    public static unsafe void TransformLowercase(byte* asciiText, int length)
    {
        var alignedStart = (byte*)(((UIntPtr)asciiText + 15) / 16 * 16);

        while (asciiText != alignedStart)
        {
            if (*asciiText >= 'A' && *asciiText <= 'Z')
                *asciiText = (byte)(*asciiText + 32);
            asciiText++;
            length--;
        }

        int simdLength = 0;
        if (Sse2.IsSupported)
        {
            simdLength = length / 16 * 16;
            for (int i = 0; i < simdLength; i += 16)
            {
                var lowercase = Vector128.LoadAligned(asciiText + i);
                lowercase = ToLower8_SSE2(lowercase);
                *(Vector128<byte>*)(asciiText + i) = lowercase;
            }
        }
        else if (AdvSimd.IsSupported)
        {
            simdLength = length / 16 * 16;
            for (int i = 0; i < simdLength; i += 16)
            {
                var lowercase = Vector128.LoadAligned(asciiText + i);
                lowercase = ToLower8_AdvSimd(lowercase);
                *(Vector128<byte>*)(asciiText + i) = lowercase;
            }
        }

        for (int i = simdLength; i < length; i++)
        {
            if (asciiText[i] >= 'A' && asciiText[i] <= 'Z')
                asciiText[i] = (byte)(asciiText[i] + 32);
        }
    }
}