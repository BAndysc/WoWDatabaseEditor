using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace WDE.MapRenderer.Managers.Entities;

public abstract class WorldObjectInstance : System.IDisposable
{
    protected readonly IGameContext gameContext;
    protected List<MdxManager.MdxInstance> mdxInstances = new();
    protected List<WmoManager.WmoInstance> wmoInstances = new();
    protected readonly RenderLayer renderLayer;

    public abstract void Dispose();

    public WorldObjectInstance(IGameContext gameContext, RenderLayer renderLayer)
    {
        this.gameContext = gameContext;
        this.renderLayer = renderLayer;
    }

    public Matrix LocalToWorld
    {
        get => gameContext.EntityManager.GetComponent<LocalToWorld>(WorldObjectEntity);
        set
        {
            gameContext.EntityManager.GetComponent<LocalToWorld>(WorldObjectEntity).Matrix = value;
            WorldObjectEntity.SetDirtyPosition(gameContext.EntityManager);
        }
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

    // Per-instance copies of the shared (per-model cached) materials this object renders with.
    // MdxInstance/WmoInstance materials are shared by every instance of the same model, so any
    // per-object material state (the translucent dither, tints...) MUST go through a clone.
    private readonly List<Material> ownedMaterials = new();

    /// <summary>Clones a shared model material into a copy owned by this object (auto-disposed with
    /// it, or hand a <paramref name="bucket"/> for sub-objects with a shorter life, e.g. a mount).
    /// Unknown material types are returned as-is (no per-instance state possible for them).</summary>
    protected Material OwnMaterial(Material shared, List<Material>? bucket = null)
    {
        Material clone;
        if (shared is Material<MdxManager.MdxMaterialData> m2)
            clone = gameContext.Engine.MaterialManager.CloneMaterial(m2);
        else if (shared is Material<WmoManager.WmoMaterialData> wmo)
            clone = gameContext.Engine.MaterialManager.CloneMaterial(wmo);
        else
            return shared;
        (bucket ?? ownedMaterials).Add(clone);
        return clone;
    }

    protected void DisposeMaterials(List<Material> materials)
    {
        foreach (var material in materials)
            gameContext.Engine.MaterialManager.DisposeMaterial(material);
        materials.Clear();
    }

    protected void DisposeOwnedMaterials() => DisposeMaterials(ownedMaterials);

    /// <summary>
    /// Toggles the dithered "half transparent" look (used for pending-delete spawns and placement
    /// phantoms): sets the owned materials' <c>translucent</c> flag, which the m2/wmo shaders turn
    /// into a checkerboard discard. Only affects this instance (materials are per-instance clones).
    /// </summary>
    public virtual void SetTranslucent(bool translucent)
    {
        ApplyTranslucent(ownedMaterials, translucent);
    }

    protected static void ApplyTranslucent(List<Material> materials, bool translucent)
    {
        foreach (var material in materials)
        {
            if (material is Material<MdxManager.MdxMaterialData> m2)
            {
                var data = m2.MaterialData;
                data.translucent = translucent ? 1 : 0;
                m2.SetMaterialData(ref data);
            }
            else if (material is Material<WmoManager.WmoMaterialData> wmo)
            {
                var data = wmo.MaterialData;
                data.translucent = translucent ? 1 : 0;
                wmo.SetMaterialData(ref data);
            }
        }
    }
}