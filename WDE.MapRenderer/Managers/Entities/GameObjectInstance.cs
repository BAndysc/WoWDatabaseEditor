using System.Collections;
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
            var animSystem = gameContext.AnimationSystem;
            var m2Model = m2Instance.model;
            var (boneBase, colorBase, texBase) = animSystem.AllocateAnimationSlots(m2Model);

            var masterAnimation = new M2AnimationComponentData(m2Model)
            {
                SetNewAnimation = 0,
                BoneBase = boneBase,
                ColorBase = colorBase,
                TexTransformBase = texBase,
                _boneCache = AnimationSystem.IdentityMatrix(m2Model.bones.Length).ToArray(),
                _colorCache = AnimationSystem.IdentityColors(m2Model.colors.Length).ToArray(),
                _texTransformCache = AnimationSystem.IdentityMatrix(m2Model.texture_transforms.Length + 1).ToArray(),
            };
            entityManager.SetManagedComponent(objectEntity, masterAnimation);
            entityManager.SetManagedComponent(objectEntity, new MdxRenderer(m2Instance) { Owner = objectEntity });

            foreach (var material in m2Instance.materials)
            {
                var ownMaterial = OwnMaterial(material.material); // per-instance copy (dither etc. must not leak onto shared model materials)
                BaseMaterial ??= ownMaterial;
                objectEntity.SetRenderer(entityManager, m2Instance.mesh, material.submesh, ownMaterial,
                    new Int4(
                        material.batch.colorIndex < 0 ? -1 : colorBase + material.batch.colorIndex,
                        material.batch.textureTransformIndex < 0 ? -1 : texBase + material.batch.textureTransformIndex,
                        material.batch.textureTransformIndex2 < 0 ? -1 : texBase + material.batch.textureTransformIndex2,
                        boneBase));
            }
        }
        else if (wmoInstance != null)
        {
            wmoInstances.Add(wmoInstance);
            foreach (var batch in wmoInstance.meshes)
            {
                for (var index = 0; index < batch.Item2.Length; index++)
                {
                    var ownMaterial = OwnMaterial(batch.Item2[index]);
                    BaseMaterial ??= ownMaterial;

                    objectEntity.SetRenderer(entityManager, batch.Item1, index, ownMaterial);
                }
            }
        }

        textEntity = gameContext.UiManager.DrawPersistentWorldText("calibri", new Vector2(0.5f, 0.5f), gameObjectTemplate?.Name ?? "", 0.25f, Matrix.Identity, 50);
        entityManager.SetParent(textEntity, objectEntity);
        entityManager.AddComponent(textEntity, new CopyParentTransform(){Parent = objectEntity});
        entityManager.AddComponent(textEntity, new DirtyPosition(true));
        textEntity.SetRenderLayer(entityManager, renderLayer);
    }

    public override void Dispose()
    {
        if (objectEntity == Entity.Empty)
        {
            throw new Exception("Double dispose!");
        }

        var entityManager = gameContext.EntityManager;

        // textEntity (and any other attachment) is parented to objectEntity, so destroying it cascades
        entityManager.DestroyEntity(objectEntity);
        objectEntity = Entity.Empty;

        DisposeOwnedMaterials();
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