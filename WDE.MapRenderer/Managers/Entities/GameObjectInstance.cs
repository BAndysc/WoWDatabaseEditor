using System.Collections;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheMaths;
using WDE.Common.Database;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public class GameObjectInstance : WorldObjectInstance
{
    private readonly IGameObjectTemplate gameObjectTemplate;
    private readonly uint gameObjectDisplayId;
    private List<INativeBuffer> bonesBuffers = new();
    private MaterialInstanceRenderData materialInstanceRenderData = null!;

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

    public MaterialInstanceRenderData MaterialRenderData => materialInstanceRenderData;

    public Material BaseMaterial { get; private set; } = null!;

    public IEnumerator Load()
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var completion = new TaskCompletionSource<(MdxManager.MdxInstance?, WmoManager.WmoInstance?)?>();
        yield return gameContext.MdxManager.LoadGameObjectModel(gameObjectDisplayId, completion);
        
        var m2Instance = completion.Task.Result?.Item1;
        var wmoInstance = completion.Task.Result?.Item2;

        if ((m2Instance == null || m2Instance.materials.Length <= 0) && (wmoInstance == null || wmoInstance.meshes.Count == 0))
        {
            // lets find some better "placeholder" model
            var m2completion = new TaskCompletionSource<MdxManager.MdxInstance?>();
            yield return gameContext.MdxManager.LoadM2Mesh("world\\arttest\\boxtest\\xyz.m2", m2completion);
            m2Instance = m2completion.Task.Result!;
        }

        objectEntity = entityManager.CreateEntity(m2Instance == null ? archetypes.WorldObjectArchetype : archetypes.AnimatedWorldObjectArchetype);
        objectEntity.SetTRS(entityManager, Vector3.Zero, Quaternion.Identity, (m2Instance?.scale ?? 1) * gameObjectTemplate.Size * Vector3.One);
        objectEntity.SetDirtyPosition(entityManager);

        materialInstanceRenderData = new MaterialInstanceRenderData();
        if (m2Instance != null)
        {
            var boneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            bonesBuffers.Add(boneMatricesBuffer);
            boneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityBones(m2Instance.model.bones.Length).Span);

            var masterAnimation = new M2AnimationComponentData(m2Instance.model)
            {
                SetNewAnimation = 0,
                _buffer = boneMatricesBuffer
            };
            entityManager.SetManagedComponent(objectEntity, masterAnimation);

            // optimization here, we can share the render data, because we know all the materials will be the same shader
            BaseMaterial = m2Instance.materials[0].material;
            materialInstanceRenderData.SetBuffer(BaseMaterial, "boneMatrices", boneMatricesBuffer);
        
            foreach (var material in m2Instance.materials)
            {
                var renderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype);
                renderer.SetRenderer(entityManager, m2Instance.mesh, material.submesh, material.material);
                renderer.SetCopyParentTransform(entityManager, objectEntity);
                renderer.SetDirtyPosition(entityManager);
                entityManager.SetManagedComponent(renderer, materialInstanceRenderData);
            
                renderers.Add(renderer);
            }            
        }
        else if (wmoInstance != null)
        {
            foreach (var batch in wmoInstance.meshes)
            {
                for (var index = 0; index < batch.Item2.Length; index++)
                {
                    var material = batch.Item2[index];
                    // optimization here, we can share the render data, because we know all the materials will be the same shader
                    BaseMaterial = material;
                    
                    var renderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype);
                    renderer.SetRenderer(entityManager, batch.Item1, index, material);
                    renderer.SetCopyParentTransform(entityManager, objectEntity);
                    renderer.SetDirtyPosition(entityManager);
                    entityManager.SetManagedComponent(renderer, materialInstanceRenderData);
            
                    renderers.Add(renderer);
                }
            }
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

    public float Orientation
    {
        get
        {
            var rot = gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation;
            return rot.Angle() * rot.Axis().Z;
        }
    }
}