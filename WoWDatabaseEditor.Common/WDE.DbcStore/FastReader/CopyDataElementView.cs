using System;

namespace WDE.DbcStore.FastReader;

public ref struct CopyDataElementView
{
    private ReadOnlySpan<byte> mem;

    public CopyDataElementView(ReadOnlySpan<byte> mem)
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
    

    public unsafe uint OldRecordId
    {
        get
        {
            fixed (byte* ptr = mem)
                return *(uint*)(ptr + 4);
        }
    }
}