using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharacterFacialHairStylesStore : IEnumerable<CharacterFacialHairStyles>
{
    private Dictionary<uint, CharacterFacialHairStyles> store = new();
    public CharacterFacialHairStylesStore(IEnumerable<IDbcIterator> rows)
    {
        uint idx = 1;
        foreach (var row in rows)
        {
            var o = new CharacterFacialHairStyles(row);
            store[idx] = o;

            idx++;
        }
    }

    // public bool Contains(uint id) => store.ContainsKey(id);
    // public CharacterFacialHairStyles this[uint id] => store[id];
    public IEnumerator<CharacterFacialHairStyles> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}