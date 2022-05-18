using System.Collections;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheMaths;
using WDE.Common.Database;
using WDE.MapRenderer.Utils;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public class CreatureInstance : WorldObjectInstance
{
    private readonly ICreatureTemplate creatureTemplate;
    private readonly uint creatureDisplayId;
    private List<INativeBuffer> bonesBuffers = new();
    private M2AnimationComponentData masterAnimation = null!;

    public CreatureInstance(IGameContext gameContext,
        ICreatureTemplate creatureTemplate,
        uint? creatureDisplayId) : base(gameContext)
    {
        this.creatureTemplate = creatureTemplate;
        this.creatureDisplayId = creatureDisplayId ?? creatureTemplate.GetRandomModel();
    }

    public float Orientation
    {
        set => gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation = Quaternion.FromEuler(0, MathUtil.RadiansToDegrees(value), 0);
    }

    public M2AnimationType Animation
    {
        set
        {
            // todo: move to animation system
            AnimationDataFlags flags = AnimationDataFlags.None;
            uint animId = (uint)value;
            if (Model != null)
            {
                var animStore = gameContext.DbcManager.AnimationDataStore;
                while (animStore.TryGetValue(animId, out var animationData) &&
                       Model.GetAnimationIndexByAnimationId((int)animId) == null)
                {
                    if (animationData.Fallback != 0)
                    {
                        animId = animationData.Fallback;
                        flags = animationData.Flags;
                    }
                    else
                        break;
                }
            }
            var animData =  gameContext.EntityManager.GetManagedComponent<M2AnimationComponentData>(objectEntity);
            animData.SetNewAnimation = (int)animId;
            animData.Flags = flags;
        }
    }

    public M2? Model { get; private set; }

    public MdxManager.MdxInstance Mount
    {
        set
        {
            var entityManager = gameContext.EntityManager;
            var archetypes = gameContext.Archetypes;
        
            var itemBoneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            bonesBuffers.Add(itemBoneMatricesBuffer);
            itemBoneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityBones(value.model.bones.Length).Span);
               
            MaterialInstanceRenderData itemMaterialInstanceRenderData = new MaterialInstanceRenderData();
            itemMaterialInstanceRenderData.SetBuffer(value.materials[0], "boneMatrices", itemBoneMatricesBuffer);

            var mountAnimationEntity = entityManager.CreateEntity(archetypes.AttachmentsAnimationRootArchetype);
            mountAnimationEntity.SetCopyParentTransform(entityManager, objectEntity);
            mountAnimationEntity.SetDirtyPosition(entityManager);
            var mountAnimationData = entityManager.SetManagedComponent(mountAnimationEntity, new M2AnimationComponentData(value.model)
            {
                SetNewAnimation = 0,
                _buffer = itemBoneMatricesBuffer
            });
            handles.Add(mountAnimationEntity);
        
            int j = 0;
            foreach (var material in value.materials)
            {
                var itemRenderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype);
                itemRenderer.SetRenderer(entityManager, value.mesh, j, material);
                itemRenderer.SetCopyParentTransform(entityManager, objectEntity);
                itemRenderer.SetDirtyPosition(entityManager);
                entityManager.SetManagedComponent(itemRenderer, itemMaterialInstanceRenderData);
                if (!isRenderingEnabled)
                    itemRenderer.SetForceDisabledRendering(entityManager, true);

                renderers.Add(itemRenderer);
                j++;
            }

            masterAnimation.AttachedTo = mountAnimationData;
            masterAnimation.AttachmentType = M2AttachmentType.MountMain;
            Animation = M2AnimationType.Mount;
        }
    }

    public IEnumerator Load()
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var completion = new TaskCompletionSource<MdxManager.MdxInstance?>();
        yield return gameContext.MdxManager.LoadCreatureModel(creatureDisplayId, completion);

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
        objectEntity.SetTRS(entityManager, Vector3.Zero, Quaternion.Identity, instance.scale * creatureTemplate.Scale * Vector3.One);
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

        if (instance.attachments != null)
        {
            foreach (var (attachmentType, itemModel) in instance.attachments)
                AddAttachment(attachmentType, itemModel);
        }
        
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

        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), creatureTemplate.Name, 0.25f, Matrix.Identity, 50);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        handles.Add(textEntity);
    }
    
    private void AddAttachment(M2AttachmentType attachmentType, MdxManager.MdxInstance itemModel)
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var itemBoneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(itemBoneMatricesBuffer);
        itemBoneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityBones(itemModel.model.bones.Length).Span);
               
        MaterialInstanceRenderData itemMaterialInstanceRenderData = new MaterialInstanceRenderData();
        itemMaterialInstanceRenderData.SetBuffer(itemModel.materials[0], "boneMatrices", itemBoneMatricesBuffer);

        var itemAnimationEntity = entityManager.CreateEntity(archetypes.AttachmentsAnimationRootArchetype);
        itemAnimationEntity.SetCopyParentTransform(entityManager, objectEntity);
        itemAnimationEntity.SetDirtyPosition(entityManager);
        entityManager.SetManagedComponent(itemAnimationEntity, new M2AnimationComponentData(itemModel.model, masterAnimation, attachmentType)
        {
            SetNewAnimation = 0,
            _buffer = itemBoneMatricesBuffer
        });
        handles.Add(itemAnimationEntity);
        
        int j = 0;
        foreach (var material in itemModel.materials)
        {
            var itemRenderer = entityManager.CreateEntity(archetypes.WorldObjectMeshRendererArchetype);
            itemRenderer.SetRenderer(entityManager, itemModel.mesh, j, material);
            itemRenderer.SetCopyParentTransform(entityManager, objectEntity);
            itemRenderer.SetDirtyPosition(entityManager);
            entityManager.SetManagedComponent(itemRenderer, itemMaterialInstanceRenderData);
            if (!isRenderingEnabled)
                itemRenderer.SetForceDisabledRendering(entityManager, true);

            renderers.Add(itemRenderer);
            j++;
        }
    }

    public IEnumerator SetVirtualItem(int slot, Item itemInfo)
    {
        var weaponModelCompletion = new TaskCompletionSource<MdxManager.MdxInstance?>();
        yield return gameContext.MdxManager.LoadItemMesh(itemInfo.DisplayId, false, weaponModelCompletion);

        var weaponModel = weaponModelCompletion.Task.Result;

        if (weaponModel == null)
            yield break;

        AddAttachment(slot is 0 or 2 ? M2AttachmentType.ItemVisual1 : M2AttachmentType.ItemVisual0, weaponModel);
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