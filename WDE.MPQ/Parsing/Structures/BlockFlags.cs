using System;

namespace WDE.MPQ.Parsing
{
    [Flags]
    public enum BlockFlags : uint
    {
        IsFile = 0x80000000,
        HasChecksums = 0x04000000,
        IsDeletionMarker = 0x02000000,
        FileIsSingleUnit = 0x01000000,
        KeyIsAdjustedByBlockOffset = 0x00020000,
        FileIsEncrypted = 0x00010000,
        FileIsCompressed = 0x00000200,
        FileIsImploded = 0x00000100,
    }
}