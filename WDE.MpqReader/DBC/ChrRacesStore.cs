using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ChrRacesStore : IEnumerable<ChrRaces>
{
    private Dictionary<uint, ChrRaces> store = new();
    public ChrRacesStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ChrRaces(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public ChrRaces this[uint id] => store[id];
    public IEnumerator<ChrRaces> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}