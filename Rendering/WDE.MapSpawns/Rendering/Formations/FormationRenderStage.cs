using TheEngine.Resources;
using TheEngine;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheMaths;
using WDE.MapSpawns.Models.Formations;

namespace WDE.MapSpawns.Rendering.Formations;

/// <summary>
/// Draws every loaded formation link as a leader→member arrow, reusing the waypoint path shader
/// (<c>data/waypoint_path.json</c>) which GPU-expands an (A, B, color) segment buffer into a
/// screen-thick ribbon with an arrowhead, depth-aware (full alpha in front of geometry, faint
/// behind). When the tool is off the arrows are dimmed and non-interactive; when on the selected
/// link is highlighted and an in-progress drag is shown as a rubber-band arrow. Endpoints are read
/// live from the spawns each frame, so moving a creature moves its arrows. No projector/decal: the
/// arrows float between the NPCs rather than being painted on the ground.
/// </summary>
public sealed class FormationRenderStage : IRenderStage
{
    private static readonly Vector4 ArrowColor = new(0.35f, 1.0f, 0.55f, 1.0f);
    private static readonly Vector4 SelectedColor = new(1.0f, 0.6f, 0.15f, 1.0f);
    private static readonly Vector4 DragColor = new(0.95f, 0.95f, 0.95f, 1.0f);
    private static readonly Vector4 SnappedDragColor = new(0.4f, 1.0f, 0.7f, 1.0f);
    // alpha the arrows are multiplied by when the tool is off (faint, read-only)
    private const float InactiveAlpha = 0.28f;
    // links with both endpoints farther than this from the camera are not drawn - at that range
    // the screen-thick ribbons collapse into flickering pixels and only add clutter/segment count
    private const float MaxDrawDistance = 400.0f;
    private const float MaxDrawDistanceSq = MaxDrawDistance * MaxDrawDistance;

    private const float OccludedAlpha = 0.3f;
    private static readonly Vector4[] VisibleParam = { new(1.0f, 1.0f, 0, 0) };
    private static readonly Vector4[] OccludedParam = { new(OccludedAlpha, 1.0f, 0, 0) };

    private readonly Engine engine;
    private readonly IFormationEditorService service;
    private readonly IMeshManager meshManager;

    private readonly Material pathVisibleMaterial;
    private readonly Material pathOccludedMaterial;
    private readonly IMesh dummyMesh;

    private Vector4[] segmentTexels = new Vector4[3 * 64];
    private int segmentCount;

    public string Name => "Formations";
    public RenderPoint RenderPoints => RenderPoint.Transparent;

    public FormationRenderStage(Engine engine, IFormationEditorService service)
    {
        this.engine = engine;
        this.service = service;
        this.meshManager = engine.MeshManager;

        pathVisibleMaterial = CreateMaterial(engine, "data/waypoint_path.json", Veldrid.ComparisonKind.LessEqual);
        pathOccludedMaterial = CreateMaterial(engine, "data/waypoint_path.json", Veldrid.ComparisonKind.Greater);
        dummyMesh = engine.MeshManager.CreateMesh(new Vector3[2], Array.Empty<ushort>());
    }

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

    public void PrepareFrame(ICamera camera)
    {
        segmentCount = 0;

        bool toolOn = service.ToolEnabled;
        float alphaMul = toolOn ? 1.0f : InactiveAlpha;
        var cameraPos = camera.Transform.Position;

        // indexed (not foreach) so iterating the ObservableCollection doesn't box an enumerator each frame
        var formations = service.LoadedFormations;
        for (int i = 0; i < formations.Count; ++i)
        {
            var f = formations[i];
            if (f.IsLeaderSelfRow)
                continue;
            if (!service.TryGetEndpoints(f, out var leaderPos, out var memberPos))
                continue;

            bool selected = toolOn && ReferenceEquals(f, service.Selected);
            if (!selected &&
                Vector3.DistanceSquared(cameraPos, leaderPos) > MaxDrawDistanceSq &&
                Vector3.DistanceSquared(cameraPos, memberPos) > MaxDrawDistanceSq)
                continue;

            var color = selected ? SelectedColor : ArrowColor;
            color.W *= alphaMul;
            // arrow points member -> leader (the arrowhead, at the segment midpoint, points at the
            // leader the member follows) - same direction as the drag gesture that created it
            PushSegment(memberPos, leaderPos, color);
        }

        // rubber-band for the in-progress drag (member -> current target)
        if (toolOn && service.DragActive)
            PushSegment(service.DragFrom, service.DragTo, service.DragSnapped ? SnappedDragColor : DragColor);
    }

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

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        if (segmentCount == 0)
            return;

        // None (not Float4): the waypoint_path shader reads these as plain std430 SSBOs, so we don't
        // want the engine to also build an unused uniform-texel-buffer view each frame.
        var buffer = commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)segmentTexels.AsSpan(0, segmentCount * 3));
        DrawTwoPass(commandList, buffer, segmentCount * 9);
    }

    // occluded portion (behind geometry, faint) then the visible portion (full alpha) - disjoint
    // depth ranges, so every fragment is drawn once.
    private void DrawTwoPass(EngineCommandList commandList, INativeBuffer geometry, int vertexCount)
    {
        commandList.SetBuffer("WaypointBuffer", geometry);
        commandList.SetBuffer("WaypointParams", commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)OccludedParam));
        commandList.SetMaterial(pathOccludedMaterial, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, vertexCount);

        commandList.SetBuffer("WaypointBuffer", geometry);
        commandList.SetBuffer("WaypointParams", commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)VisibleParam));
        commandList.SetMaterial(pathVisibleMaterial, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, vertexCount);
    }

    public void EndFrame()
    {
    }

    public void Dispose()
    {
        meshManager.DisposeMesh(dummyMesh);
    }
}
