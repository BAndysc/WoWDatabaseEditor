using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using WDE.MapSpawnsEditor.Models;

namespace WDE.MapSpawnsEditor.ViewModels;

public class CreatureSpawnInstance : SpawnInstance
{
    public ICreatureTemplate CreatureTemplate { get; }
    private ICreature data;
    public override uint Guid => data.Guid;
    public override uint Entry => data.Entry;
    public override Vector3 Position => new Vector3(data.X, data.Y, data.Z);
    public override uint PhaseMask => data.PhaseMask ?? 0;
    public override SmallReadOnlyList<int>? Phases => data.PhaseId;
    public override int Map => data.Map;
    public MovementType MovementType => data.MovementType;
    public float WanderDistance => data.WanderDistance;
    public override (int, int) Chunk => new Vector3(data.X, data.Y, data.Z).WoWPositionToChunk();
    public sealed override string Header { get; protected set; } = "";
    public override WorldObjectInstance? WorldObject => Creature;
    private CreatureInstance? creature;
    public CreatureInstance? Creature
    {
        get => creature;
        set
        {
            creature = value;
            OnPropertyChanged(nameof(IsSpawned));
        }
    }
    public float Orientation => data.O;
    public uint CreatureDisplayId => data.Model;
    public IReadOnlyList<IGameEventCreature>? GameEvents { get; set; }
    public IBaseCreatureAddon? Addon { get; set; }
    public IBaseEquipmentTemplate? Equipment { get; set; }
    public override ImageUri Icon { get; } = new ImageUri("Icons/document_creature_template.png");

    public CreatureSpawnInstance(ICreature data, ICreatureTemplate creatureTemplate, ISpawnGroupTemplate? spawnGroup)
    {
        CreatureTemplate = creatureTemplate;
        this.data = data;
        UpdateData(data, spawnGroup);
    }
    
    public void UpdateData(ICreature creatureData, ISpawnGroupTemplate? spawnGroup)
    {
        data = creatureData;
        SpawnGroup = spawnGroup;
        if (spawnGroup == null)
            Header = data.Guid.ToString();
        else
            Header = $"{data.Guid} ({CreatureTemplate.Name} - {CreatureTemplate.Entry})";
    }
    
    public override bool IsVisibleInPhase(IGamePhaseService gamePhaseService)
    {
        return gamePhaseService.IsVisible(data.PhaseMask, data.PhaseId, data.PhaseGroup);
    }

    public override void Dispose()
    {
        WorldObject?.Dispose();
        Creature = null;
    }

    public override bool IsVisibleInEvents(IGameEventService eventService)
    {
        if (GameEvents == null || GameEvents.Count == 0)
            return true;

        foreach (var @event in GameEvents)
        {
            if (@event.Mode == GameEventSpawnMode.SpawnOnEvent)
            {
                if (eventService.IsEventActive(@event.ActualEventEntry))
                    return true;
            }
            else if (@event.Mode == GameEventSpawnMode.RemoveOnEvent)
            {
                if (eventService.IsEventActive(@event.ActualEventEntry))
                    return false;
            }
        }

        return GameEvents[0].Mode != GameEventSpawnMode.SpawnOnEvent;
    }

    public override string ToString() => Header;
}