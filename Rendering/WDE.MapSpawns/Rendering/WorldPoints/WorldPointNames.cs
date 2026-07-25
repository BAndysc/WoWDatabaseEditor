using System.Text;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>DBC name lookups shared by the world-point inspectors (same access pattern as
/// SpawnTreeBuilder).</summary>
internal static class WorldPointNames
{
    public static unsafe string AreaName(DbcManager dbc, uint areaId)
    {
        if (dbc.AreaTableStore.TryGetValue(areaId, out var a))
        {
            var s = Utf8ToString(a->Name);
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return $"Area {areaId}";
    }

    public static unsafe string MapName(DbcManager dbc, int mapId)
    {
        if (dbc.MapStore.TryGetValue(mapId, out var m))
        {
            var s = Utf8ToString(m->Name);
            if (!string.IsNullOrWhiteSpace(s))
                return s;
        }
        return $"Map {mapId}";
    }

    /// <summary>Area under the position and its parent zone (zone == the area itself when the area
    /// is a top-level zone).</summary>
    public static unsafe (int? zone, int? area) ZoneAndArea(DbcManager dbc, ZoneAreaManager zoneArea, int map, Vector3 pos)
    {
        var areaId = zoneArea.GetAreaId(map, pos);
        if (areaId == null)
            return (null, null);
        if (!dbc.AreaTableStore.TryGetValue((uint)areaId.Value, out var a))
            return (null, null);
        if (a->ParentAreaId == 0)
            return (areaId.Value, null); // this area is itself a zone
        return ((int)a->ParentAreaId, areaId.Value);
    }

    private static unsafe string Utf8ToString(Utf8NativeString s)
        => s.IsNull || s.Length == 0 ? "" : Encoding.UTF8.GetString(s.AsSpan());
}
