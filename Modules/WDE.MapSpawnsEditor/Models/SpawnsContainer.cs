using System.Collections.ObjectModel;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.MapSpawnsEditor.Models;

[UniqueProvider]
public interface ISpawnsContainer
{
    bool IsLoading { get; }
    int? LoadedMap { get; }
    void LoadMap(int mapId, ZoneAreaManager zoneAreaManager, DbcManager dbcManager);
    PerChunkHolder<List<SpawnInstance>> SpawnsPerChunk { get; }
    FlatTreeList<SpawnGroup, SpawnInstance> Spawns { get; }
    void Clear();
    Task Reload(GuidType type, uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager);
    Task ReloadCreature(uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager);
    Task ReloadGameobject(uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager);
    event Action<CreatureSpawnInstance>? OnCreatureModified;
    event Action<GameObjectSpawnInstance>? OnGameobjectModified;
}

[AutoRegister]
[SingleInstance]
public class SpawnsContainer : ISpawnsContainer
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly Lazy<ISpawnSelectionService> selectionService;
    public bool IsLoading { get; private set; }
    public int? LoadedMap { get; private set; }

    private HierarchicalContainer hierarchy = null!;
    private ObservableCollection<SpawnGroup> entries = new();
    private PerChunkHolder<List<SpawnInstance>> spawnsPerChunk = new();
    public FlatTreeList<SpawnGroup, SpawnInstance> spawns;
    public PerChunkHolder<List<SpawnInstance>> SpawnsPerChunk => spawnsPerChunk;
    public FlatTreeList<SpawnGroup, SpawnInstance> Spawns => spawns;
    private Dictionary<uint, CreatureSpawnInstance> guidToCreature = new();
    private Dictionary<uint, GameObjectSpawnInstance> guidToGameobject = new();
    
    public event Action<CreatureSpawnInstance>? OnCreatureModified;
    public event Action<GameObjectSpawnInstance>? OnGameobjectModified;
    
    public SpawnsContainer(IDatabaseProvider databaseProvider,
        Lazy<ISpawnSelectionService> selectionService)
    {
        this.databaseProvider = databaseProvider;
        this.selectionService = selectionService;
        spawns = new FlatTreeList<SpawnGroup, SpawnInstance>(entries);
    }
    
    public void LoadMap(int mapId, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        if (mapId == LoadedMap)
            return;
        
        if (IsLoading)
            throw new Exception("Another load in progress");

        LoadModelsTasks(mapId, zoneAreaManager, dbcManager).ListenErrors();
    }
    
    public void Clear()
    {
        guidToCreature = new();
        guidToGameobject = new();
        hierarchy = null!;
        entries.Clear();
        spawnsPerChunk.Clear();
        LoadedMap = null;
    }

    public Task Reload(GuidType type, uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        if (type == GuidType.Creature)
            return ReloadCreature(entry, guid, zoneAreaManager, dbcManager);
        else
            return ReloadGameobject(entry, guid, zoneAreaManager, dbcManager);
    }

    public async Task ReloadCreature(uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        var creatureData = await databaseProvider.GetCreaturesByGuidAsync(entry, guid);

        if (creatureData == null)
        {
            // object is removed
            RemoveCreatureFromHierarchy(guid);
            if (selectionService.Value.SelectedSpawn.Value is { } selectedSpawn && selectedSpawn.Guid == guid)
                selectionService.Value.SelectedSpawn.Value = null;
        }
        else
        {
            var events = await databaseProvider.GetGameEventCreaturesByGuidAsync(entry, guid);
            var areaId = zoneAreaManager.GetAreaId(creatureData.Map, new Vector3(creatureData.X, creatureData.Y, creatureData.Z));
            var template = await databaseProvider.GetCreatureTemplate(creatureData.Entry);
            var addonTemplate = await databaseProvider.GetCreatureTemplateAddon(creatureData.Entry);
            var addon = await databaseProvider.GetCreatureAddon(creatureData.Entry, creatureData.Guid);
            var spawnGroup = await databaseProvider.GetSpawnGroupSpawnByGuidAsync(creatureData.Guid, SpawnGroupTemplateType.Creature);
            var spawnGroupTemplate = spawnGroup == null ? null : await databaseProvider.GetSpawnGroupTemplateByIdAsync(spawnGroup.TemplateId);
            
            if (template == null)
                return;
            
            var templateGroup = hierarchy.GetCreature(areaId, creatureData.Map, template, spawnGroupTemplate);
            var existingInstance = (CreatureSpawnInstance?)templateGroup.Spawns.FirstOrDefault(s => s.Guid == guid && s is CreatureSpawnInstance);

            if (existingInstance == null)
            {
                RemoveCreatureFromHierarchy(guid);
                
                var instance = new CreatureSpawnInstance(creatureData, template, spawnGroupTemplate);
                guidToCreature[instance.Guid] = instance;
                instance.GameEvents = events;
                instance.Addon = (IBaseCreatureAddon?)addon ?? addonTemplate;
            
                instance.Parent = templateGroup;
                templateGroup.Spawns.Add(instance);
                OnCreatureModified?.Invoke(instance);
                selectionService.Value.SelectedSpawn.Value = instance;
            }
            else
            {
                existingInstance.UpdateData(creatureData, spawnGroupTemplate);
                existingInstance.Addon = (IBaseCreatureAddon?)addon ?? addonTemplate;
                existingInstance.GameEvents = events;
                existingInstance.Dispose();
                OnCreatureModified?.Invoke(existingInstance);
            }
        }
    }

    private void RemoveCreatureFromHierarchy(uint guid)
    {
        if (guidToCreature.TryGetValue(guid, out var existing))
        {
            ((SpawnGroup?)existing.Parent)!.Spawns.Remove(existing);
            existing.Dispose();
            guidToCreature.Remove(guid);
        }
    }

    private void RemoveGameobjectFromHierarchy(uint guid)
    {
        if (guidToGameobject.TryGetValue(guid, out var existing))
        {
            ((SpawnGroup?)existing.Parent)?.Spawns.Remove(existing);
            existing.Dispose();
            guidToGameobject.Remove(guid);
        }
    }

    public async Task ReloadGameobject(uint entry, uint guid, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        var gameobjectData = await databaseProvider.GetGameObjectByGuidAsync(entry, guid);

        if (gameobjectData == null)
        {
             // object is removed
             RemoveGameobjectFromHierarchy(guid);
             if (selectionService.Value.SelectedSpawn.Value is { } selectedSpawn && selectedSpawn.Guid == guid)
                 selectionService.Value.SelectedSpawn.Value = null;
        }
        else
        {
            var gameobjectEvents = await databaseProvider.GetGameEventGameObjectsByGuidAsync(entry, guid);
            var areaId = zoneAreaManager.GetAreaId(gameobjectData.Map, new Vector3(gameobjectData.X, gameobjectData.Y, gameobjectData.Z));
            var template = await databaseProvider.GetGameObjectTemplate(gameobjectData.Entry);
            
            if (template == null)
                return;
            
            var spawnGroup = await databaseProvider.GetSpawnGroupSpawnByGuidAsync(gameobjectData.Guid, SpawnGroupTemplateType.GameObject);
            var spawnGroupTemplate = spawnGroup == null ? null : await databaseProvider.GetSpawnGroupTemplateByIdAsync(spawnGroup.TemplateId);

            var templateGroup = hierarchy.GetGameObject(areaId, gameobjectData.Map, template, spawnGroupTemplate);
            var existingInstance = (GameObjectSpawnInstance?)templateGroup.Spawns.FirstOrDefault(s => s.Guid == guid && s is GameObjectSpawnInstance);

            if (existingInstance == null)
            {
                RemoveGameobjectFromHierarchy(guid);
                
                var instance = new GameObjectSpawnInstance(gameobjectData, template, spawnGroupTemplate);
                guidToGameobject[gameobjectData.Guid] = instance;
                instance.GameEvents = gameobjectEvents;
            
                instance.Parent = templateGroup;
                templateGroup.Spawns.Add(instance);
                OnGameobjectModified?.Invoke(instance);
                selectionService.Value.SelectedSpawn.Value = instance;
            }
            else
            {
                existingInstance.UpdateData(gameobjectData, spawnGroupTemplate);
                existingInstance.GameEvents = gameobjectEvents;
                existingInstance.Dispose();
                OnGameobjectModified?.Invoke(existingInstance);
            }
        }
    }

    private async Task LoadSpawns(int mapId, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        var creatureSpawns = await databaseProvider.GetCreaturesByMapAsync(mapId);
        var gameObjectSpawns = await databaseProvider.GetGameObjectsByMapAsync(mapId);

        var spawnGroupTemplates = (await databaseProvider.GetSpawnGroupTemplatesAsync()).ToDictionary(x => x.Id, x => x);
        
        var creatureSpawnGroups = spawnGroupTemplates
            .Where(s => s.Value.Type == SpawnGroupTemplateType.Creature)
            .Select(c => c.Key)
            .ToHashSet();
        var spawnGroupSpawns = (await databaseProvider.GetSpawnGroupSpawnsAsync()).SafeToDictionary(x => (x.Type == SpawnGroupTemplateType.Any ? (creatureSpawnGroups.Contains(x.TemplateId) ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject) : x.Type, x.Guid), x => x);
        
        var creatureEvents = (await databaseProvider.GetGameEventCreaturesAsync()).GroupBy(x => x.Guid).ToDictionary(x => x.Key, x=>x.ToList());
        var gameObjectEvents = (await databaseProvider.GetGameEventGameObjectsAsync()).GroupBy(x => x.Guid).ToDictionary(x => x.Key, x=>x.ToList());
        
        var creatureTemplateAddons = (await databaseProvider.GetCreatureTemplateAddons()).ToDictionary(x => x.Entry);
        var creatureAddons = (await databaseProvider.GetCreatureAddons()).ToDictionary(x => x.Guid);

        var trinityEquipmentsList = await databaseProvider.GetCreatureEquipmentTemplates();
        var mangosEquipmentsList = await databaseProvider.GetMangosCreatureEquipmentTemplates();

        Dictionary<(uint, byte), IBaseEquipmentTemplate>? trinityEquipments =
            trinityEquipmentsList?.ToDictionary(x => (x.Entry, x.Id), x => (IBaseEquipmentTemplate)x);
        Dictionary<uint, IBaseEquipmentTemplate>? mangosEquipments =
            mangosEquipmentsList?.ToDictionary(x => x.Entry, x => (IBaseEquipmentTemplate)x);

        hierarchy = new(entries, dbcManager);

        foreach (var creatureSpawn in creatureSpawns)
        {
            var areaId = zoneAreaManager.GetAreaId(creatureSpawn.Map, new Vector3(creatureSpawn.X, creatureSpawn.Y, creatureSpawn.Z));

            creatureTemplateAddons.TryGetValue(creatureSpawn.Entry, out var creatureTemplateAddon);
            var template = await databaseProvider.GetCreatureTemplate(creatureSpawn.Entry);
            
            if (template == null)
                continue;

            spawnGroupSpawns.TryGetValue((SpawnGroupTemplateType.Creature, creatureSpawn.Guid), out var spawnGroup);
            
            ISpawnGroupTemplate? spawnGroupTemplate = null;
            if (spawnGroup != null)
                spawnGroupTemplates.TryGetValue(spawnGroup.TemplateId, out spawnGroupTemplate);
            
            var templateGroup = hierarchy.GetCreature(areaId, creatureSpawn.Map, template, spawnGroupTemplate);

            var instance = new CreatureSpawnInstance(creatureSpawn, template, spawnGroupTemplate);
            guidToCreature[creatureSpawn.Guid] = instance;

            if (creatureEvents.TryGetValue(creatureSpawn.Guid, out var events))
                instance.GameEvents = events;
                
            creatureAddons.TryGetValue(creatureSpawn.Guid, out var creatureAddon);
            instance.Addon = (IBaseCreatureAddon?)creatureAddon ?? creatureTemplateAddon;

            if (creatureSpawn.EquipmentId != 0 || (template.EquipmentTemplateId.HasValue && template.EquipmentTemplateId.Value != 0))
            {
                var id = creatureSpawn.EquipmentId == 0 ? ((int?)template.EquipmentTemplateId ?? 0) : (int)creatureSpawn.EquipmentId;
                if (trinityEquipments != null && trinityEquipments.TryGetValue((creatureSpawn.Entry, id == -1 ? (byte)1 : (byte)id), out var eq))
                    instance.Equipment = eq;
                else if (mangosEquipments != null && mangosEquipments.TryGetValue(((uint)id), out var eq2))
                    instance.Equipment = eq2;   
            }

            instance.Parent = templateGroup;
            templateGroup.Spawns.Add(instance);
        }
        
        foreach (var gameobjectData in gameObjectSpawns)
        {
            var areaId = zoneAreaManager.GetAreaId(gameobjectData.Map, new Vector3(gameobjectData.X, gameobjectData.Y, gameobjectData.Z));

            var template = await databaseProvider.GetGameObjectTemplate(gameobjectData.Entry);
            
            if (template == null)
                continue;
            
            spawnGroupSpawns.TryGetValue((SpawnGroupTemplateType.GameObject, gameobjectData.Guid), out var spawnGroup);
            
            ISpawnGroupTemplate? spawnGroupTemplate = null;
            if (spawnGroup != null)
                spawnGroupTemplates.TryGetValue(spawnGroup.TemplateId, out spawnGroupTemplate);

            var templateGroup = hierarchy.GetGameObject(areaId, gameobjectData.Map, template, spawnGroupTemplate);
            
            var instance = new GameObjectSpawnInstance(gameobjectData, template, spawnGroupTemplate);
            guidToGameobject[gameobjectData.Guid] = instance;
            
            if (gameObjectEvents.TryGetValue(gameobjectData.Guid, out var events))
                instance.GameEvents = events;
            
            instance.Parent = templateGroup;
            templateGroup.Spawns.Add(instance);
        }
    }
    
    private async Task LoadModelsTasks(int mapId, ZoneAreaManager zoneAreaManager, DbcManager dbcManager)
    {
        IsLoading = true;
        try
        {
            Clear();

            await LoadSpawns(mapId, zoneAreaManager, dbcManager);

            foreach (var child in spawns.GetChildren())
            {
                if (child.Map == mapId)
                    spawnsPerChunk[child.Chunk]!.Add(child);
            }
            
            LoadedMap = mapId;
        }
        finally
        {
            IsLoading = false;
        }
    }
}