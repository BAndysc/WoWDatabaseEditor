using System.Collections.Generic;
using WDE.MPQ.Parsing;

namespace WDE.Common.MPQ
{
    public interface IMpqArchiveDetails
    {
        ArchiveHeader ArchiveHeader { get; }
        IMpqHashTable HashTable { get; }
        IList<BlockTableEntry> BlockTable { get; }

        int SectorSize { get; }
    }
    public interface IMpqHashTable
    {
        HashTableEntry? FindEntry(ulong hashA, ulong hashB);
    }

}