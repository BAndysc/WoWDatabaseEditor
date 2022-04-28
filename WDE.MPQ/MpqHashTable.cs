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
        private Dictionary<(ulong, ulong), HashTableEntry> table = new();
        
        public MpqHashTable(IEnumerable<HashTableEntry> entries)
        {
            foreach (var e in entries)
                table[(e.FilePathHashA, e.FilePathHashB)] = e;
        }

        public HashTableEntry? FindEntry(ulong hashA, ulong hashB)
        {
            if (table.TryGetValue((hashA, hashB), out var entry))
                return entry;
            
            return null;
        }
    }
}