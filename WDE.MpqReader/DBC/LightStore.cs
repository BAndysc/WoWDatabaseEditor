using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightStore : IEnumerable<Light>
{
    private Dictionary<uint, Light> store = new();
    public LightStore(IEnumerable<IDbcIterator> rows, LightParamStore lightParamStore)
    {
        foreach (var row in rows)
        {
            var o = new Light(row, lightParamStore);
            store[o.Id] = o;
        }
    }
        
    public Light this[uint id] => store[id];
    public IEnumerator<Light> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}