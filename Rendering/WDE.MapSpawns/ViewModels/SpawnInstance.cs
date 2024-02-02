using TheEngine.ECS;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;

namespace WDE.MapSpawns.ViewModels;

public abstract class SpawnInstance : IChildType, IManagedComponentData
{
    public abstract uint Guid { get; }
    public abstract uint Entry { get; }
    public abstract Vector3 Position { get; }
    public abstract (int, int) Chunk { get; }
    public abstract string Header { get; protected set; }
    public abstract WorldObjectInstance? WorldObject { get; }
    public abstract bool IsVisibleInEvents(IGameEventService eventService);
    public abstract bool IsVisibleInPhase(IGamePhaseService gamePhaseService);
    
    public bool IsSpawned => WorldObject != null;
    public bool CanBeExpanded => false;
    public abstract void Dispose();
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
}