using WDE.Common.Database;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.StaticData;
using WDE.MapSpawnsEditor.Models;

namespace WDE.MapSpawnsEditor.ViewModels;

public class GameObjectSpawnInstance : SpawnInstance
{
    public IGameObjectTemplate GameObjectTemplate { get; }
    private IGameObject data;
    public override uint Guid => data.Guid;
    public override uint Entry => data.Entry;
    public override Vector3 Position => new Vector3(data.X, data.Y, data.Z);
    public override uint Map => data.Map;
    public Quaternion Rotation => new Quaternion(data.Rotation0, data.Rotation1, data.Rotation2, data.Rotation3);
    public override (int, int) Chunk => new Vector3(data.X, data.Y, data.Z).WoWPositionToChunk();
    public sealed override string Header { get; protected set; } = "";
    public override WorldObjectInstance? WorldObject => GameObject;
    public GameObjectInstance? GameObject { get; set; }
    public IList<IGameEventGameObject>? GameEvents { get; set; }

    public GameObjectSpawnInstance(IGameObject data, IGameObjectTemplate gameObjectTemplate, ISpawnGroupTemplate? spawnGroup)
    {
        GameObjectTemplate = gameObjectTemplate;
        this.data = data;
        UpdateData(data, spawnGroup);
    }
    
    public void UpdateData(IGameObject gameobjectData, ISpawnGroupTemplate? spawnGroup)
    {
        data = gameobjectData;
        SpawnGroup = spawnGroup;
        if (spawnGroup == null)
            Header = data.Guid.ToString();
        else
            Header = $"{data.Guid} ({GameObjectTemplate.Name} - {GameObjectTemplate.Entry})";
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
    
    public override void Dispose()
    {
        WorldObject?.Dispose();
        GameObject = null;
    }
    
    public override string ToString() => Header;
}