using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Resources;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheEngine.Structures;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MpqReader.Structures;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Renders the currently-picked creature/gameobject model into a small off-screen texture (shown in
/// the spawn picker) using the engine's <see cref="IRenderManager.DrawRenderersToTexture"/> — a
/// BeforeOpaque pass with a dedicated preview camera. The model lives far off the play area so it is
/// culled from the main view but still animates (bones upload), then is framed by the preview camera.
/// </summary>
public sealed class SpawnPreviewRenderStage : IRenderStage
{
    private const int Resolution = 256;

    private readonly IGameContext gameContext;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly IEntityManager entityManager;
    private readonly RenderLayer renderLayer;

    private ITexture? color0, color1, depth, renderTexture;
    private bool rendered;

    private WorldObjectInstance? model;
    private bool modelReady;
    private (bool isCreature, uint entry)? current;
    private int generation;
    private float previewStartTime; // engine time when the current template was selected (turntable reset)

    private readonly List<(LocalToWorld, MeshRenderer)> rendererList = new();

    public string Name => "SpawnPreview";
    public RenderPoint RenderPoints => RenderPoint.BeforeOpaque;

    /// <summary>ImGui texture id (the render texture's native handle) for ImGui.Image.</summary>
    public nint PreviewTextureId => color0 != null ? color0.Handle.ToRawIntPtr() : 0;
    public bool HasModel => model != null && modelReady;
    public bool HasPreview => rendered && color0 != null;

    public SpawnPreviewRenderStage(IGameContext gameContext,
        ICachedDatabaseProvider databaseProvider,
        IEntityManager entityManager,
        RenderLayer renderLayer)
    {
        this.gameContext = gameContext;
        this.databaseProvider = databaseProvider;
        this.entityManager = entityManager;
        this.renderLayer = renderLayer;
    }

    public void SetTemplate(bool isCreature, uint entry)
    {
        if (current is { } c && c.isCreature == isCreature && c.entry == entry)
            return;
        current = (isCreature, entry);
        modelReady = false;
        rendered = false;
        previewStartTime = (float)gameContext.Engine.TotalTime; // reset the turntable
        model?.Dispose();
        model = null;
        LoadModel(++generation, isCreature, entry).ListenErrors();
    }

    public void Clear()
    {
        generation++;
        current = null;
        modelReady = false;
        model?.Dispose();
        model = null;
    }

    private async Task LoadModel(int gen, bool isCreature, uint entry)
    {
        WorldObjectInstance? instance = null;
        if (isCreature)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(entry) ?? await databaseProvider.GetCreatureTemplate(entry);
            if (template != null)
            {
                var creature = new CreatureInstance(gameContext, template, null, renderLayer);
                await creature.Load();
                creature.Animation = M2AnimationType.Stand; // idle loop for the preview
                instance = creature;
            }
        }
        else
        {
            var template = databaseProvider.GetCachedGameObjectTemplate(entry) ?? await databaseProvider.GetGameObjectTemplate(entry);
            if (template != null)
            {
                var go = new GameObjectInstance(gameContext, template, null, renderLayer);
                await go.Load();
                instance = go;
            }
        }

        if (gen != generation || instance == null)
        {
            instance?.Dispose();
            return;
        }

        // NOTE: keep rendering ENABLED so the engine computes this entity's LocalToWorld (scale/rotation)
        // and WorldMeshBounds (only done during the render update). It therefore also shows at the world
        // origin for now - once the preview is confirmed we'll hide it via a preview-only render layer.
        instance.Position = Vector3.Zero;
        model = instance;
        modelReady = true;
    }

    /// <summary>Advances the preview model's animation. The regular AnimationSystem.Update skips
    /// entities far (&gt;100u) from the main camera, and our preview model sits at the origin, so we
    /// drive it ourselves; the engine's UploadToGpu then carries the computed bones to the GPU. Call
    /// each frame from the game update (before UploadToGpu).</summary>
    public void TickAnimation(float delta)
    {
        if (!modelReady || model is not CreatureInstance)
            return;
        var e = model.WorldObjectEntity;
        if (!entityManager.Exist(e))
            return;
        var animData = entityManager.GetManagedComponent<M2AnimationComponentData>(e);
        if (animData == null)
            return;
        var l2w = entityManager.GetComponent<LocalToWorld>(e);
        var viewMatrix = gameContext.Engine.CameraManager.MainCamera.ViewMatrix;
        AnimationSystem.ManualAnimationStep(delta, animData, in l2w, in viewMatrix);
    }

    public void PrepareFrame(ICamera camera) { }

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        if (point != RenderPoint.BeforeOpaque || !modelReady || model == null)
            return;

        GatherRenderers();
        if (rendererList.Count == 0)
            return;

        EnsureTargets();
        ComputeCamera(out var view, out var proj, out var camPos);

        var rm = gameContext.Engine.RenderManager;
        commandList.BeginRenderTexture(renderTexture!, new Color4(0, 0, 0, 0), LoadOp.Clear); // transparent background
        rm.SetSceneCameraOverride(view, proj, camPos);
        rm.DrawRenderers(commandList, CollectionsMarshal.AsSpan(rendererList));
        commandList.EndRenderingPass();
        commandList.BarrierToShaderRead(renderTexture!);
        rendered = true;
    }

    private void EnsureTargets()
    {
        if (renderTexture != null)
            return;
        var tm = gameContext.Engine.TextureManager;
        color0 = tm.CreateTexture((uint[]?)null, Resolution, Resolution, TextureFormat.R8G8B8A8);
        color1 = tm.CreateTexture((uint[]?)null, Resolution, Resolution, TextureFormat.R32ui);
        depth = tm.CreateTexture((uint[]?)null, Resolution, Resolution, TextureFormat.DepthComponent);
        renderTexture = tm.CreateRenderTexture(color0, depth, color1);
    }

    private void GatherRenderers()
    {
        rendererList.Clear();
        var e = model!.WorldObjectEntity;
        if (!entityManager.Exist(e))
            return;
        var l2w = entityManager.GetComponent<LocalToWorld>(e);
        // use the renderer's real InstanceData (its bone base) so the model shows its live animated
        // pose - the AnimationSystem already computes + uploads bones for this (enabled) entity.
        foreach (var mr in entityManager.GetArrayComponents<MeshRenderer>(e))
            rendererList.Add((l2w, mr));
    }

    private void ComputeCamera(out Matrix view, out Matrix proj, out Vector3 cameraPosition)
    {
        var e = model!.WorldObjectEntity;
        Vector3 center = Vector3.Zero;
        float radius = 2f;
        if (entityManager.HasComponent<WorldMeshBounds>(e))
        {
            var box = entityManager.GetComponent<WorldMeshBounds>(e).box;
            center = box.Center;
            radius = MathF.Max(box.Size.Length() * 0.5f, 0.5f);
        }

        const float fov = 45f * (MathF.PI / 180f);
        float dist = radius / MathF.Tan(fov * 0.5f) * 1.4f;
        // turntable: orbit the camera around Z, one full turn per 10s (reset when the template changes).
        // Engine.TotalTime is in MILLISECONDS, so 10s = 10000ms.
        float azimuth = (float)(gameContext.Engine.TotalTime - previewStartTime) / 10000f * (2f * MathF.PI);
        var dir = Vectors.Normalize(new Vector3(MathF.Cos(azimuth), MathF.Sin(azimuth), 0.5f));
        cameraPosition = center + dir * dist;

        // this engine's camera looks along its local Down (-Z) axis transformed by its rotation
        // (see CameraManager: forward = Vectors.Down.Multiply(Rotation)), so build a rotation that maps
        // Down onto the look direction - NOT LookRotation, which aligns +X.
        var transform = new Transform { Position = cameraPosition, Rotation = LookAt(center - cameraPosition) };
        view = transform.WorldToLocalMatrix;
        proj = Matrix.CreatePerspectiveFieldOfView(fov, 1f, 0.05f, dist * 6f + radius * 6f);
    }

    // Camera orientation for this engine: local -Z (Vectors.Down) looks along the direction, local +Y
    // is screen-up (kept ~world +Z), local +X is screen-right. Built as an orthonormal basis so the
    // camera stays upright (a shortest-arc rotation leaves the roll undefined -> upside-down image).
    private static Quaternion LookAt(Vector3 lookDir)
    {
        var forward = Vectors.Normalize(lookDir);
        var worldUp = Vectors.Up;
        var right = Vector3.Cross(forward, worldUp);
        if (right.LengthSquared() < 1e-6f)
            right = Vector3.Cross(forward, Vectors.Forward);
        right = Vectors.Normalize(right);
        var up = Vector3.Cross(right, forward);
        var basis = new Matrix(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            -forward.X, -forward.Y, -forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(basis);
    }

    public void EndFrame() { }

    public void Dispose()
    {
        Clear();
        var tm = gameContext.Engine.TextureManager;
        if (renderTexture != null) tm.DisposeTexture(renderTexture);
        if (color0 != null) tm.DisposeTexture(color0);
        if (color1 != null) tm.DisposeTexture(color1);
        if (depth != null) tm.DisposeTexture(depth);
        renderTexture = color0 = color1 = depth = null;
    }
}
