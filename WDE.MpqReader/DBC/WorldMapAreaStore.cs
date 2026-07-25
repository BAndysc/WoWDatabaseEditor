using System.Runtime.CompilerServices;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public unsafe class WorldMapAreaStore : NativeBaseDbcStore<uint, WorldMapArea>
{
    private Dictionary<int, List<nint>> perMap = new();

    public WorldMapAreaStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new WorldMapArea(row, version);
            Set(o.Id, ref o);
            if (!perMap.TryGetValue(o.MapId, out var mapList))
                perMap[o.MapId] = mapList = new List<nint>();
            mapList.Add((nint)Unsafe.AsPointer(ref o));
        }
    }

    public WorldMapAreaStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new WorldMapArea(row, version);
            Set(o.Id, ref o);
            if (!perMap.TryGetValue(o.MapId, out var mapList))
                perMap[o.MapId] = mapList = new List<nint>();
            mapList.Add((nint)Unsafe.AsPointer(ref o));
        }
    }

    public WorldMapArea* FindClosest(int mapId, float x, float y)
    {
        if (!perMap.TryGetValue(mapId, out var mapList))
            return null;

        WorldMapArea* best = null;
        float bestDistance = float.MaxValue;

        foreach (var ptr in mapList)
        {
            var area = (WorldMapArea*)ptr;
            if (y > area->Left || y < area->Right || x > area->Top || x < area->Bottom)
                continue;

            var centerY = (area->Left + area->Right) / 2;
            var centerX = (area->Top + area->Bottom) / 2;

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
