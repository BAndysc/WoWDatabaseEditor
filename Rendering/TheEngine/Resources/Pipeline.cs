using TheEngine.Resources;
using TheEngine.Handles;
using Veldrid;
using IShader = TheEngine.Resources.IShader;

namespace TheEngine.Resources;

public class Pipeline : System.IDisposable
{
    private readonly ShaderHandle handle;

    internal IShader Shader { get; }
    public GraphicsPipelineDescription Description { get; }
    internal bool SmallLayout { get; }
    public PipelineHandle Handle { get; }

    /// <summary>The per-type MaterialData SSBO (set 1 binding 0) shared by every material drawn with
    /// this pipeline, or null if the shader has no MaterialDataArray. A pipeline's shader has exactly
    /// one MaterialData layout, so this is 1:1 with the material struct type. Set by MaterialManager;
    /// the set-1 bind reads it directly instead of a per-material field or a per-draw dictionary.</summary>
    internal INativeBuffer? MaterialArrayBuffer;

    internal Pipeline(PipelineHandle pipelineHandle, OutputDescription output, ShaderHandle handle, IShader shader, PrimitiveTopology topology, GraphicsPipelineDescription description, bool smallLayout)
    {
        this.Handle = pipelineHandle;
        this.handle = handle;
        Shader = shader;

        description.PrimitiveTopology = topology;
        description.ResourceLayouts = System.Array.Empty<ResourceLayout>();

        if (description.BlendState.AttachmentStates.Length == 1 && output.ColorAttachments.Length == 2)
        {
            description.BlendState = new BlendStateDescription(
                description.BlendState.BlendFactor,
                description.BlendState.AttachmentStates[0],
                BlendAttachmentDescription.Disabled);
        }

        VertexLayoutDescription vertexLayout;

        if (smallLayout)
        {
            vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2, 0),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2, 8),
                new VertexElementDescription("in_color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4_Norm, 16));
        }
        else
        {
            vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("position", VertexElementSemantic.Position, VertexElementFormat.Float4, 0),
                new VertexElementDescription("normal", VertexElementSemantic.Normal, VertexElementFormat.Float4, 16),
                new VertexElementDescription("uv1", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2, 32),
                new VertexElementDescription("uv2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2, 40),
                new VertexElementDescription("color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4, 48),
                new VertexElementDescription("color2", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4, 52));
        }

        // shaders are resolved via ShaderHandle at draw time (GL); only the vertex layout is part of the description for now
        description.ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, System.Array.Empty<Veldrid.Shader>());
        // NOT authoritative: the same pipeline legitimately draws into targets with different
        // attachment sets (game view, scene view, gui). A Vulkan executor must derive the real
        // output formats from the active rendering pass and cache pipeline variants per format set.
        description.Outputs = output;
        Description = description;
        SmallLayout = smallLayout;
    }

    public ShaderHandle ShaderHandle => handle;

    public void Dispose()
    {
    }
}
