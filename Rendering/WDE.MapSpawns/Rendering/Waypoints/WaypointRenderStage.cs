using System.Runtime.InteropServices;
using TheEngine.Resources;
using TheEngine;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheMaths;
using WDE.Common.Database;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Waypoints;

namespace WDE.MapSpawns.Rendering.Waypoints;

/// <summary>
/// Top-down ortho projector that paints the editable path around the camera. Shared by the render
/// stage (which renders the path into the projector RT with these matrices) and the editor module
/// (which drives a decal whose box is the inverse of this transform, so the decal samples the RT at
/// exactly the texels the projector wrote). Camera-fitted: covers a fixed world span around the
/// camera so resolution stays high up close (where the projected path is wanted) regardless of how
/// many points there are.
/// </summary>
internal static class WaypointProjector
{
    /// <summary>Padding (world units) added around the path bounds so the ribbon edges aren't clipped.</summary>
    public const float Padding = 6f;
    /// <summary>Smallest XY half-span - keeps a tiny/1-point path from collapsing the projection.</summary>
    public const float MinRadius = 8f;
    /// <summary>Largest XY half-span - a path bigger than this can't be covered sharply by one texture,
    /// so we stop growing (it just gets lower-res, which only matters when zoomed way out anyway).</summary>
    public const float MaxRadius = 300f;
    /// <summary>Extra vertical band above/below the path's Z range, so the decal still lands on the
    /// terrain beneath floating waypoints without painting onto far-above/below geometry.</summary>
    public const float HeightMargin = 60f;
    /// <summary>Target on-screen half-width of the projected path, in PIXELS - the projector shader
    /// sizes the world ribbon per-vertex so it lands at this screen thickness against the main camera,
    /// matching the overlay line (RIBBON_HALF_PX = 4 in waypoint_path.vert) at any zoom.</summary>
    public const float ScreenHalfWidthPx = 4f;

    // Engine render targets are stored top-down (row 0 = top, via a negative-height viewport), while
    // the decal samples uv with uv.y increasing in +world-Y. So the projector flips Y to keep the two
    // in register. If the projected path comes out vertically mirrored, flip this to +1.
    private const float YSign = -1f;

    /// <summary>"Best fit" box: the XY/Z bounds of the editable path's points (clamped), so the
    /// fixed-size projector texture concentrates its resolution on the actual path instead of a fixed
    /// area around the camera. Depends only on the points (not the camera), so the render stage and
    /// the decal compute the SAME box regardless of when in the frame they call this. Returns false
    /// if the path is null/empty.</summary>
    public static bool ComputeBox(EditablePath? path, out Vector3 center, out float radius, out float verticalHalf)
    {
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        bool any = false;

        if (path != null)
            foreach (var wp in path.Points)
            {
                any = true;
                minX = Math.Min(minX, wp.X); maxX = Math.Max(maxX, wp.X);
                minY = Math.Min(minY, wp.Y); maxY = Math.Max(maxY, wp.Y);
                minZ = Math.Min(minZ, wp.Z); maxZ = Math.Max(maxZ, wp.Z);
            }

        if (!any)
        {
            center = default;
            radius = MinRadius;
            verticalHalf = HeightMargin;
            return false;
        }

        center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
        float halfXY = Math.Max((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
        radius = Math.Clamp(halfXY + Padding, MinRadius, MaxRadius);
        verticalHalf = (maxZ - minZ) * 0.5f + HeightMargin;
        return true;
    }

    /// <summary>Top-down world->clip matrix used to render the projector RT. Built by hand (an
    /// axis-aligned off-center ortho) so its XY maps world->ndc identically to how the decal box maps
    /// world->local, guaranteeing the decal samples the exact texels the projector wrote. Row-vector
    /// layout (clip = world * M), matching how the engine's other view/proj matrices feed the shaders.</summary>
    public static Matrix ComputeViewProj(Vector3 center, float radius, float verticalHalf)
    {
        var m = new Matrix();
        m.M11 = 1f / radius;                    m.M41 = -center.X / radius;                              // clip.x = (x-cx)/R
        m.M22 = YSign / radius;                 m.M42 = -YSign * center.Y / radius;                      // clip.y = ±(y-cy)/R
        m.M33 = 1f / (2f * verticalHalf);       m.M43 = -(center.Z - verticalHalf) / (2f * verticalHalf); // clip.z = [0,1] over the band
        m.M44 = 1f;
        return m;
    }

    /// <summary>Decal box (local cube [-1,1] -> the same world volume the ortho covers). Its
    /// worldToLocal.xy equals the ortho's world->ndc.xy, so the decal samples the projector RT 1:1.</summary>
    public static Matrix ComputeDecalLocalToWorld(Vector3 center, float radius, float verticalHalf)
    {
        return Matrix.CreateScale(radius, radius, verticalHalf) * Matrix.CreateTranslation(center);
    }
}

/// <summary>
/// Draws the one editable waypoint path (plus read-only route previews). Two outputs:
///  - the on-screen overlay (RenderPoint.Transparent): GPU-expanded thick ribbons + arrowheads +
///    handle dots, depth-aware (full alpha in front of geometry, faint behind).
///  - the projector overlay (RenderPoint.BeforeOpaque): the same path rendered flat top-down into a
///    render texture THIS stage owns, which a decal then projects onto the terrain/objects as a
///    "ground path". The stage allocates that RT and manages its pass + barrier itself.
/// Positions are uploaded to transient buffers and pulled by gl_VertexIndex, so nothing is
/// regenerated on the CPU when a point is edited - scales to thousands of points.
/// </summary>
public sealed class WaypointRenderStage : IRenderStage
{
    // three path states: just loaded/visible (dim yellow), selected = inspectable (cyan),
    // editing = pen mode armed, clicks add points (green - matches the rubber-band preview)
    private static readonly Vector4 PathColor = new(1.0f, 0.85f, 0.2f, 0.65f);
    private static readonly Vector4 SelectedPathColor = new(0.2f, 0.9f, 1.0f, 1.0f);
    private static readonly Vector4 EditingPathColor = new(0.35f, 1.0f, 0.45f, 1.0f);
    private static readonly Vector4 HandleColor = new(0.95f, 0.95f, 0.95f, 0.75f);
    private static readonly Vector4 SelectedPathHandleColor = new(0.95f, 0.95f, 0.95f, 1.0f);
    private static readonly Vector4 SelectedHandleColor = new(1.0f, 0.45f, 0.1f, 1.0f);
    private const float HandleSizePx = 7.0f;
    private const float SelectedHandleSizePx = 10.0f;

    // alpha multiplier (.x) applied to the portion OCCLUDED by scene geometry (the visible portion
    // draws at full alpha - see the two-pass overlay below), and arrow-head toggle (.y).
    private const float OccludedAlpha = 0.3f;
    private static readonly Vector4[] VisibleParam = { new(1.0f, 1.0f, 0, 0) };
    private static readonly Vector4[] OccludedParam = { new(OccludedAlpha, 1.0f, 0, 0) };

    [StructLayout(LayoutKind.Sequential)]
    private struct ProjectorParamsData
    {
        public Matrix ViewProj;   // std430 mat4
        public Vector4 Misc;      // .x = world half-width
    }

    // the projector RT is fixed-size (independent of the window), two color attachments to match the
    // standard layout the default pipelines are built for (color0 R8G8B8A8 + color1 R32ui) - a single
    // attachment makes the (pipeline, target) render pass invalid on MoltenVK.
    private const int ProjectorResolution = 2048;

    private readonly Engine engine;
    private readonly IWaypointEditorService service;
    private readonly ISpawnEditorToolService toolService;
    private readonly IMeshManager meshManager;

    // overlay: each shader drawn twice - a "visible" pipeline (depth LessEqual) and an "occluded"
    // pipeline (depth Greater) so the part behind terrain fades. Both leave depth writes off.
    private readonly Material pathVisibleMaterial;
    private readonly Material pathOccludedMaterial;
    private readonly Material handleVisibleMaterial;
    private readonly Material handleOccludedMaterial;
    // projector: flat top-down render into the projector RT, no depth, opaque write.
    private readonly Material projectorMaterial;
    private readonly IMesh dummyMesh;

    // owned by this stage (RenderPoint.BeforeOpaque): the path painted top-down, sampled by the decal.
    private ITexture? projectorRenderTexture;
    /// <summary>The top-down projector texture (path painted flat); null until the first BeforeOpaque
    /// pass has run. The editor module feeds this to a decal that projects it onto terrain/objects.</summary>
    public ITexture? ProjectorTexture => projectorRenderTexture;

    private Vector4[] segmentTexels = new Vector4[3 * 64];
    private int segmentCount;
    // segments [0..editableSegmentCount) belong to the one editable path; the rest are read-only
    // previews. Only the editable ones feed the ground projector.
    private int editableSegmentCount;
    private Vector4[] handleTexels = new Vector4[2 * 64];
    private int handleCount;

    public string Name => "Waypoints";
    public RenderPoint RenderPoints => RenderPoint.Transparent | RenderPoint.BeforeOpaque;

    public WaypointRenderStage(Engine engine, IWaypointEditorService service, ISpawnEditorToolService toolService)
    {
        this.engine = engine;
        this.service = service;
        this.toolService = toolService;
        this.meshManager = engine.MeshManager;

        pathVisibleMaterial = CreateMaterial(engine, "data/waypoint_path.json", Veldrid.ComparisonKind.LessEqual);
        pathOccludedMaterial = CreateMaterial(engine, "data/waypoint_path.json", Veldrid.ComparisonKind.Greater);
        handleVisibleMaterial = CreateMaterial(engine, "data/waypoint_handle.json", Veldrid.ComparisonKind.LessEqual);
        handleOccludedMaterial = CreateMaterial(engine, "data/waypoint_handle.json", Veldrid.ComparisonKind.Greater);
        projectorMaterial = CreateProjectorMaterial(engine, "data/waypoint_projector.json");
        dummyMesh = engine.MeshManager.CreateMesh(new Vector3[2], Array.Empty<ushort>());
    }

    // depthCompare LessEqual -> passes where in front of (or on) scene depth; Greater -> behind.
    // Depth writes stay off so the overlay never pollutes the depth buffer.
    private static Material CreateMaterial(Engine engine, string shaderPath, Veldrid.ComparisonKind depthCompare)
    {
        var shader = engine.ShaderManager.LoadShader(shaderPath);
        var pipeline = engine.PipelineManager.CreatePipeline(shader, Veldrid.PrimitiveTopology.TriangleList,
            new Veldrid.GraphicsPipelineDescription
            {
                BlendState = Veldrid.BlendStateDescription.SingleAlphaBlend,
                RasterizerState = Veldrid.RasterizerStateDescription.CullNone,
                DepthStencilState = new Veldrid.DepthStencilStateDescription(true, false, depthCompare)
            }, false);
        return engine.MaterialManager.CreateMaterial(pipeline);
    }

    // projector RT is a flat 2D overlay: no depth test/write, opaque write (the RT is cleared
    // transparent, the ribbon writes solid color + alpha 1 to mark coverage).
    private static Material CreateProjectorMaterial(Engine engine, string shaderPath)
    {
        var shader = engine.ShaderManager.LoadShader(shaderPath);
        var pipeline = engine.PipelineManager.CreatePipeline(shader, Veldrid.PrimitiveTopology.TriangleList,
            new Veldrid.GraphicsPipelineDescription
            {
                BlendState = Veldrid.BlendStateDescription.SingleDisabled,
                RasterizerState = Veldrid.RasterizerStateDescription.CullNone,
                DepthStencilState = new Veldrid.DepthStencilStateDescription(false, false, Veldrid.ComparisonKind.Always)
            }, false);
        return engine.MaterialManager.CreateMaterial(pipeline);
    }

    public void PrepareFrame(ICamera camera)
    {
        segmentCount = 0;
        handleCount = 0;

        // The one editable path's ribbon/handles (+ the ground projector it feeds) only exist while
        // the Waypoint tool is active - switching to another tool stops them (the module likewise
        // disables the projector decal, see UpdateProjectorDecal). The read-only previews below draw
        // in ANY tool, so a creature's route is visible from the Select tool too.
        if (toolService.ActiveTool == SpawnEditorTool.Waypoint && service.SelectedPath is { } path && path.Points.Count > 0)
        {
            var points = path.Points;
            bool editing = ReferenceEquals(path, service.EditingPath);
            var lineColor = editing ? EditingPathColor : SelectedPathColor;

            for (int i = 0; i < points.Count - 1; ++i)
                PushSegment(Pos(points[i]), Pos(points[i + 1]), lineColor);

            for (int i = 0; i < points.Count; ++i)
            {
                bool selectedPoint = i == service.SelectedPointIndex;
                AddHandle(Pos(points[i]),
                    selectedPoint ? SelectedHandleSizePx : HandleSizePx,
                    selectedPoint ? SelectedHandleColor : SelectedPathHandleColor);
            }
        }

        editableSegmentCount = segmentCount;

        // read-only overlays (selected creature's route, sniffed movement previews): ribbons + small
        // dots, never pickable, drawn in every tool
        foreach (var preview in service.PreviewPaths)
        {
            var pts = preview.Points;
            for (int i = 0; i < pts.Count - 1; ++i)
                PushSegment(pts[i], pts[i + 1], preview.Color);
            for (int i = 0; i < pts.Count; ++i)
                AddHandle(pts[i], HandleSizePx * 0.6f, preview.Color);
        }
    }

    private static Vector3 Pos(UniversalWaypoint w) => new(w.X, w.Y, w.Z);

    private void PushSegment(Vector3 a, Vector3 b, Vector4 color)
    {
        int need = (segmentCount + 1) * 3;
        if (need > segmentTexels.Length)
            Array.Resize(ref segmentTexels, Math.Max(need, segmentTexels.Length * 2));
        int o = segmentCount * 3;
        segmentTexels[o + 0] = new Vector4(a, 0);
        segmentTexels[o + 1] = new Vector4(b, 0);
        segmentTexels[o + 2] = color;
        segmentCount++;
    }

    private void AddHandle(Vector3 pos, float sizePx, Vector4 color)
    {
        int need = (handleCount + 1) * 2;
        if (need > handleTexels.Length)
            Array.Resize(ref handleTexels, Math.Max(need, handleTexels.Length * 2));
        int o = handleCount * 2;
        handleTexels[o + 0] = new Vector4(pos, sizePx);
        handleTexels[o + 1] = color;
        handleCount++;
    }

    // The transient buffers below are bound as plain std430 SSBOs (readonly buffer) by the waypoint shaders.
    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        if (point == RenderPoint.BeforeOpaque)
        {
            RenderProjector(commandList, camera);
            return;
        }

        // on-screen overlay
        if (segmentCount > 0)
        {
            var buffer = commandList.UploadTransientBuffer(
                (ReadOnlySpan<Vector4>)segmentTexels.AsSpan(0, segmentCount * 3));
            DrawTwoPass(commandList, "WaypointBuffer", "WaypointParams", buffer, segmentCount * 9,
                pathOccludedMaterial, pathVisibleMaterial);
        }

        if (handleCount > 0)
        {
            var buffer = commandList.UploadTransientBuffer(
                (ReadOnlySpan<Vector4>)handleTexels.AsSpan(0, handleCount * 2));
            DrawTwoPass(commandList, "HandleBuffer", "HandleParams", buffer, handleCount * 6,
                handleOccludedMaterial, handleVisibleMaterial);
        }
    }

    // Renders the path flat top-down into the projector RT with the shared ortho matrix and a
    // constant world-space ribbon width (6 verts/segment, no arrowheads).
    // RenderPoint.BeforeOpaque: this stage owns the whole off-screen lifecycle - allocate the RT,
    // begin a pass into it, draw the path flat top-down, end the pass, barrier it to shader-read so
    // the opaque pass (the decal) can sample it. The engine doesn't manage the target here.
    private void RenderProjector(EngineCommandList commandList, ICamera camera)
    {
        // only the editable path projects onto the ground; read-only previews don't
        if (editableSegmentCount == 0)
            return;

        projectorRenderTexture ??= engine.TextureManager.CreateRenderTexture(ProjectorResolution, ProjectorResolution, 2);

        var segments = commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)segmentTexels.AsSpan(0, editableSegmentCount * 3));

        WaypointProjector.ComputeBox(service.SelectedPath, out var boxCenter, out var boxRadius, out var boxVHalf);
        Span<ProjectorParamsData> param = stackalloc ProjectorParamsData[1];
        param[0] = new ProjectorParamsData
        {
            ViewProj = WaypointProjector.ComputeViewProj(boxCenter, boxRadius, boxVHalf),
            Misc = new Vector4(WaypointProjector.ScreenHalfWidthPx, 0, 0, 0),
        };
        var paramBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<ProjectorParamsData>)param);

        // clear to fully transparent: texels the path doesn't cover read as "no path"
        commandList.BeginRenderTexture(projectorRenderTexture, new Color4(0, 0, 0, 0), LoadOp.Clear);
        commandList.SetBuffer("WaypointBuffer", segments);
        commandList.SetBuffer("ProjectorParams", paramBuffer);
        commandList.SetMaterial(projectorMaterial, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, editableSegmentCount * 6);
        commandList.EndRenderingPass();
        commandList.BarrierToShaderRead(projectorRenderTexture);
    }

    // Draws the occluded portion (behind scene geometry, faint) then the visible portion (full
    // alpha) of the same geometry - disjoint depth ranges, so every fragment is drawn once.
    private void DrawTwoPass(EngineCommandList commandList, string geometryName, string paramName,
        INativeBuffer geometry, int vertexCount, Material occluded, Material visible)
    {
        commandList.SetBuffer(geometryName, geometry);
        commandList.SetBuffer(paramName, commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)OccludedParam));
        commandList.SetMaterial(occluded, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, vertexCount);

        commandList.SetBuffer(geometryName, geometry);
        commandList.SetBuffer(paramName, commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)VisibleParam));
        commandList.SetMaterial(visible, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, vertexCount);
    }

    public void EndFrame()
    {
    }

    public void Dispose()
    {
        meshManager.DisposeMesh(dummyMesh);
        if (projectorRenderTexture != null)
        {
            engine.TextureManager.DisposeTexture(projectorRenderTexture);
            projectorRenderTexture = null;
        }
    }
}
