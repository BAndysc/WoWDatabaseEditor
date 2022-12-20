using System.Runtime.InteropServices;

namespace WDE.MPQ.Parsing
{
    // note: there is a 1-byte gap between the Platform and FileBlockIndex fields. 
    //	That is why we use LayoutKind.Explicit
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct HashTableEntry
    {
        [FieldOffset(0x00)] public readonly uint FilePathHashA;

        [FieldOffset(0x04)] public readonly uint FilePathHashB;

        [FieldOffset(0x08)] public readonly ushort Language;

        [FieldOffset(0x0a)] public readonly byte Platform;

        [FieldOffset(0x0c)] public readonly uint FileBlockIndex;

        private const uint EmptyMarker = 0xffffffff;

        public bool IsEmpty => FilePathHashA == EmptyMarker && FilePathHashB == EmptyMarker;
    }
}