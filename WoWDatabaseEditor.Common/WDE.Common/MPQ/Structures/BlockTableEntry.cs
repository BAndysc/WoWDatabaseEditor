using System.Runtime.InteropServices;

namespace WDE.MPQ.Parsing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockTableEntry
    {
        public readonly uint BlockOffset;
        public readonly uint BlockSize;
        public readonly uint FileSize;
        public readonly BlockFlags Flags;

        public bool HasKeyAdjustedByBlockOffset => (Flags & BlockFlags.KeyIsAdjustedByBlockOffset) != 0;

        public bool IsFile => (Flags & BlockFlags.IsFile) != 0;

        public bool IsFileSingleUnit => (Flags & BlockFlags.FileIsSingleUnit) != 0;

        public bool IsCompressed => (Flags & BlockFlags.FileIsCompressed) != 0;

        public bool IsEncrypted => (Flags & BlockFlags.FileIsEncrypted) != 0;

        public bool IsImploded => (Flags & BlockFlags.FileIsImploded) != 0;
    }
}