using System.Collections;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class GlobalWorldMapObjectManager
{
    public Archetypes Archetypes { get; }
    private readonly IRenderManager renderManager;
    private readonly IEntityManager entityManager;
    private readonly WorldManager worldManager;
    private readonly WmoManager wmoManager;
    private readonly Engine engine;

    private List<StaticRenderHandle> handles = new();
    private List<Entity> entities = new();
    private WmoManager.WmoInstance? currentWmoInstance;

    public GlobalWorldMapObjectManager(IRenderManager renderManager,
        IEntityManager entityManager,
        Archetypes archetypes,
        WorldManager worldManager,
        WmoManager wmoManager,
        Engine engine)
    {
        Archetypes = archetypes;
        this.renderManager = renderManager;
        this.entityManager = entityManager;
        this.worldManager = worldManager;
        this.wmoManager = wmoManager;
        this.engine = engine;
    }

    public async ValueTask Load()
    {
        if (worldManager.CurrentWdt?.WorldMapObject is not { } wmo)
            return;
        
        FileId wmoPath = wmo.fileId ?? worldManager.CurrentWdt?.Mwmo!;

        var wmoInstance = currentWmoInstance = await wmoManager.LoadWorldMapObject(wmoPath);
        if (wmoInstance == null)
            return;

        await engine.NextFrame;

        var wmoTransform = new Transform();
        wmoTransform.Position = wmo.pos;
        wmoTransform.Rotation = Utilities.FromEuler(wmo.rot.X,  wmo.rot.Y + 180, wmo.rot.Z);
                
        foreach (var mesh in wmoInstance.Meshes)
        {
            int i = 0;
            foreach (var material in mesh.Item2)
            {
                handles.Add(renderManager.RegisterStaticRenderer(mesh.Item1.Handle, material, i++, wmoTransform));
                
                if (!material.BlendingEnabled)
                {
                    var entity = entityManager.CreateEntity(Archetypes.CollisionOnlyArchetype, $"Global WMO collider");
                    entityManager.GetComponent<LocalToWorld>(entity).Matrix = wmoTransform.LocalToWorldMatrix;
                    var meshRenderer = new MeshRenderer() { Mesh = mesh.Item1, SubMeshId = i - 1 };
                    entityManager.AddArrayComponent(entity, meshRenderer);
                    entityManager.GetComponent<WorldMeshBounds>(entity) = RenderManager.LocalToWorld((MeshBounds)mesh.Item1.Bounds, new LocalToWorld() { Matrix = wmoTransform.LocalToWorldMatrix });   
                    entities.Add(entity);
                }
            }

            await engine.NextFrame;
        }
    }
    
    public async ValueTask Unload()
    {
        foreach (var handle in handles)
            renderManager.UnregisterStaticRenderer(handle);

        await engine.NextFrame;
        
        foreach (var entity in entities)
            entityManager.DestroyEntity(entity);
        
        handles.Clear();
        entities.Clear();
        currentWmoInstance = null; // free the reference to the WMO instance so it can be garbage collected
    }
}