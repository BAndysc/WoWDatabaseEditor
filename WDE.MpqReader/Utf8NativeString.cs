using System.Runtime.InteropServices;

namespace WDE.MpqReader;

public readonly unsafe struct Utf8NativeString
{
    private static byte* NullString;

    private readonly byte* ptr;
    private readonly int length;

    public Utf8NativeString(byte* ptr, int length)
    {
        this.ptr = ptr;
        this.length = length;
    }

    static Utf8NativeString()
    {
        NullString = (byte*)NativeMemory.AlignedAlloc(1, 8);
        NullString[0] = 0;
    }

    // String length excluding byte 0
    public int Length => length;
    public bool IsNull => ptr == null;

    public static Utf8NativeString Null => new Utf8NativeString(NullString, 0);

    // Utf8 string as span, excluding byte 0.
    public ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>(ptr, length);
}