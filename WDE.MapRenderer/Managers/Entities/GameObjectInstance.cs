using System.Collections;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;
using WDE.Common.Database;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public class GameObjectInstance : WorldObjectInstance
{
    private readonly IGameObjectTemplate gameObjectTemplate;
    private readonly uint gameObjectDisplayId;
    private List<INativeBuffer> bonesBuffers = new();

    public GameObjectInstance(IGameContext gameContext,
        IGameObjectTemplate gameObjectTemplate,
        uint? gameObjectDisplayId,
        RenderLayer renderLayer) : base(gameContext, renderLayer)
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

    public Material BaseMaterial { get; private set; } = null!;

    public async ValueTask Load()
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;

        var model = await gameContext.MdxManager.LoadGameObjectModel(gameObjectDisplayId);
        
        var m2Instance = model?.Item1;
        var wmoInstance = model?.Item2;

        if ((m2Instance == null || m2Instance.materials.Length <= 0) && (wmoInstance == null || wmoInstance.meshes.Count == 0))
        {
            // lets find some better "placeholder" model
            m2Instance = await gameContext.MdxManager.LoadM2Mesh("world\\arttest\\boxtest\\xyz.m2")!;
        }

        objectEntity = entityManager.CreateEntity(m2Instance == null ? archetypes.WorldObjectArchetype : archetypes.AnimatedWorldObjectArchetype, gameObjectTemplate?.Name);
        objectEntity.SetTRS(entityManager, Vector3.Zero, Quaternion.Identity, (m2Instance?.scale ?? 1) * (gameObjectTemplate?.Size ?? 1) * Vector3.One);
        objectEntity.SetDirtyPosition(entityManager);
        objectEntity.SetRenderLayer(entityManager, renderLayer);

        if (m2Instance != null)
        {
            mdxInstances.Add(m2Instance);
            var boneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            bonesBuffers.Add(boneMatricesBuffer);
            boneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(m2Instance.model.bones.Length).Span);

            var colorBuffer = gameContext.Engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Float4);
            bonesBuffers.Add(colorBuffer);
            colorBuffer.UpdateBuffer(AnimationSystem.IdentityColors(m2Instance.model.colors.Length).Span);

            var textureTransformsBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferPixelOnly, 1, BufferInternalFormat.Float4);
            bonesBuffers.Add(textureTransformsBuffer);
            textureTransformsBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(m2Instance.model.texture_transforms.Length + 1).Span);

            var masterAnimation = new M2AnimationComponentData(m2Instance.model)
            {
                SetNewAnimation = 0,
                _buffer = boneMatricesBuffer,
                _colors = colorBuffer,
                _textureTransforms = textureTransformsBuffer
            };
            entityManager.SetManagedComponent(objectEntity, masterAnimation);
            entityManager.SetManagedComponent(objectEntity, new MdxRenderer(m2Instance));

            // optimization here, we can share the render data, because we know all the materials will be the same shader
            BaseMaterial = m2Instance.materials[0].material;

            foreach (var material in m2Instance.materials)
            {
                var materialInstanceRenderData = new MaterialInstanceRenderData();
                materialInstanceRenderData.SetBuffer("boneMatrices", boneMatricesBuffer);
                materialInstanceRenderData.SetBuffer("vertexColors", colorBuffer);
                materialInstanceRenderData.SetBuffer("textureTransforms", textureTransformsBuffer);
                materialInstanceRenderData.InstanceData = new Int4(material.batch.colorIndex, material.batch.textureTransformIndex, material.batch.textureTransformIndex2, 0);

                var renderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype, $"Renderer of {gameObjectTemplate?.Name}");
                renderer.SetRenderer(entityManager, m2Instance.mesh, material.submesh, material.material);
                renderer.SetCopyParentTransform(entityManager, objectEntity);
                renderer.SetDirtyPosition(entityManager);
                renderer.SetRenderLayer(entityManager, renderLayer);
                entityManager.SetManagedComponent(renderer, materialInstanceRenderData);
            
                renderers.Add(renderer);
            }            
        }
        else if (wmoInstance != null)
        {
            wmoInstances.Add(wmoInstance);
            foreach (var batch in wmoInstance.meshes)
            {
                for (var index = 0; index < batch.Item2.Length; index++)
                {
                    var material = batch.Item2[index];
                    // optimization here, we can share the render data, because we know all the materials will be the same shader
                    BaseMaterial = material;
                    
                    var renderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype, $"Renderer of {gameObjectTemplate?.Name}");
                    renderer.SetRenderer(entityManager, batch.Item1, index, material);
                    renderer.SetCopyParentTransform(entityManager, objectEntity);
                    renderer.SetDirtyPosition(entityManager);
                    renderer.SetRenderLayer(entityManager, renderLayer);
                    var materialInstanceRenderData = new MaterialInstanceRenderData();
                    entityManager.SetManagedComponent(renderer, materialInstanceRenderData);
            
                    renderers.Add(renderer);
                }
            }
        }

        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), gameObjectTemplate?.Name ?? "", 0.25f, Matrix.Identity, 50);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        textEntity.SetRenderLayer(entityManager, renderLayer);
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