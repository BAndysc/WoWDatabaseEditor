using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightFloatParamStore
{
    private Dictionary<uint, LightFloatParam> store = new();
    public LightFloatParamStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LightFloatParam(row);
            store[o.Id] = o;
        }
    }
        
    public LightFloatParam this[uint id] => store[id];
}