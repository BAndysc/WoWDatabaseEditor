using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace WDE.MapRenderer.Managers.Entities;

public abstract class WorldObjectInstance : System.IDisposable
{
    protected readonly IGameContext gameContext;
    protected List<Entity> handles = new();
    protected List<Entity> colliders = new();
    protected List<MdxManager.MdxInstance> mdxInstances = new();
    protected List<WmoManager.WmoInstance> wmoInstances = new();
    protected readonly RenderLayer renderLayer;

    public abstract void Dispose();

    public WorldObjectInstance(IGameContext gameContext, RenderLayer renderLayer)
    {
        this.gameContext = gameContext;
        this.renderLayer = renderLayer;
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
            textEntity.SetDisabledObject(gameContext.EntityManager, !value);
        }
    }

    protected Entity objectEntity;
    protected Entity textEntity;
    
    public Entity WorldObjectEntity => objectEntity;

}