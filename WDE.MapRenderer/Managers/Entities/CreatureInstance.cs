using System.Collections;
using System.Text;
using ImGuiNET;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheEngine.Utils;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Utils;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public class MdxRenderer : IManagedComponentData
{
    public readonly M2 Model;
    // maps a geoset id to an index within the Owner's MeshRenderer array component
    public List<(int geoset, int rendererIndex)> Geosets = new();
    public Entity Owner;
    public readonly FileId ModelFileId;
    public readonly uint DisplayId;

    public MdxRenderer(MdxManager.MdxInstance mdx)
    {
        Model = mdx.model;
        ModelFileId = mdx.fileId;
        DisplayId = mdx.displayId;
    }
}

[AutoRegister]
public class MdxRendererInspector : IInspectorDrawer<MdxRenderer>
{
    private readonly IEntityManager entityManager;
    private readonly IUIManager uiManager;
    private readonly EntityInspector entityInspector;

    public MdxRendererInspector(IEntityManager entityManager,
        IUIManager uiManager,
        EntityInspector entityInspector)
    {
        this.entityManager = entityManager;
        this.uiManager = uiManager;
        this.entityInspector = entityInspector;
    }

    public byte[] ModelBufferString = new byte[256];

    public void Draw(MdxRenderer component)
    {
        Encoding.ASCII.GetBytes(component.ModelFileId.ToString(), ModelBufferString);
        ImGui.InputText("Model: ", ModelBufferString, (uint)ModelBufferString.Length);
        ImGui.Text($"Display ID: {component.DisplayId}");
        if (component.Owner != Entity.Empty && component.Geosets.Count > 0)
        {
            var renderers = entityManager.GetArrayComponents<MeshRenderer>(component.Owner);
            var groupedGeosets = component.Geosets.GroupBy(x => x.geoset).OrderBy(x => x.Key).ToList();
            foreach (var group in groupedGeosets)
            {
                var geoset = group.Key;
                var isRendered = !renderers[group.First().rendererIndex].Hidden;
                if (ImGui.Checkbox($"Geoset {geoset}", ref isRendered))
                {
                    foreach (var (_, rendererIndex) in group)
                    {
                        renderers[rendererIndex].Hidden = !isRendered;
                    }
                }
            }
        }

        if (ImGui.CollapsingHeader("M2"))
        {
            entityInspector.DrawInspector(component.Model);
        }
    }
}

public class CreatureInstance : WorldObjectInstance
{
    private readonly ICreatureTemplate? creatureTemplate;
    public readonly uint CreatureDisplayId;
    private List<INativeBuffer> bonesBuffers = new();
    public M2AnimationComponentData masterAnimation = null!;
    private MaterialInstanceRenderData materialInstanceRenderData = null!;
    private string unitName = "";

    public CreatureInstance(IGameContext gameContext,
        ICreatureTemplate creatureTemplate,
        uint? creatureDisplayId,
        RenderLayer renderLayer) : base(gameContext, renderLayer)
    {
        this.creatureTemplate = creatureTemplate;
        unitName = creatureTemplate.Name;
        this.CreatureDisplayId = creatureDisplayId ?? creatureTemplate.GetRandomModel();
    }

    public CreatureInstance(IGameContext gameContext,
        string playerName,
        uint displayId,
        RenderLayer renderLayer) : base(gameContext, renderLayer)
    {
        unitName = playerName;
        CreatureDisplayId = displayId;
    }

    public float Orientation
    {
        get
        {
            var rot = gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation;
            return rot.Angle() * rot.Axis().Z;
        }
        set
        {
            gameContext.EntityManager.GetComponent<LocalToWorld>(objectEntity).Rotation = Utilities.FromEuler(0, MathUtil.RadiansToDegrees(value), 0);
            objectEntity.SetDirtyPosition(gameContext.EntityManager);
        }
    }

    private (int animId, AnimationDataFlags flags) GetAnimId(M2AnimationType type)
    {
        AnimationDataFlags flags = AnimationDataFlags.None;
        int animId = (int)type;
        if (Model != null)
        {
            var animStore = gameContext.DbcManager.AnimationDataStore;
            Span<bool> visited = stackalloc bool[(int)animStore.MaxId + 1];
            while (animStore.TryGetValue((uint)animId, out var animationData) &&
                   Model.GetAnimationIndexByAnimationId((int)animId) == null &&
                   !visited[(int)animId])
            {
                visited[(int)animId] = true;
                if (animationData.Fallback != 0)
                {
                    animId = (int)animationData.Fallback;
                    flags = animationData.Flags;
                }
                else
                    break;
            }
        }
        return (animId, flags);
    }

    public M2AnimationType Animation
    {
        set
        {
            // todo: move to animation system
            var (animId, flags) = GetAnimId(value);
            var animData = gameContext.EntityManager.GetManagedComponent<M2AnimationComponentData>(objectEntity);
            if (animData == null)
            {
                throw new Exception();
            }
            animData.SetNewAnimation = (int)animId;
            animData.Flags = flags;
        }
    }

    public M2AnimationType OneShotAnimation
    {
        set
        {
            // todo: move to animation system
            var (animId, flags) = GetAnimId(value);
            var animData = gameContext.EntityManager.GetManagedComponent<M2AnimationComponentData>(objectEntity);
            animData.SetNewOneShotAnimation = (int)animId;
            animData.OneShotFlags = flags;
        }
    }

    public float AnimationSpeed
    {
        set
        {
            var animData = gameContext.EntityManager.GetManagedComponent<M2AnimationComponentData>(objectEntity);
            animData.SpeedModifier = value;
        }
    }

    public MdxManager.MdxInstance? Mdx { get; private set; }
    public M2? Model => Mdx?.model;

    public MaterialInstanceRenderData MaterialRenderData => materialInstanceRenderData;
    
    public Material BaseMaterial { get; private set; } = null!;

    private class MountData
    {
        public MdxManager.MdxInstance mdxInstance; // to keep reference to the mesh
        public INativeBuffer<Matrix4x4> boneMatricesBuffer;
        public INativeBuffer<Matrix4x4> textureTransformsBuffer;
        public INativeBuffer<Vector4> colorBuffer;
        public Entity mountEntity;
        public SmallList<Entity> renderers;

        public void Destroy(IEntityManager entityManager)
        {
            foreach (var entity in renderers)
            {
                entityManager.DestroyEntity(entity);
            }
            entityManager.DestroyEntity(mountEntity);
            boneMatricesBuffer.Dispose();
            textureTransformsBuffer.Dispose();
            colorBuffer.Dispose();
        }
    }

    private MountData? mountData;
    public MdxManager.MdxInstance? Mount
    {
        get => mountData?.mdxInstance;
        set
        {
            var entityManager = gameContext.EntityManager;
            var archetypes = gameContext.Archetypes;

            this.mountData?.Destroy(entityManager);
            // todo: delete would be better?
            entityManager.AddComponent(WorldObjectEntity, new ShareRenderEnabledBit(){OtherEntity = Entity.Empty});
            this.mountData = null;

            if (value == null)
                return;
            mountData = new();
            mountData.mdxInstance = value;
        
            mountData.boneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
            mountData.boneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(value.model.bones.Length).Span);
            mountData.colorBuffer = gameContext.Engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Float4);
            mountData.colorBuffer.UpdateBuffer(AnimationSystem.IdentityColors(value.model.colors.Length).Span);
            mountData.textureTransformsBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferPixelOnly, 1, BufferInternalFormat.Float4);
            mountData.textureTransformsBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(value.model.texture_transforms.Length + 1).Span);

            mountData.mountEntity = entityManager.CreateEntity(archetypes.AttachmentArchetype, "Mount"u8);
            mountData.mountEntity.SetCopyParentTransform(entityManager, objectEntity);
            mountData.mountEntity.SetDirtyPosition(entityManager);
            mountData.mountEntity.SetRenderLayer(entityManager, renderLayer);
            var mountAnimationData = entityManager.SetManagedComponent(mountData.mountEntity, new M2AnimationComponentData(value.model)
            {
                SetNewAnimation = 0,
                _buffer = mountData.boneMatricesBuffer,
                _colors = mountData.colorBuffer,
                _textureTransforms = mountData.textureTransformsBuffer
            });
            entityManager.AddComponent(mountData.mountEntity, new ShareRenderEnabledBit(){OtherEntity = WorldObjectEntity});
            entityManager.AddComponent(WorldObjectEntity, new ShareRenderEnabledBit(){OtherEntity = mountData.mountEntity});
            entityManager.AddManagedComponent(mountData.mountEntity, new MdxRenderer(value) { Owner = mountData.mountEntity });
            MaterialInstanceRenderData itemMaterialInstanceRenderData = new MaterialInstanceRenderData();
            itemMaterialInstanceRenderData.SetBuffer("boneMatrices", mountData.boneMatricesBuffer);
            itemMaterialInstanceRenderData.SetBuffer("vertexColors", mountData.colorBuffer);
            itemMaterialInstanceRenderData.SetBuffer("textureTransforms", mountData.textureTransformsBuffer);
            entityManager.SetManagedComponent(mountData.mountEntity, itemMaterialInstanceRenderData);
            if (!isRenderingEnabled)
                mountData.mountEntity.SetForceDisabledRendering(entityManager, true);

            foreach (var material in value.materials)
            {
                var instanceData = new Int4(material.batch.colorIndex, material.batch.textureTransformIndex, material.batch.textureTransformIndex2, 0);

                mountData.mountEntity.SetRenderer(entityManager, value.mesh, material.submesh, material.material, instanceData);
            }

            masterAnimation.AttachedTo = mountAnimationData;
            masterAnimation.AttachedTo = mountAnimationData;
            masterAnimation.AttachmentType = M2AttachmentType.MountMain;
            Animation = M2AnimationType.Mount;
        }
    }

    public async ValueTask Load()
    {
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;

        var instance = await gameContext.MdxManager.LoadCreatureModel(CreatureDisplayId);

        if (instance == null || instance.materials.Length <= 0)
        {
            // lets find some better "placeholder" model
            instance = await gameContext.MdxManager.LoadM2Mesh("world\\arttest\\boxtest\\xyz.m2");
        }

        if (disposed)
        {
            return; // we are disposed, so we don't need to load anything
        }

        Mdx = instance;

        objectEntity = entityManager.CreateEntity(archetypes.AnimatedWorldObjectArchetype, unitName);
        objectEntity.SetTRS(entityManager, Vector3.Zero, Quaternion.Identity, instance.scale * (creatureTemplate?.Scale ?? 1) * Vector3.One);
        objectEntity.SetDirtyPosition(entityManager);
        objectEntity.SetRenderLayer(entityManager, renderLayer);

        var boneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(boneMatricesBuffer);
        boneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(instance.model.bones.Length).Span);
        var colorBuffer = gameContext.Engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(colorBuffer);
        colorBuffer.UpdateBuffer(AnimationSystem.IdentityColors(instance.model.colors.Length).Span);
        var textureTransformsBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferPixelOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(textureTransformsBuffer);
        textureTransformsBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(instance.model.texture_transforms.Length + 1).Span);

        masterAnimation = new M2AnimationComponentData(instance.model)
        {
            SetNewAnimation = 0,
            _buffer = boneMatricesBuffer,
            _colors = colorBuffer,
            _textureTransforms = textureTransformsBuffer,
        };
        entityManager.SetManagedComponent(objectEntity, masterAnimation);

        // optimization here, we can share the render data, because we know all the materials will be the same shader
        BaseMaterial = instance.materials[0].material;

        if (instance.attachments != null)
        {
            foreach (var (attachmentType, itemModel) in instance.attachments)
                AddAttachment(attachmentType, itemModel);
        }

        materialInstanceRenderData = new MaterialInstanceRenderData();
        materialInstanceRenderData.SetBuffer("boneMatrices", boneMatricesBuffer);
        materialInstanceRenderData.SetBuffer("vertexColors", colorBuffer);
        materialInstanceRenderData.SetBuffer("textureTransforms", textureTransformsBuffer);
        entityManager.SetManagedComponent(objectEntity, materialInstanceRenderData);

        var mdxRenderer = new MdxRenderer(instance) { Owner = objectEntity };
        entityManager.SetManagedComponent(objectEntity, mdxRenderer);
        foreach (var material in instance.materials)
        {
            var instanceData = new Int4(material.batch.colorIndex, material.batch.textureTransformIndex, material.batch.textureTransformIndex2, 0);

            var rendererIndex = objectEntity.SetRenderer(entityManager, instance.mesh, material.submesh, material.material, instanceData,
                hidden: !material.batch.activeByDefault);
            mdxRenderer.Geosets.Add((material.batch.geoset, rendererIndex));
        }

        var size = instance.mesh.Bounds.Size / 2;
        
        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), unitName, 0.25f, Matrix.Identity, 50);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity, Local = Matrix4x4.CreateTranslation(new Vector3(0, 0, size.Z))});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        textEntity.SetRenderLayer(entityManager, renderLayer);
        handles.Add(textEntity);
    }

    private Entity chatEntity;

    public void Say(CreatureTextType textType, string text)
    {
        StopSaying();
        text = SplitLongText(text);
        var entityManager = gameContext.EntityManager;
        var size = (Model.bounding_box.max.Z - Model.bounding_box.min.Z) / 2;
        chatEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), text, 0.25f, Matrix.Identity, 50,
            textType.GetTextColor(), new Vector4(0, 0, 0, 0.9f));
        entityManager.AddComponent(chatEntity, new CopyParentTransform(){Parent = objectEntity, Local = Matrix4x4.CreateTranslation(new Vector3(0, 0, size))});
        entityManager.AddComponent(chatEntity, new DirtyPosition(true));
        handles.Add(chatEntity);
    }

    private static string SplitLongText(string text)
    {
        int maxLength =50;
        if (text.Length <= maxLength)
            return text;
        int startOffset = 0;
        Span<char> sb = stackalloc char[text.Length * 2];
        int destOffset = 0;
        while (startOffset < text.Length)
        {
            if (text[startOffset] == ' ') { startOffset++; continue; }
            if (text.Length - startOffset < maxLength)
            {
                text.AsSpan(startOffset).CopyTo(sb.Slice(destOffset));
                startOffset = text.Length;
            }
            else
            {
                var subStr = text.AsSpan(startOffset, maxLength);
                var nextSpaceIndex = text.AsSpan(startOffset + subStr.Length).IndexOf(' ');
                if (nextSpaceIndex == -1)
                {
                    subStr = text.AsSpan(startOffset);
                }
                else
                {
                    subStr = text.AsSpan(startOffset, nextSpaceIndex + maxLength);
                }
                subStr.CopyTo(sb.Slice(destOffset));
                destOffset += subStr.Length;
                sb[destOffset++] = '\n'; // add a new line
                startOffset += subStr.Length;
            }
        }

        return sb.ToString();
    }

    public void StopSaying()
    {
        if (chatEntity != Entity.Empty)
        {
            var entityManager = gameContext.EntityManager;
            entityManager.DestroyEntity(chatEntity);
            handles.Remove(chatEntity);
            chatEntity = Entity.Empty;
        }
    }
    
    private void AddAttachment(M2AttachmentType attachmentType, MdxManager.MdxInstance itemModel)
    {
        mdxInstances.Add(itemModel);
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var itemBoneMatricesBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(itemBoneMatricesBuffer);
        itemBoneMatricesBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(itemModel.model.bones.Length).Span);
        var itemColorBuffer = gameContext.Engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBuffer, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(itemColorBuffer);
        itemColorBuffer.UpdateBuffer(AnimationSystem.IdentityColors(itemModel.model.colors.Length).Span);
        var itemTextureTransformsBuffer = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferPixelOnly, 1, BufferInternalFormat.Float4);
        bonesBuffers.Add(itemTextureTransformsBuffer);
        itemTextureTransformsBuffer.UpdateBuffer(AnimationSystem.IdentityMatrix(itemModel.model.texture_transforms.Length + 1).Span);

        var itemEntity = entityManager.CreateEntity(archetypes.AttachmentArchetype, $"Item of {unitName}");
        itemEntity.SetCopyParentTransform(entityManager, objectEntity);
        itemEntity.SetDirtyPosition(entityManager);
        itemEntity.SetRenderLayer(entityManager, renderLayer);
        entityManager.SetManagedComponent(itemEntity, new M2AnimationComponentData(itemModel.model, masterAnimation, attachmentType)
        {
            SetNewAnimation = 0,
            _buffer = itemBoneMatricesBuffer,
            _colors = itemColorBuffer,
            _textureTransforms = itemTextureTransformsBuffer
        });
        handles.Add(itemEntity);
        MaterialInstanceRenderData itemMaterialInstanceRenderData = new MaterialInstanceRenderData();
        itemMaterialInstanceRenderData.SetBuffer("boneMatrices", itemBoneMatricesBuffer);
        itemMaterialInstanceRenderData.SetBuffer("vertexColors", itemColorBuffer);
        itemMaterialInstanceRenderData.SetBuffer("textureTransforms", itemTextureTransformsBuffer);
        entityManager.SetManagedComponent(itemEntity, itemMaterialInstanceRenderData);
        if (!isRenderingEnabled)
            itemEntity.SetForceDisabledRendering(entityManager, true);

        foreach (var material in itemModel.materials)
        {
            var instanceData = new Int4(material.batch.colorIndex, material.batch.textureTransformIndex, material.batch.textureTransformIndex2, 0);
            itemEntity.SetRenderer(entityManager, itemModel.mesh, material.submesh, material.material, instanceData);
        }
    }

    public async ValueTask SetVirtualItem(int slot, uint itemId, CancellationToken cancellationToken)
    {
        if (itemId != 0 &&
            gameContext.DbcManager.ItemStore.TryGetValue(itemId, out var item))
        {
            await SetVirtualItem(slot, item, cancellationToken);
        }
    }

    public async ValueTask SetVirtualItem(int slot, Item itemInfo, CancellationToken cancellationToken)
    {
        var weaponModel = await gameContext.MdxManager.LoadItemMesh(itemInfo.DisplayId, false, 0, 0);

        if (weaponModel == null)
            return;

        if (cancellationToken.IsCancellationRequested)
            return;
        AddAttachment(slot is 0 or 2 ? M2AttachmentType.ItemVisual1 : M2AttachmentType.ItemVisual0, weaponModel);
    }

    private bool disposed;
    
    public override void Dispose()
    {
        if (disposed)
        {
            throw new Exception("Double dispose!");
        }

        disposed = true;
        if (objectEntity == Entity.Empty)
        {
            return; // not yet loaded
        }
        Mdx = null;
        chatEntity = Entity.Empty;
        pendingMountDisplayId = 0; // to make sure we don't load mount model after dispose
        
        var entityManager = gameContext.EntityManager;

        mountData?.Destroy(entityManager);

        foreach (var entity in handles)
            entityManager.DestroyEntity(entity);

        foreach (var collider in colliders)
            entityManager.DestroyEntity(collider);
        
        colliders.Clear();
        handles.Clear();
        
        foreach (var buf in bonesBuffers)
        {
            buf.Dispose();
        }
        
        bonesBuffers.Clear();
        
        entityManager.DestroyEntity(objectEntity);
        objectEntity = Entity.Empty;
    }

    private uint pendingMountDisplayId = 0;

    public async ValueTask LoadMount(uint mountDisplayId)
    {
        pendingMountDisplayId = mountDisplayId;
        if (mountDisplayId == 0)
        {
            Mount = null;
            return;
        }
        var mountModel = await gameContext.MdxManager.LoadCreatureModel(mountDisplayId);
        if (mountModel != null && pendingMountDisplayId == mountDisplayId)
            Mount = mountModel;
    }
}

