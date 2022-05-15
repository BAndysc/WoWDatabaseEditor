using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharSectionsStore : IEnumerable<CharSections>
{
    private Dictionary<uint, CharSections> store = new();
    public CharSectionsStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CharSections(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public CharSections this[uint id] => store[id];
    public IEnumerator<CharSections> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}