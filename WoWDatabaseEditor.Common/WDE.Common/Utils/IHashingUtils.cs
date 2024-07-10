using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WDE.Module.Attributes;

namespace WDE.Common.Utils;

[System.Runtime.CompilerServices.InlineArray(16)]
public struct FixedByteArray16
{
    public byte element;
}

public struct MD5Hash
{
    public FixedByteArray16 array;

    public override bool Equals(object? obj)
    {
        if (obj is MD5Hash other)
            return this == other;
        return false;
    }

    public static bool operator ==(MD5Hash a, MD5Hash b)
    {
        for (int i = 0; i < 16; ++i)
        {
            if (a.array[i] != b.array[i])
                return false;
        }
        return true;
    }

    public static bool operator !=(MD5Hash a, MD5Hash b)
    {
        return !(a == b);
    }

    public ReadOnlySpan<byte> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<FixedByteArray16, byte>(ref Unsafe.AsRef(in array)), 16);

    public override int GetHashCode()
    {
        var a = array[0] | (array[1] << 8) | (array[2] << 16) | (array[3] << 24);
        var b = array[4] | (array[5] << 8) | (array[6] << 16) | (array[7] << 24);
        var c = array[8] | (array[9] << 8) | (array[10] << 16) | (array[11] << 24);
        var d = array[12] | (array[13] << 8) | (array[14] << 16) | (array[15] << 24);
        return a ^ b ^ c ^ d;
    }

    public byte this[int i]
    {
        get { return array[i]; }
        set { array[i] = value; }
    }
}

[UniqueProvider]
public interface IHashingUtils
{
    MD5Hash ComputeMD5(ReadOnlySpan<byte> data);
    MD5Hash ComputeMD5(FileInfo file);
}