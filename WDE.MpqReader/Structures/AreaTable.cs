using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures;

public class AreaTable
{
    public readonly uint Id;
    public readonly uint MapId;
    public readonly uint ParentAreaId;
    public readonly uint AreaBit;
    public readonly uint Flags;
    public readonly string Name;

    public AreaTable(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        MapId = dbcIterator.GetUInt(1);
        ParentAreaId = dbcIterator.GetUInt(2);
        AreaBit = dbcIterator.GetUInt(3);
        Flags = dbcIterator.GetUInt(4);
        Name = dbcIterator.GetString(11);
    }
}

public class AreaTableStore : IEnumerable<AreaTable>
{
    private Dictionary<uint, AreaTable> store = new();
    public AreaTableStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new AreaTable(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public AreaTable this[uint id] => store[id];
    public IEnumerator<AreaTable> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}