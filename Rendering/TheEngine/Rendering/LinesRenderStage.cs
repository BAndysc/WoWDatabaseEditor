using System.Diagnostics;
using System.Runtime.InteropServices;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using Veldrid;

namespace TheEngine.Rendering;

/// <summary>
/// Accumulates debug lines (DrawLine/DrawBox/DrawFrustum) and draws them in batched,
/// procedural draws - the vertices are pulled from a transient structured buffer by
/// gl_VertexID, so nothing persistent is ever mutated mid-frame (the old immediate path
/// rewrote a shared 2-vertex mesh for every single line, which a Vulkan backend can't do).
///
/// Flushing is incremental: each <see cref="Flush"/> draws only the lines added since the
/// previous flush, into whatever target is currently active. That preserves the immediate-
/// mode routing the callers rely on - lines added during the game's render callbacks land
/// in the game back buffer, lines added by the scene view's overlay land in the scene view
/// texture. The orchestrator flushes at the transparent point of each view and once more
/// after the game's translucent callback (gizmos and path visualizers draw there).
/// </summary>
internal sealed class LinesRenderStage : IRenderStage
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LineVertex
    {
        public Vector4 Position;
        public Vector4 Color;
    }

    private readonly Engine engine;
    private readonly Material material;
    // the shader reads no vertex attributes, but GL still needs a VAO bound to draw;
    // on Vulkan the vertex-input-less pipeline makes this disappear
    private readonly IMesh dummyMesh;

    private LineVertex[] vertices = new LineVertex[256];
    private int vertexCount;
    private int flushedCount;

    public string Name => "Lines";

    public RenderPoint RenderPoints => RenderPoint.Transparent;

    internal LinesRenderStage(Engine engine)
    {
        this.engine = engine;

        var shader = engine.shaderManager.LoadShader("internalShaders/lines.json");
        var pipeline = engine.pipelineManager.CreatePipeline(shader, PrimitiveTopology.LineList, new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleDisabled,
            RasterizerState = RasterizerStateDescription.CullNone with {FillMode = PolygonFillMode.Wireframe},
            DepthStencilState = new DepthStencilStateDescription(true, false, ComparisonKind.LessEqual)
        }, false);
        material = engine.materialManager.CreateMaterial(pipeline);
        dummyMesh = engine.meshManager.CreateMesh(new Vector3[2], new ushort[] { });
    }

    public void Add(Vector3 start, Vector3 end, Vector4 color)
    {
        if (vertexCount + 2 > vertices.Length)
            Array.Resize(ref vertices, vertices.Length * 2);
        vertices[vertexCount++] = new LineVertex() { Position = new Vector4(start, 1), Color = color };
        vertices[vertexCount++] = new LineVertex() { Position = new Vector4(end, 1), Color = color };
    }

    public void PrepareFrame(ICamera camera)
    {
    }

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        Flush(commandList);
    }

    /// <summary>Draws the lines added since the previous flush into the active target.</summary>
    internal void Flush(EngineCommandList commandList)
    {
        int pending = vertexCount - flushedCount;
        if (pending == 0)
            return;

        var buffer = commandList.UploadTransientBuffer(
            (ReadOnlySpan<LineVertex>)vertices.AsSpan(flushedCount, pending));
        flushedCount = vertexCount;

        commandList.SetBuffer(ShaderUniforms.LineVertices, buffer);
        commandList.SetMaterial(material, ShaderPassType.Forward);
        commandList.Draw(dummyMesh, pending);
    }

    public void EndFrame()
    {
        Debug.Assert(flushedCount == vertexCount, "lines were added after the last flush point of the frame (during the GUI phase?) and would be lost");
        vertexCount = 0;
        flushedCount = 0;
    }

    public void Dispose()
    {
        engine.meshManager.DisposeMesh(dummyMesh);
    }
}
