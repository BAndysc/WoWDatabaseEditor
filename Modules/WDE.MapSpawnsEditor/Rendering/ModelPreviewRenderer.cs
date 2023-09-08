using System.Collections;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Services;
using WDE.MapRenderer.Managers;
using WDE.MpqReader.Structures;

namespace WDE.MapSpawnsEditor.Rendering;

public class ModelPreviewRenderer
{
    private readonly IRenderManager renderManager;
    private readonly ITextureManager textureManager;
    private readonly IGameContext gameContext;
    private readonly ICameraManager cameraManager;
    private readonly RaycastSystem raycastSystem;
    private readonly MdxManager mdxManager;
    private static int Width => 512;
    private static int Height => 512;
    public IntPtr TextureHandle => rt.ToRawIntPtr();

    private TextureHandle rt;
    private SceneData sceneData;

    private MaterialInstanceRenderData? materialInstanceData;
    private M2AnimationComponentData? animationComponentData;
    private MdxManager.MdxInstance? currentModelInstance;
    private WmoManager.WmoInstance? currentWmoInstance;
    private (GuidType, uint) currentModel;
    private Camera previewCamera;
    private NativeBuffer<Matrix4x4> bonesMatrix;

    public ModelPreviewRenderer(IRenderManager renderManager,
        ITextureManager textureManager,
        IGameContext gameContext,
        ICameraManager cameraManager,
        
        RaycastSystem raycastSystem,
        MdxManager mdxManager)
    {
        this.renderManager = renderManager;
        this.textureManager = textureManager;
        this.gameContext = gameContext;
        this.cameraManager = cameraManager;
        this.raycastSystem = raycastSystem;
        this.mdxManager = mdxManager;
        rt = textureManager.CreateRenderTexture(Width, Height);
        previewCamera = new Camera();
        previewCamera.Aspect = 1;
        previewCamera.Transform.Position = Vector3.Zero;
        previewCamera.Transform.Rotation = Utilities.FromEuler(180, 0, 90 + 15);

        var mainLight = new DirectionalLight
        {
            LightRotation = Utilities.FromEuler(0, 90, 0),
            LightIntensity = 1,
            LightColor = new Vector4(1, 1, 1, 1),
            AmbientColor = new Vector4(0.6f, 0.6f, 0.6f, 1),
            LightPosition = Vector3.Zero
        };

        var secondaryLight = new DirectionalLight
        {
            LightIntensity = 0,
            AmbientColor = Vector4.Zero
        };

        sceneData = new SceneData(previewCamera, new FogSettings(){Enabled = false}, mainLight, secondaryLight);
        bonesMatrix = gameContext.Engine.CreateBuffer<Matrix>(BufferTypeEnum.StructuredBufferVertexOnly, 1, BufferInternalFormat.Float4);
    }

    public void Dispose()
    {
        bonesMatrix.Dispose();
        textureManager.DisposeTexture(rt);
    }

    public void SetModel(uint displayId, GuidType type)
    {
        if (currentModel == (type, displayId))
            return;
        currentModel = (type, displayId);
        currentModelInstance = null;
        gameContext.StartCoroutine(LoadModel(displayId, type));
    }

    private IEnumerator LoadModel(uint displayId, GuidType type)
    {
        if (type == GuidType.Creature)
        {
            TaskCompletionSource<MdxManager.MdxInstance?> mdx = new();
            yield return mdxManager.LoadCreatureModel(displayId, mdx);
            
            if (currentModel == (type, displayId)) // it might have changed in the meantime
            {
                rotation = (float)Math.PI / 2;
                currentModelInstance = mdx.Task.Result;
                currentWmoInstance = null;
                materialInstanceData = null;
                animationComponentData = currentModelInstance == null ? null : new M2AnimationComponentData(currentModelInstance.model)
                {
                    SetNewAnimation = (int)M2AnimationType.Stand,
                    _buffer = bonesMatrix
                };
            }
        }
        else
        {
            TaskCompletionSource<(MdxManager.MdxInstance?, WmoManager.WmoInstance?)?> mdx = new();
            yield return mdxManager.LoadGameObjectModel(displayId, mdx);
            if (currentModel == (type, displayId)) // it might have changed in the meantime
            {
                rotation = (float)Math.PI / 2;
                currentModelInstance = mdx.Task.Result?.Item1;
                currentWmoInstance = mdx.Task.Result?.Item2;
                materialInstanceData = null;
                animationComponentData = null;
            }
        }
    }

    private float rotation = 0;
    
    public void Render(float delta)
    {
        renderManager.ActivateScene(sceneData);
        renderManager.ActivateRenderTexture(rt, Color4.TransparentBlack);
        rotation += delta / 1000 * 0.2f;
        if (currentModelInstance != null)
        {
            if (animationComponentData != null)
                AnimationSystem.ManualAnimationStep(delta, animationComponentData);
            var localToWorld = Utilities.TRS(
                Vectors.Down.Multiply(previewCamera.Transform.Rotation) *
                Math.Max(Math.Max(currentModelInstance.mesh.Bounds.Width, currentModelInstance.mesh.Bounds.Height), currentModelInstance.mesh.Bounds.Depth) * 1.2f
                - new Vector3(0, 0, currentModelInstance.mesh.Bounds.Size.Z / 2),
                Quaternion.CreateFromAxisAngle(Vectors.Down, rotation), Vector3.One);
            
            if (materialInstanceData == null)
            {
                materialInstanceData = new MaterialInstanceRenderData();
                materialInstanceData.SetBuffer(currentModelInstance.materials[0].material, "boneMatrices", bonesMatrix);
            }
    
            foreach (var batch in currentModelInstance.materials)
            {
                renderManager.Render(currentModelInstance.mesh, batch.material, batch.submesh, localToWorld, null, materialInstanceData);
            }
        }
        else if (currentWmoInstance != null && currentWmoInstance.meshes.Count > 0)
        {
            BoundingBox bounds = currentWmoInstance.meshes[0].Item1.Bounds;
            for (int i = 1; i < currentWmoInstance.meshes.Count; ++i)
                bounds = BoundingBox.Merge(bounds, currentWmoInstance.meshes[i].Item1.Bounds);
            
            var localToWorld = Utilities.TRS(
                Vectors.Down.Multiply(previewCamera.Transform.Rotation) *
                Math.Max(Math.Max(bounds.Width, bounds.Height), bounds.Depth) * 1.2f
                - new Vector3(0, 0, bounds.Size.Z / 2),
                Quaternion.CreateFromAxisAngle(Vectors.Down, rotation), Vector3.One);
            foreach (var batch in currentWmoInstance.meshes)
            {
                for (var index = 0; index < batch.Item2.Length; index++)
                {
                    var material = batch.Item2[index];
                    renderManager.Render(batch.Item1, material, index, localToWorld);
                }
            }
        }
        renderManager.ActivateDefaultRenderTexture();
        renderManager.ActivateScene(null);
    }
}