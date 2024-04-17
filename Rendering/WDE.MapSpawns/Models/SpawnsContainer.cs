using System.Collections.ObjectModel;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Rendering;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using SpawnEntry = WDE.MapSpawns.ViewModels.SpawnEntry;

namespace WDE.MapSpawns.Models;

[UniqueProvider]
public interface ISpawnsContainer
{
    bool IsLoading { get; }
    int? LoadedMap { get; }
    void LoadMap(int mapId);
    PerChunkHolder<List<SpawnInstance>> SpawnsPerChunk { get; }
    FlatTreeList<SpawnEntry, SpawnInstance> Spawns { get; }
    void Clear();
}

[AutoRegister]
[SingleInstance]
public class SpawnsContainer : ISpawnsContainer
{
    private readonly IDatabaseProvider databaseProvider;
    public bool IsLoading { get; private set; }
    public int? LoadedMap { get; private set; }

    private ObservableCollection<SpawnEntry> entries = new();
    private PerChunkHolder<List<SpawnInstance>> spawnsPerChunk = new();
    public FlatTreeList<SpawnEntry, SpawnInstance> spawns;
    public PerChunkHolder<List<SpawnInstance>> SpawnsPerChunk => spawnsPerChunk;
    public FlatTreeList<SpawnEntry, SpawnInstance> Spawns => spawns;
    
    public SpawnsContainer(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
        spawns = new FlatTreeList<SpawnEntry, SpawnInstance>(entries);
    }
    
    public void LoadMap(int mapId)
    {
        if (mapId == LoadedMap)
            return;
        
        if (IsLoading)
            throw new Exception("Another load in progress");

        LoadModelsTasks(mapId).ListenErrors();
    }
    
    public void Clear()
    {
        entries.Clear();
        spawnsPerChunk.Clear();
        LoadedMap = null;
    }

    private async Task LoadModelsTasks(int mapId)
    {
        IsLoading = true;
        try
        {
            Clear();
            
            var creatureSpawns = await databaseProvider.GetCreaturesByMapAsync(mapId);
            var gameObjectSpawns = await databaseProvider.GetGameObjectsByMapAsync(mapId);

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

            foreach (var creatureEntry in creatureSpawns.GroupBy(c => c.Entry))
            {
                var template = await databaseProvider.GetCreatureTemplate(creatureEntry.Key);
                
                if (template == null)
                    continue;
                
                var entry = new SpawnEntry(template);

                creatureTemplateAddons.TryGetValue(entry.Entry, out var creatureTemplateAddon);
                
                foreach (var creatureData in creatureEntry)
                {
                    var instance = new CreatureSpawnInstance(creatureData, template);

                    if (creatureEvents.TryGetValue(creatureData.Guid, out var events))
                        instance.GameEvents = events;
                    
                    creatureAddons.TryGetValue(creatureData.Guid, out var creatureAddon);
                    instance.Addon = (IBaseCreatureAddon?)creatureAddon ?? creatureTemplateAddon;

                    if (creatureData.EquipmentId != 0 || (template.EquipmentTemplateId.HasValue && template.EquipmentTemplateId.Value != 0))
                    {
                        var id = creatureData.EquipmentId == 0 ? ((int?)template.EquipmentTemplateId ?? 0) : (int)creatureData.EquipmentId;
                        if (trinityEquipments != null && trinityEquipments.TryGetValue((creatureData.Entry, id == -1 ? (byte)1 : (byte)id), out var eq))
                            instance.Equipment = eq;
                        else if (mangosEquipments != null && mangosEquipments.TryGetValue(((uint)id), out var eq2))
                            instance.Equipment = eq2;   
                    }

                    entry.Spawns.Add(instance);
                    var chunk = instance.Chunk;
                    spawnsPerChunk[chunk]!.Add(instance);
                }
                entries.Add(entry);
            }

            foreach (var gameObjectEntry in gameObjectSpawns.GroupBy(c => c.Entry))
            {
                var template = await databaseProvider.GetGameObjectTemplate(gameObjectEntry.Key);
                
                if (template == null)
                    continue;
                
                var entry = new SpawnEntry(template);
                foreach (var gameobjectData in gameObjectEntry)
                {
                    var instance = new GameObjectSpawnInstance(gameobjectData, template);
                    if (gameObjectEvents.TryGetValue(gameobjectData.Guid, out var events))
                        instance.GameEvents = events;
                    
                    entry.Spawns.Add(instance);
                    
                    var chunk = instance.Chunk;
                    spawnsPerChunk[chunk]!.Add(instance);
                }
                entries.Add(entry);
            }

            LoadedMap = mapId;
        }
        finally
        {
            IsLoading = false;
        }
    }
}