// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System.Collections.Generic;
using System.Linq;
using WDE.Common.MPQ;
using WDE.MPQ.Parsing;

namespace WDE.MPQ
{
    public class MpqHashTable : IMpqHashTable
    {
        public MpqHashTable(IEnumerable<HashTableEntry> entries)
        {
            Entries = entries.ToList().AsReadOnly();
        }

        public IList<HashTableEntry> Entries { get; private set; }

        public HashTableEntry? FindEntry(ulong hashA, ulong hashB)
        {
            foreach (var entry in Entries)
            {
                if (hashA == (ulong) entry.FilePathHashA && hashB == (ulong) entry.FilePathHashB)
                    return entry;
            }

            return null;
        }
    }
}