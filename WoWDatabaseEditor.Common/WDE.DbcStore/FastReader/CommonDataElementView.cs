using System;

namespace WDE.DbcStore.FastReader;

public ref struct CommonDataElementView
{
    private ReadOnlySpan<byte> mem;

    public CommonDataElementView(ReadOnlySpan<byte> mem)
    {
        this.mem = mem;
    }

    public bool IsValid => mem != ReadOnlySpan<byte>.Empty && mem.Length >= 8;
    
    public unsafe uint RecordId
    {
        get
        {
            fixed (byte* ptr = mem)
                return *(uint*)ptr;
        }
    }
    

    public unsafe uint Value
    {
        get
        {
            fixed (byte* ptr = mem)
                return *(uint*)(ptr + 4);
        }
    }
}