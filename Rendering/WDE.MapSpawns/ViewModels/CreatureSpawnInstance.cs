using WDE.Common.Database;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using WDE.MapSpawns.Models;

namespace WDE.MapSpawns.ViewModels;

public class CreatureSpawnInstance : SpawnInstance
{
    public ICreatureTemplate CreatureTemplate { get; }
    private readonly ICreature data;
    public override uint Guid => data.Guid;
    public override uint Entry => data.Entry;
    public override Vector3 Position => new Vector3(data.X, data.Y, data.Z);
    public override (int, int) Chunk => new Vector3(data.X, data.Y, data.Z).WoWPositionToChunk();
    public sealed override string Header { get; protected set; }
    public override WorldObjectInstance? WorldObject => Creature;
    public CreatureInstance? Creature { get; set; }
    public float Orientation => data.O;
    public List<IGameEventCreature>? GameEvents { get; set; }
    public IBaseCreatureAddon? Addon { get; set; }
    public IBaseEquipmentTemplate? Equipment { get; set; }

    public CreatureSpawnInstance(ICreature data, ICreatureTemplate creatureTemplate)
    {
        CreatureTemplate = creatureTemplate;
        this.data = data;
        Header = data.Guid.ToString();
    }
    
    public override bool IsVisibleInPhase(IGamePhaseService gamePhaseService)
    {
        if (data.PhaseMask.HasValue)
        {
            return gamePhaseService.PhaseMaskOverlaps(data.PhaseMask.Value);
        }
        else
        {
            return gamePhaseService.IsPhaseActive(data.PhaseId, data.PhaseGroup);
        }
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