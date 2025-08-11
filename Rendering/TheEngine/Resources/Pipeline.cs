using TheEngine.Handles;
using TheEngine.Interfaces;
using Veldrid;
using Shader = TheAvaloniaOpenGL.Resources.Shader;
using GraphicsDevice = TheAvaloniaOpenGL.TheDevice;

namespace TheEngine.Resources;

public class Pipeline : System.IDisposable
{
    private readonly ShaderHandle handle;
    // private Veldrid.Pipeline forwardPipeline;
    // private Veldrid.Pipeline? shadowPipeline;

    internal Shader Shader { get; }
    public GraphicsPipelineDescription Description { get; }
    internal bool SmallLayout { get; }
    public PipelineHandle Handle { get; }

    internal Pipeline(PipelineHandle pipelineHandle, GraphicsDevice device, OutputDescription output, ShaderHandle handle, Shader shader, PrimitiveTopology topology, GraphicsPipelineDescription description, bool smallLayout)
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

        // todo: veldrid
        // description.ShaderSet = new ShaderSetDescription(
        //     vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
        //     shaders: shader.ForwardPass.Shaders);
        // description.ResourceLayouts = shader.layouts;
        description.Outputs = output;
        Description = description;
        SmallLayout = smallLayout;
        // forwardPipeline = device.ResourceFactory.CreateGraphicsPipeline(description);
        // forwardPipeline.Name = "Forward Pipeline " + shader.ForwardPass.Name;

        if (shader.ShadowPass != null)
        {
            // todo: veldrid
            // description.ShaderSet = new ShaderSetDescription(
            //     vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
            //     shaders: shader.ShadowPass.Shaders);
            description.Outputs = IRenderManager.ShadowPassOutput;
            description.RasterizerState = description.RasterizerState with { DepthClipEnabled = false };
            // shadowPipeline = device.ResourceFactory.CreateGraphicsPipeline(description);
            // shadowPipeline.Name = "Shadow Pipeline " + shader.ShadowPass.Name;
        }
    }

    public ShaderHandle ShaderHandle => handle;
    // public Veldrid.Pipeline ForwardPipeline => forwardPipeline;
    // public Veldrid.Pipeline? ShadowPipeline => shadowPipeline;

    public void Dispose()
    {
        // forwardPipeline.Dispose();
        // shadowPipeline?.Dispose();
    }
}