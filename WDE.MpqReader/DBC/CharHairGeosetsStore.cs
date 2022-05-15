using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharHairGeosetsStore : IEnumerable<CharHairGeosets>
{
    private Dictionary<uint, CharHairGeosets> store = new();
    public CharHairGeosetsStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CharHairGeosets(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public CharHairGeosets this[uint id] => store[id];
    public IEnumerator<CharHairGeosets> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}