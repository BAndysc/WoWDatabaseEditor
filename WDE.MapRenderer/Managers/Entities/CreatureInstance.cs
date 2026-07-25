using System.Collections;
using System.Text;
using Hexa.NET.ImGui;
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

    public unsafe void Draw(MdxRenderer component)
    {
        Encoding.ASCII.GetBytes(component.ModelFileId.ToString(), ModelBufferString);
        fixed (byte* ptr = &ModelBufferString[0])
        {
            ImGui.InputText("Model: \0"u8, ptr, (uint)ModelBufferString.Length);
        }
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

        if (ImGui.CollapsingHeader("M2"u8))
        {
            entityInspector.DrawInspector(component.Model);
        }
    }
}

public class CreatureInstance : WorldObjectInstance
{
    private readonly ICreatureTemplate? creatureTemplate;
    public readonly uint CreatureDisplayId;
    public M2AnimationComponentData masterAnimation = null!;
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

    private unsafe (int animId, AnimationDataFlags flags) GetAnimId(M2AnimationType type)
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
                if (animationData->Fallback != 0)
                {
                    animId = (int)animationData->Fallback;
                    flags = animationData->Flags;
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

    public override void SetTranslucent(bool translucent)
    {
        base.SetTranslucent(translucent); // body + equipment attachments (owned materials)
        if (mountData != null)
            ApplyTranslucent(mountData.materials, translucent);
    }

    public Material BaseMaterial { get; private set; } = null!;

    private class MountData
    {
        public MdxManager.MdxInstance mdxInstance; // to keep reference to the mesh
        public Entity mountEntity;
        public List<Material> materials = new(); // per-instance clones (shorter life than the creature)

        public void Destroy(IEntityManager entityManager)
        {
            entityManager.DestroyEntity(mountEntity);
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

            if (this.mountData != null)
            {
                this.mountData.Destroy(entityManager);
                DisposeMaterials(this.mountData.materials);
            }
            // todo: delete would be better?
            entityManager.AddComponent(WorldObjectEntity, new ShareRenderEnabledBit(){OtherEntity = Entity.Empty});
            this.mountData = null;

            if (value == null)
                return;
            mountData = new();
            mountData.mdxInstance = value;

            var mountModel = value.model;
            var animSystem = gameContext.AnimationSystem;
            var (mountBoneBase, mountColorBase, mountTexBase) = animSystem.AllocateAnimationSlots(mountModel);

            mountData.mountEntity = entityManager.CreateEntity(archetypes.AttachmentArchetype, "Mount"u8);
            entityManager.SetParent(mountData.mountEntity, objectEntity);
            mountData.mountEntity.SetCopyParentTransform(entityManager, objectEntity);
            mountData.mountEntity.SetDirtyPosition(entityManager);
            mountData.mountEntity.SetRenderLayer(entityManager, renderLayer);
            var mountAnimationData = entityManager.SetManagedComponent(mountData.mountEntity, new M2AnimationComponentData(mountModel)
            {
                SetNewAnimation = 0,
                BoneBase = mountBoneBase,
                ColorBase = mountColorBase,
                TexTransformBase = mountTexBase,
                _boneCache = AnimationSystem.IdentityMatrix(mountModel.bones.Length).ToArray(),
                _colorCache = AnimationSystem.IdentityColors(mountModel.colors.Length).ToArray(),
                _texTransformCache = AnimationSystem.IdentityMatrix(mountModel.texture_transforms.Length + 1).ToArray(),
            });
            entityManager.AddComponent(mountData.mountEntity, new ShareRenderEnabledBit(){OtherEntity = WorldObjectEntity});
            entityManager.AddComponent(WorldObjectEntity, new ShareRenderEnabledBit(){OtherEntity = mountData.mountEntity});
            entityManager.AddManagedComponent(mountData.mountEntity, new MdxRenderer(value) { Owner = mountData.mountEntity });
            if (!isRenderingEnabled)
                mountData.mountEntity.SetForceDisabledRendering(entityManager, true);

            foreach (var material in value.materials)
            {
                var instanceData = new Int4(
                    material.batch.colorIndex < 0 ? -1 : mountColorBase + material.batch.colorIndex,
                    material.batch.textureTransformIndex < 0 ? -1 : mountTexBase + material.batch.textureTransformIndex,
                    material.batch.textureTransformIndex2 < 0 ? -1 : mountTexBase + material.batch.textureTransformIndex2,
                    mountBoneBase);
                mountData.mountEntity.SetRenderer(entityManager, value.mesh, material.submesh, OwnMaterial(material.material, mountData.materials), instanceData);
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

        var animSystem = gameContext.AnimationSystem;
        var model = instance.model;
        var (boneBase, colorBase, texBase) = animSystem.AllocateAnimationSlots(model);

        masterAnimation = new M2AnimationComponentData(model)
        {
            SetNewAnimation = 0,
            BoneBase = boneBase,
            ColorBase = colorBase,
            TexTransformBase = texBase,
            _boneCache = AnimationSystem.IdentityMatrix(model.bones.Length).ToArray(),
            _colorCache = AnimationSystem.IdentityColors(model.colors.Length).ToArray(),
            _texTransformCache = AnimationSystem.IdentityMatrix(model.texture_transforms.Length + 1).ToArray(),
        };
        entityManager.SetManagedComponent(objectEntity, masterAnimation);

        if (instance.attachments != null)
        {
            foreach (var (attachmentType, itemModel) in instance.attachments)
                AddAttachment(attachmentType, itemModel);
        }

        var mdxRenderer = new MdxRenderer(instance) { Owner = objectEntity };
        entityManager.SetManagedComponent(objectEntity, mdxRenderer);
        foreach (var material in instance.materials)
        {
            var instanceData = new Int4(
                material.batch.colorIndex < 0 ? -1 : colorBase + material.batch.colorIndex,
                material.batch.textureTransformIndex < 0 ? -1 : texBase + material.batch.textureTransformIndex,
                material.batch.textureTransformIndex2 < 0 ? -1 : texBase + material.batch.textureTransformIndex2,
                boneBase);
            var ownMaterial = OwnMaterial(material.material); // per-instance copy (dither etc. must not leak onto shared model materials)
            BaseMaterial ??= ownMaterial;
            var rendererIndex = objectEntity.SetRenderer(entityManager, instance.mesh, material.submesh, ownMaterial, instanceData,
                hidden: !material.batch.activeByDefault);
            mdxRenderer.Geosets.Add((material.batch.geoset, rendererIndex));
        }

        var size = instance.mesh.Bounds.Size / 2;
        
        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), unitName, 0.25f, Matrix.Identity, 50);
        entityManager.SetParent(textEntity, objectEntity);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity, Local = Matrix4x4.CreateTranslation(new Vector3(0, 0, size.Z))});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        textEntity.SetRenderLayer(entityManager, renderLayer);

        // status icons (quest !/?, gossip, AI) anchor above the nameplate. The nameplate renders
        // ~[0.85, 1.15] local units ABOVE its own anchor (world_text.vert offsets glyphs by
        // "1.0 - y"), and everything local is scaled by the entity transform - so the icon anchor
        // (world space) is (name anchor + text extent + gap) * scale
        if (creatureTemplate != null)
        {
            var worldScale = instance.scale * creatureTemplate.Scale;
            gameContext.StatusIconsManager.Register(this, creatureTemplate, (size.Z + 1.25f) * worldScale);
        }
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
        entityManager.SetParent(chatEntity, objectEntity);
        entityManager.AddComponent(chatEntity, new CopyParentTransform(){Parent = objectEntity, Local = Matrix4x4.CreateTranslation(new Vector3(0, 0, size))});
        entityManager.AddComponent(chatEntity, new DirtyPosition(true));
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
            chatEntity = Entity.Empty;
        }
    }
    
    private void AddAttachment(M2AttachmentType attachmentType, MdxManager.MdxInstance itemModel)
    {
        mdxInstances.Add(itemModel);
        var entityManager = gameContext.EntityManager;
        var archetypes = gameContext.Archetypes;
        
        var itemAnimSystem = gameContext.AnimationSystem;
        var itemM = itemModel.model;
        var (itemBoneBase, itemColorBase, itemTexBase) = itemAnimSystem.AllocateAnimationSlots(itemM);

        var itemEntity = entityManager.CreateEntity(archetypes.AttachmentArchetype, $"Item of {unitName}");
        entityManager.SetParent(itemEntity, objectEntity);
        itemEntity.SetCopyParentTransform(entityManager, objectEntity);
        itemEntity.SetDirtyPosition(entityManager);
        itemEntity.SetRenderLayer(entityManager, renderLayer);
        entityManager.SetManagedComponent(itemEntity, new M2AnimationComponentData(itemM, masterAnimation, attachmentType)
        {
            SetNewAnimation = 0,
            BoneBase = itemBoneBase,
            ColorBase = itemColorBase,
            TexTransformBase = itemTexBase,
            _boneCache = AnimationSystem.IdentityMatrix(itemM.bones.Length).ToArray(),
            _colorCache = AnimationSystem.IdentityColors(itemM.colors.Length).ToArray(),
            _texTransformCache = AnimationSystem.IdentityMatrix(itemM.texture_transforms.Length + 1).ToArray(),
        });
        if (!isRenderingEnabled)
            itemEntity.SetForceDisabledRendering(entityManager, true);

        foreach (var material in itemModel.materials)
        {
            var instanceData = new Int4(
                material.batch.colorIndex < 0 ? -1 : itemColorBase + material.batch.colorIndex,
                material.batch.textureTransformIndex < 0 ? -1 : itemTexBase + material.batch.textureTransformIndex,
                material.batch.textureTransformIndex2 < 0 ? -1 : itemTexBase + material.batch.textureTransformIndex2,
                itemBoneBase);
            itemEntity.SetRenderer(entityManager, itemModel.mesh, material.submesh, OwnMaterial(material.material), instanceData);
        }
    }

    public async ValueTask SetVirtualItem(int slot, uint itemId, CancellationToken cancellationToken)
    {
        uint? itemDisplayInfo = null;
        unsafe
        {
            if (itemId != 0 &&
                gameContext.DbcManager.ItemStore.TryGetValue(itemId, out var item))
            {
                itemDisplayInfo = item->DisplayId;
            }
        }
        if (itemDisplayInfo.HasValue)
            await SetVirtualItemDisplayId(slot, itemDisplayInfo.Value, cancellationToken);
    }

    public async ValueTask SetVirtualItemDisplayId(int slot, uint itemDisplayInfo, CancellationToken cancellationToken)
    {
        var weaponModel = await gameContext.MdxManager.LoadItemMesh(itemDisplayInfo, false, 0, 0);

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

        gameContext.StatusIconsManager.Unregister(this);

        // mount, items, nametag and chat bubble are all parented to objectEntity, so destroying it cascades
        entityManager.DestroyEntity(objectEntity);
        objectEntity = Entity.Empty;

        DisposeOwnedMaterials();
        if (mountData != null)
        {
            DisposeMaterials(mountData.materials);
            mountData = null;
        }
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

