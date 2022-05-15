using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightParamStore
{
    private Dictionary<uint, LightParam> store = new();
    public LightParamStore(IEnumerable<IDbcIterator> rows, LightIntParamStore lightIntParamStore, LightFloatParamStore floatParamStore)
    {
        foreach (var row in rows)
        {
            var o = new LightParam(row, lightIntParamStore, floatParamStore);
            store[o.Id] = o;
        }
    }
        
    public LightParam this[uint id] => store[id];
}