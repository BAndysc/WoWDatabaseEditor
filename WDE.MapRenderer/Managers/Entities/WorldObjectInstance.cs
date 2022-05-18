using TheEngine.Components;
using TheEngine.ECS;
using TheMaths;

namespace WDE.MapRenderer.Managers.Entities;

public abstract class WorldObjectInstance : System.IDisposable
{
    protected readonly IGameContext gameContext;
    protected List<Entity> handles = new();
    protected List<Entity> renderers = new();
    protected List<Entity> colliders = new();
    public IReadOnlyList<Entity> Renderers => renderers;
    
    public abstract void Dispose();

    public WorldObjectInstance(IGameContext gameContext)
    {
        this.gameContext = gameContext;
    }
    
    public Vector3 Position
    {
        get => gameContext.EntityManager.GetComponent<LocalToWorld>(WorldObjectEntity).Position;
        set
        {
            gameContext.EntityManager.GetComponent<LocalToWorld>(WorldObjectEntity).Position = value;
            WorldObjectEntity.SetDirtyPosition(gameContext.EntityManager);
        }
    }
    
    protected bool isRenderingEnabled = true;
    public bool EnableRendering
    {
        set
        {
            isRenderingEnabled = value;
            WorldObjectEntity.SetForceDisabledRendering(gameContext.EntityManager, !value);
            foreach (var renderer in renderers)
                renderer.SetForceDisabledRendering(gameContext.EntityManager, !value);
            foreach (var collider in colliders)
                collider.SetDisabledObject(gameContext.EntityManager, !value);
            textEntity.SetDisabledObject(gameContext.EntityManager, !value);
        }
    }
    
    protected Entity objectEntity;
    protected Entity textEntity;
    
    public Entity WorldObjectEntity => objectEntity;

}