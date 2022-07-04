using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class LightParamStore : BaseDbcStore<uint, LightParam>
{
    public LightParamStore(GameFilesVersion wowVersion, IEnumerable<IDbcIterator> rows, LightIntParamStore lightIntParamStore, LightFloatParamStore floatParamStore, LightDataStore lightDataStore)
    {
        foreach (var row in rows)
        {
            var o = new LightParam(wowVersion, row, lightIntParamStore, floatParamStore, lightDataStore);
            store[o.Id] = o;
        }
    }
    
    public LightParamStore(GameFilesVersion wowVersion, IEnumerable<IWdcIterator> rows, LightIntParamStore lightIntParamStore, LightFloatParamStore floatParamStore, LightDataStore lightDataStore)
    {
        foreach (var row in rows)
        {
            var o = new LightParam(wowVersion, row, lightDataStore);
            store[o.Id] = o;
        }
    }
}