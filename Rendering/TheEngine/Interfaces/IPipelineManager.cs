using TheEngine.Handles;
using Veldrid;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Interfaces;

public interface IPipelineManager
{
    Pipeline CreatePipeline(ShaderHandle shaderHandle, PrimitiveTopology topology, GraphicsPipelineDescription description, bool smallLayout, OutputDescription? outputDescription = null);
    internal Pipeline GetPipelineByHandle(PipelineHandle pipelineHandle);
    void DisposePipeline(PipelineHandle pipeline);
}