using System.Collections;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheMaths;
using WDE.Common.Database;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public class GameObjectInstance : WorldObjectInstance
{
    private readonly IGameObjectTemplate gameObjectTemplate;
    private readonly uint gameObjectDisplayId;
    private List<INativeBuffer> bonesBuffers = new();
    private M2AnimationComponentData masterAnimation = null!;

    public GameObjectInstance(IGameContext gameContext,
        IGameObjectTemplate gameObjectTemplate,
        uint? gameObjectDisplayId) : base(gameContext)
    {
        this.gameObjectTemplate = gameObjectTemplate;
        this.gameObjectDisplayId = gameObjectDisplayId ?? gameObjectTemplate.DisplayId;
    }

    public Quaternion Rotation
    {
        get => gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation;
        set
        {
            gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation = value;
            WorldObjectEntity.SetDirtyPosition(gameContext.EntityManager);
        }
    }

    public M2? Model { get; private set; }

    public IEnumerator Load()
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var completion = new TaskCompletionSource<MdxManager.MdxInstance?>();
        yield return gameContext.MdxManager.LoadGameObjectModel(gameObjectDisplayId, completion);

        var instance = completion.Task.Result;

        if (instance == null || instance.materials.Length <= 0)
        {
            // lets find some better "placeholder" model
            completion = new();
            yield return gameContext.MdxManager.LoadM2Mesh("world\\arttest\\boxtest\\xyz.m2", completion);
            instance = completion.Task.Result!;
        }

        Model = instance.model;

        objectEntity = entityManager.CreateEntity(archetypes.WorldObjectArchetype);
        objectEntity.SetTRS(entityManager, Vector3.Zero, Quaternion.Identity, instance.scale * gameObjectTemplate.Size * Vector3.One);
        objectEntity.SetDirtyPosition(entityManager);

        var boneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(boneMatricesBuffer);
        boneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityBones(instance.model.bones.Length).Span);

        masterAnimation = new M2AnimationComponentData(instance.model)
        {
            SetNewAnimation = 0,
            _buffer = boneMatricesBuffer
        };
        entityManager.SetManagedComponent(objectEntity, masterAnimation);

        // optimization here, we can share the render data, because we know all the materials will be the same shader
        MaterialInstanceRenderData materialInstanceRenderData = new MaterialInstanceRenderData();
        materialInstanceRenderData.SetBuffer(instance.materials[0], "boneMatrices", boneMatricesBuffer);
        
        int i = 0;
        foreach (var material in instance.materials)
        {
            var collider = entityManager.CreateEntity(archetypes.WorldObjectCollider);
            collider.SetCollider(entityManager, instance.mesh, i, Collisions.COLLISION_MASK_CREATURE);
            collider.SetCopyParentTransform(entityManager, objectEntity);
            collider.SetDirtyPosition(entityManager);
            
            var renderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype);
            renderer.SetRenderer(entityManager, instance.mesh, i, material);
            renderer.SetCopyParentTransform(entityManager, objectEntity);
            renderer.SetDirtyPosition(entityManager);
            entityManager.SetManagedComponent(renderer, materialInstanceRenderData);
            
            renderers.Add(renderer);
            colliders.Add(collider);
            i++;
        }

        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), gameObjectTemplate.Name, 0.25f, Matrix.Identity, 50);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        handles.Add(textEntity);
    }
    
    public override void Dispose()
    {
        if (objectEntity == Entity.Empty)
        {
            throw new Exception("Double dispose!");
        }
        Model = null;
        
        var entityManager = gameContext.EntityManager;
        
        foreach (var entity in handles)
            entityManager.DestroyEntity(entity);

        foreach (var entity in renderers)
            entityManager.DestroyEntity(entity);
        
        foreach (var collider in colliders)
            entityManager.DestroyEntity(collider);
        
        colliders.Clear();
        handles.Clear();
        renderers.Clear();
        
        foreach (var buf in bonesBuffers)
        {
            buf.Dispose();
        }
        
        bonesBuffers.Clear();
        
        entityManager.DestroyEntity(objectEntity);
        objectEntity = Entity.Empty;
    }
}