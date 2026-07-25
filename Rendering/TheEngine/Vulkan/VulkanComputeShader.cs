using Silk.NET.Vulkan;
using TheEngine.Resources;
using VkPipeline = Silk.NET.Vulkan.Pipeline;

namespace TheEngine.Vulkan;

/// <summary>
/// A compiled compute shader: pipeline layout = [set0Layout, bindlessLayout] so compute
/// passes can read the Forward+ light/grid/index SSBOs and SceneData (set 0) plus sample
/// any bindless texture (set 1, e.g. the depth prepass output) without a dedicated
/// descriptor set. Optional push constants (compute stage only) carry per-dispatch
/// parameters that don't belong in SceneData (e.g. a bindless texture index).
/// </summary>
internal sealed unsafe class VulkanComputeShader : IDisposable
{
    private readonly VulkanContext ctx;

    internal ShaderModule Module;
    internal VkPipeline Pipeline;
    internal PipelineLayout PipelineLayout;

    internal VulkanComputeShader(VulkanContext ctx, DescriptorSetLayout set0Layout, DescriptorSetLayout set1Layout, DescriptorSetLayout bindlessLayout, string sourcePath, uint pushConstantSize)
    {
        this.ctx = ctx;

        var source = ShaderSource.ParseShader(sourcePath, true, "COMPUTE_SHADER");
        var spirv = GlslCompiler.Compile(source, "comp", sourcePath);
        fixed (byte* pCode = spirv)
        {
            var moduleInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)pCode,
            };
            VulkanContext.Check(ctx.vk.CreateShaderModule(ctx.Device, in moduleInfo, null, out Module), "compute shader module " + sourcePath);
        }

        // [set0Layout, set1Layout (empty placeholder), bindlessLayout] - the bindless
        // texture arrays are declared at set=2 in theengine.cginc to match the graphics
        // pipelines' layout, so set 1 must still be present even though compute doesn't use it.
        var setLayouts = stackalloc DescriptorSetLayout[3] { set0Layout, set1Layout, bindlessLayout };
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = pushConstantSize,
        };
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 3,
            PSetLayouts = setLayouts,
            PushConstantRangeCount = pushConstantSize > 0 ? 1u : 0u,
            PPushConstantRanges = pushConstantSize > 0 ? &pushConstantRange : null,
        };
        VulkanContext.Check(ctx.vk.CreatePipelineLayout(ctx.Device, in layoutInfo, null, out PipelineLayout), "compute pipeline layout " + sourcePath);

        var entryPoint = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("main");
        var stageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = Module,
            PName = entryPoint,
        };
        var pipelineInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Stage = stageInfo,
            Layout = PipelineLayout,
        };
        var result = ctx.vk.CreateComputePipelines(ctx.Device, default, 1, in pipelineInfo, null, out Pipeline);
        Silk.NET.Core.Native.SilkMarshal.Free((nint)entryPoint);
        VulkanContext.Check(result, "compute pipeline " + sourcePath);
    }

    public void Dispose()
    {
        var vk = ctx.vk;
        var device = ctx.Device;
        var module = Module;
        var pipeline = Pipeline;
        var layout = PipelineLayout;
        ctx.DestroyLater(() =>
        {
            vk.DestroyPipeline(device, pipeline, null);
            vk.DestroyPipelineLayout(device, layout, null);
            vk.DestroyShaderModule(device, module, null);
        });
    }
}
