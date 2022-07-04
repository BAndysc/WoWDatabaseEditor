using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightDataStore : BaseDbcStore<uint, LightData>
{
    private readonly Dictionary<ushort, List<LightData>> storeByLightParamId = new Dictionary<ushort, List<LightData>>();
    public LightDataStore(IEnumerable<IWdcIterator> wdcIterators)
    {
        foreach (var iterator in wdcIterators)
        {
            var data = new LightData(iterator);
            store[data.Id] = data;
            if (!storeByLightParamId.TryGetValue(data.LightParamID, out var list))
                list = storeByLightParamId[data.LightParamID] = new List<LightData>();
            list.Add(data);
        }
    }
    public LightDataStore(IEnumerable<IDbcIterator> iterators)
    {
        foreach (var row in iterators)
        {
            var data = new LightData(row);
            store[data.Id] = data;
            if (!storeByLightParamId.TryGetValue(data.LightParamID, out var list))
                list = storeByLightParamId[data.LightParamID] = new List<LightData>();
            list.Add(data);
        }
    }

    public IReadOnlyList<LightData>? GetDataByLight(ushort lightId)
    {
        if (storeByLightParamId.TryGetValue(lightId, out var list))
            return list;
        return null;
    }

    public LightDataStore()
    {
        
    }
}