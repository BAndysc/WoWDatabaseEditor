using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class WorldMapAreaStore : BaseDbcStore<uint, WorldMapArea>
{
    private Dictionary<int, List<WorldMapArea>> perMap = new();
    
    public WorldMapAreaStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new WorldMapArea(row, version);
            store[o.Id] = o;
            if (!perMap.TryGetValue(o.MapId, out var mapList))
                perMap[o.MapId] = mapList = new List<WorldMapArea>();
            mapList.Add(o);
        }
    }
    
    public WorldMapAreaStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new WorldMapArea(row, version);
            store[o.Id] = o;
            if (!perMap.TryGetValue(o.MapId, out var mapList))
                perMap[o.MapId] = mapList = new List<WorldMapArea>();
            mapList.Add(o);
        }
    }

    public WorldMapArea? FindClosest(int mapId, float x, float y)
    {
        if (!perMap.TryGetValue(mapId, out var mapList))
            return null;
        
        WorldMapArea? best = null;
        float bestDistance = float.MaxValue;
        
        foreach (var area in mapList)
        {
            if (y > area.Left || y < area.Right || x > area.Top || x < area.Bottom)
                continue;
            
            var centerY = (area.Left + area.Right) / 2;
            var centerX = (area.Top + area.Bottom) / 2;
            
            var distance = (centerX - x) * (centerX - x) + (centerY - y) * (centerY - y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = area;
            }
        }

        return best;
    }
}