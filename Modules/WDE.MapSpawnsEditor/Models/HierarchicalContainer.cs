using WDE.Common.Database;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Models;

internal class HierarchicalContainer
{
    private readonly DbcManager dbcManager;
    private IList<SpawnGroup> roots;
    private Dictionary<uint, SpawnGroup> maps = new();
    private Dictionary<uint, SpawnGroup> zones = new();
    private Dictionary<uint, SpawnGroup> areas = new();
    private Dictionary<(int, uint), SpawnGroup> creatures = new();
    private Dictionary<(int, uint), SpawnGroup> gameobjects = new();
    private Dictionary<uint, SpawnGroup> spawnGroupTemplates = new();

    public HierarchicalContainer(IList<SpawnGroup> roots, DbcManager dbcManager)
    {
        this.roots = roots;
        this.dbcManager = dbcManager;
    }
    
    private SpawnGroup GetMap(uint mapId)
    {
        if (maps.TryGetValue(mapId, out var mapEntry))
            return mapEntry;

        dbcManager.MapStore.TryGetValue((int)mapId, out var map);

        maps[mapId] = mapEntry = new(GroupType.Map, mapId, map?.Name ?? "(unknown map)");
        roots.Add(mapEntry);
        return mapEntry;
    }

    private SpawnGroup GetZone(uint zoneId)
    {
        if (zones.TryGetValue(zoneId, out var zoneEntry))
            return zoneEntry;

        dbcManager.AreaTableStore.TryGetValue(zoneId, out var zone);

        zones[zoneId] = zoneEntry = new(GroupType.Zone, zoneId, zone?.Name ?? "(unknown zone)");

        if (zone == null)
            roots.Add(zoneEntry);
        else
        {
            var mapEntry = GetMap(zone.MapId);
            mapEntry.Nested.Add(zoneEntry);
            zoneEntry.Parent = mapEntry;
        }
        
        return zoneEntry;
    }

    private SpawnGroup GetArea(uint areaId)
    {
        if (areas.TryGetValue(areaId, out var areaEntry))
            return areaEntry;

        if (!dbcManager.AreaTableStore.TryGetValue(areaId, out var area))
            return GetMap(areaId);

        if (area.ParentAreaId == 0)
        {
            var zone = GetZone(area.Id);
            areas[zone.Entry] = zone;
            return zone;
        }
                
        areas[areaId] = areaEntry = new(GroupType.Area, areaId, area.Name);

        var zoneEntry = GetZone(area.ParentAreaId);
        zoneEntry.Nested.Add(areaEntry); 
        areaEntry.Parent = zoneEntry;
        
        return areaEntry;
    }

    public SpawnGroup GetSpawnGroupTemplate(ISpawnGroupTemplate groupTemplate, uint? areaId, uint fallbackMapId)
    {
        if (spawnGroupTemplates.TryGetValue(groupTemplate.Id, out var templateEntry))
            return templateEntry;
        
        var areaEntry = areaId.HasValue ? GetArea(areaId.Value) : GetMap(fallbackMapId);
        spawnGroupTemplates[groupTemplate.Id] = templateEntry = new(GroupType.SpawnGroup, groupTemplate.Id, groupTemplate.Name);
        areaEntry.Nested.Add(templateEntry);
        templateEntry.Parent = areaEntry;
        
        return templateEntry;
    }

    public SpawnGroup GetCreature(uint? areaId, uint fallbackMapId, ICreatureTemplate template, ISpawnGroupTemplate? spawnGroup)
    {
        if (spawnGroup != null)
        {
            var group = GetSpawnGroupTemplate(spawnGroup, areaId, fallbackMapId);
            return group;
        }
        else
        {
            (int, uint) key;
            if (areaId.HasValue)
                key = ((int)areaId.Value, template.Entry);
            else
                key = (-(int)fallbackMapId, template.Entry);

            if (creatures.TryGetValue(key, out var creatureEntry))
                return creatureEntry;
            
            creatures[key] = creatureEntry = new(GroupType.Creature, template.Entry, template.Name);

            if (areaId.HasValue)
            {
                var area = GetArea(areaId.Value);
                area.Nested.Add(creatureEntry);
                creatureEntry.Parent = area;
            }
            else
            {
                var map = GetMap(fallbackMapId);
                map.Nested.Add(creatureEntry);
                creatureEntry.Parent = map;
            }
    
            return creatureEntry;
        }
    }

    public SpawnGroup GetGameObject(uint? areaId, uint fallbackMapId, IGameObjectTemplate template, ISpawnGroupTemplate? spawnGroup)
    {
        if (spawnGroup != null)
        {
            var group = GetSpawnGroupTemplate(spawnGroup, areaId, fallbackMapId);
            return group;
        }
        else
        {
            (int, uint) key;
            if (areaId.HasValue)
                key = ((int)areaId.Value, template.Entry);
            else
                key = (-(int)fallbackMapId, template.Entry);

            if (gameobjects.TryGetValue(key, out var gobjectEntry))
                return gobjectEntry;

            gameobjects[key] = gobjectEntry = new(GroupType.GameObject, template.Entry, template.Name);

            if (areaId.HasValue)
            {
                var area = GetArea(areaId.Value);
                area.Nested.Add(gobjectEntry);
                gobjectEntry.Parent = area;
            }
            else
            {
                var map = GetMap(fallbackMapId);
                map.Nested.Add(gobjectEntry);
                gobjectEntry.Parent = map;
            }

            return gobjectEntry;
        }
    }
}