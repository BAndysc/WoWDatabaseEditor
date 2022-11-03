using TheEngine.ECS;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawnsEditor.Models;

namespace WDE.MapSpawnsEditor.ViewModels;

public abstract class SpawnInstance : IChildType, IManagedComponentData
{
    public abstract uint Guid { get; }
    public abstract uint Entry { get; }
    public abstract Vector3 Position { get; }
    public abstract uint Map { get; }
    public abstract (int, int) Chunk { get; }
    public abstract string Header { get; protected set; }
    public abstract WorldObjectInstance? WorldObject { get; }
    public ISpawnGroupTemplate? SpawnGroup { get; set; }
    public abstract bool IsVisibleInEvents(IGameEventService eventService);
    public abstract bool IsVisibleInPhase(IGamePhaseService gamePhaseService);
    
    public bool IsSpawned => WorldObject != null;
    public abstract void Dispose();
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
}