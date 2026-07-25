using TheEngine.Handles;
using TheEngine.Interfaces;
using Veldrid;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Managers;

public class PipelineManager : IPipelineManager
{
    private readonly Engine engine;
    private List<Pipeline?> pipelines = new List<Pipeline?>();

    public PipelineManager(Engine engine)
    {
        this.engine = engine;
    }

    public PipelineHandle MaxHandle => new PipelineHandle(pipelines.Count - 1);

    public Pipeline CreatePipeline(ShaderHandle shaderHandle, PrimitiveTopology topology,
        GraphicsPipelineDescription description, bool smallLayout, OutputDescription? outputDescription)
    {
        var handle = new PipelineHandle(pipelines.Count);
        var pipeline = new Pipeline(handle, outputDescription ?? IRenderManager.DefaultOutput, shaderHandle, engine.shaderManager.GetShaderByHandle(shaderHandle), topology, description, smallLayout);
        pipelines.Add(pipeline);
        return pipeline;
    }

    public Pipeline CreatePipeline(ShaderHandle shaderHandle, PrimitiveTopology topology,
        GraphicsPipelineDescription description, bool smallLayout)
    {
        return CreatePipeline(shaderHandle, topology, description, smallLayout, null);
    }

    public Pipeline GetPipelineByHandle(PipelineHandle handle)
    {
        return pipelines[handle.Handle] ?? throw new NullReferenceException("This pipeline has been disposed.");
    }

    public void DisposePipeline(PipelineHandle pipeline)
    {
        pipelines[pipeline.Handle]?.Dispose();
        pipelines[pipeline.Handle] = null;
    }

    public void Dispose()
    {
        foreach (var pipeline in pipelines)
        {
            pipeline?.Dispose();
        }
        pipelines.Clear();
        pipelines = null!;
    }
}