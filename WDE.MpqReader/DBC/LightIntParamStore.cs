using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightIntParamStore
{
    private Dictionary<uint, LightIntParam> store = new();
    public LightIntParamStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LightIntParam(row);
            store[o.Id] = o;
        }
    }
        
    public LightIntParam this[uint id] => store[id];
}