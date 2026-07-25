using System;
using System.Collections.Generic;
using TheEngine;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Rendering;
using TheEngine.Resources;
using TheMaths;
using WDE.MapSpawns.Models.CreatureLinking;

namespace WDE.MapSpawns.Rendering.CreatureLinking;

/// <summary>
/// Draws every loaded creature link as a slave→master arrow, reusing the waypoint path shader
/// (<c>data/waypoint_path.json</c>) which GPU-expands an (A, B, color) segment buffer into a
/// screen-thick, depth-aware arrow. Guid links (creature_linking) and entry links
/// (creature_linking_template) get distinct colors; the entry links fan out one arrow per loaded
/// slave spawn to its nearest master spawn. When the tool is off the arrows are faint and
/// non-interactive; when on the selected link is highlighted and an in-progress drag is a
/// rubber-band arrow. Endpoints are read live from the spawns each frame.
/// </summary>
public sealed class CreatureLinkRenderStage : IRenderStage
{
    internal static readonly Vector4 GuidArrowColor = new(1.0f, 0.55f, 0.20f, 1.0f);   // amber
    internal static readonly Vector4 EntryArrowColor = new(0.45f, 0.75f, 1.0f, 1.0f);  // sky blue
    internal static readonly Vector4 SelectedColor = new(1.0f, 0.85f, 0.20f, 1.0f);    // bright gold
    internal static readonly Vector4 DragColor = new(0.95f, 0.95f, 0.95f, 1.0f);
    internal static readonly Vector4 SnappedDragColor = new(0.4f, 1.0f, 0.7f, 1.0f);

    private const float InactiveAlpha = 0.28f;
    private const float MaxDrawDistance = 400.0f;
    private const float MaxDrawDistanceSq = MaxDrawDistance * MaxDrawDistance;
    // entry links can fan out into thousands of arrows on a crowded map - cap the segment count.
    private const int MaxSegments = 512;

    private const float OccludedAlpha = 0.3f;
    private static readonly Vector4[] VisibleParam = { new(1.0f, 1.0f, 0, 0) };
    private static readonly Vector4[] OccludedParam = { new(OccludedAlpha, 1.0f, 0, 0) };

    private readonly ICreatureLinkEditorService service;
    private readonly IMeshManager meshManager;

    private readonly Material pathVisibleMaterial;
    private readonly Material pathOccludedMaterial;
    private readonly IMesh dummyMesh;

    private Vector4[] segmentTexels = new Vector4[3 * 64];
    private int segmentCount;
    private readonly List<(Vector3 slave, Vector3 master)> arrowScratch = new();

    public string Name => "Creature linking";
    public RenderPoint RenderPoints => RenderPoint.Transparent;

    public CreatureLinkRenderStage(Engine engine, ICreatureLinkEditorService service)
    {
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

        var guidLinks = service.GuidLinks;
        for (int i = 0; i < guidLinks.Count && segmentCount < MaxSegments; ++i)
        {
            var link = guidLinks[i];
            if (!service.TryGetEndpoints(link, out var slavePos, out var masterPos))
                continue;

            bool selected = toolOn && ReferenceEquals(link, service.Selected);
            if (!selected && FarFromCamera(cameraPos, slavePos, masterPos))
                continue;

            var color = selected ? SelectedColor : GuidArrowColor;
            color.W *= alphaMul;
            // arrow points slave -> master (the arrowhead, at the midpoint, points at the master
            // whose events the slave reacts to) - same direction as the drag gesture that created it
            PushSegment(slavePos, masterPos, color);
        }

        var templateLinks = service.TemplateLinks;
        for (int i = 0; i < templateLinks.Count && segmentCount < MaxSegments; ++i)
        {
            var link = templateLinks[i];
            bool selected = toolOn && ReferenceEquals(link, service.Selected);
            service.CollectTemplateArrows(link, arrowScratch);
            for (int j = 0; j < arrowScratch.Count && segmentCount < MaxSegments; ++j)
            {
                var (slavePos, masterPos) = arrowScratch[j];
                if (!selected && FarFromCamera(cameraPos, slavePos, masterPos))
                    continue;
                var color = selected ? SelectedColor : EntryArrowColor;
                color.W *= alphaMul;
                PushSegment(slavePos, masterPos, color);
            }
        }

        if (toolOn && service.DragActive)
            PushSegment(service.DragFrom, service.DragTo, service.DragSnapped ? SnappedDragColor : DragColor);
    }

    private static bool FarFromCamera(Vector3 cameraPos, Vector3 a, Vector3 b) =>
        Vector3.DistanceSquared(cameraPos, a) > MaxDrawDistanceSq &&
        Vector3.DistanceSquared(cameraPos, b) > MaxDrawDistanceSq;

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

        var buffer = commandList.UploadTransientBuffer(
            (ReadOnlySpan<Vector4>)segmentTexels.AsSpan(0, segmentCount * 3));
        DrawTwoPass(commandList, buffer, segmentCount * 9);
    }

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
