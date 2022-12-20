using System.Collections;
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

    private List<StaticRenderHandle> handles = new();
    private List<Entity> entities = new();

    public GlobalWorldMapObjectManager(IRenderManager renderManager,
        IEntityManager entityManager,
        Archetypes archetypes,
        WorldManager worldManager,
        WmoManager wmoManager)
    {
        Archetypes = archetypes;
        this.renderManager = renderManager;
        this.entityManager = entityManager;
        this.worldManager = worldManager;
        this.wmoManager = wmoManager;
    }

    public IEnumerator Load()
    {
        if (worldManager.CurrentWdt?.WorldMapObject is not { } wmo) 
            yield break; 
        
        FileId wmoPath = wmo.fileId ?? worldManager.CurrentWdt?.Mwmo!;
        
        var tcs = new TaskCompletionSource<WmoManager.WmoInstance?>();
            
        yield return wmoManager.LoadWorldMapObject(wmoPath, tcs);
        if (tcs.Task.Result == null)
            yield break;

        yield return null;
        
        var wmoTransform = new Transform();
        wmoTransform.Position = wmo.pos;
        wmoTransform.Rotation = Utilities.FromEuler(wmo.rot.X,  wmo.rot.Y + 180, wmo.rot.Z);
                
        foreach (var mesh in tcs.Task.Result.Meshes)
        {
            int i = 0;
            foreach (var material in mesh.Item2)
            {
                handles.Add(renderManager.RegisterStaticRenderer(mesh.Item1.Handle, material, i++, wmoTransform));
                
                if (!material.BlendingEnabled)
                {
                    var entity = entityManager.CreateEntity(Archetypes.CollisionOnlyArchetype);
                    entityManager.GetComponent<LocalToWorld>(entity).Matrix = wmoTransform.LocalToWorldMatrix;
                    entityManager.GetComponent<MeshRenderer>(entity).SubMeshId = i - 1;
                    entityManager.GetComponent<MeshRenderer>(entity).MeshHandle = mesh.Item1.Handle;
                    entityManager.GetComponent<WorldMeshBounds>(entity) = RenderManager.LocalToWorld((MeshBounds)mesh.Item1.Bounds, new LocalToWorld() { Matrix = wmoTransform.LocalToWorldMatrix });   
                    entities.Add(entity);
                }
            }
            
            yield return null;
        }
    }
    
    public IEnumerator Unload()
    {
        foreach (var handle in handles)
            renderManager.UnregisterStaticRenderer(handle);
        
        yield return null;
        
        foreach (var entity in entities)
            entityManager.DestroyEntity(entity);
        
        handles.Clear();
        entities.Clear();
    }
}