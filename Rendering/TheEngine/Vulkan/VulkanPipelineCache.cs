using Silk.NET.Vulkan;
using TheEngine.Handles;
using Veldrid;
using EnginePipeline = TheEngine.Resources.Pipeline;
using VkPipeline = Silk.NET.Vulkan.Pipeline;
using VkFormat = Silk.NET.Vulkan.Format;
using VkPrimitiveTopology = Silk.NET.Vulkan.PrimitiveTopology;
using VkCompareOp = Silk.NET.Vulkan.CompareOp;
using VkBlendFactor = Silk.NET.Vulkan.BlendFactor;

namespace TheEngine.Vulkan;

/// <summary>
/// Builds and caches VkPipeline variants. The engine's <see cref="EnginePipeline"/> is an
/// immutable state description but its Outputs are NOT authoritative (the same pipeline
/// draws into the game view, scene view and gui targets), so variants are keyed by
/// (pipeline, shader pass, actual attachment formats of the active rendering pass).
///
/// State mapping rules (native top-down framebuffer via negative-height viewport):
///  - frontFace = CounterClockwise (GL's default CCW winding, unflipped in top-down memory),
///  - cull mode swaps Front/Back (mirroring the GL executor's swap),
///  - the vertex layout is the engine's real one (UniversalVertex / ImGui small layout),
///    not the stale layout in the description.
/// </summary>
internal sealed unsafe class VulkanPipelineCache : IDisposable
{
    private readonly VulkanContext ctx;
    private readonly bool supportsFillModeNonSolid;

    private readonly record struct VariantKey(
        PipelineHandle Pipeline,
        VulkanShaderPass Pass,
        VkFormat Color0,
        VkFormat Color1,
        VkFormat Depth,
        int ColorCount);

    private readonly Dictionary<VariantKey, VkPipeline> variants = new();

    // Single-slot memo of the most recent lookup. Draws are heavily batched by pipeline/pass and
    // the attachment formats are constant within a rendering pass, so consecutive Get calls almost
    // always repeat the same key - this skips the dictionary's GetHashCode + bucket probe (the
    // VariantKey hash combines a reference and several enums) in favour of one struct compare.
    private bool hasLast;
    private VariantKey lastKey;
    private VkPipeline lastVariant;

    public VulkanPipelineCache(VulkanContext ctx, bool supportsFillModeNonSolid)
    {
        this.ctx = ctx;
        this.supportsFillModeNonSolid = supportsFillModeNonSolid;
    }

    public VkPipeline Get(EnginePipeline pipeline, VulkanShaderPass pass, ReadOnlySpan<VkFormat> colorFormats, VkFormat depthFormat)
    {
        var key = new VariantKey(pipeline.Handle, pass,
            colorFormats.Length > 0 ? colorFormats[0] : VkFormat.Undefined,
            colorFormats.Length > 1 ? colorFormats[1] : VkFormat.Undefined,
            depthFormat, colorFormats.Length);
        if (hasLast && key.Equals(lastKey))
            return lastVariant;
        if (!variants.TryGetValue(key, out var existing))
        {
            existing = Build(pipeline, pass, colorFormats, depthFormat);
            variants[key] = existing;
        }
        hasLast = true;
        lastKey = key;
        lastVariant = existing;
        return existing;
    }

    internal static VkCompareOp ToVkCompareOp(ComparisonKind kind) => kind switch
    {
        ComparisonKind.Never => VkCompareOp.Never,
        ComparisonKind.Less => VkCompareOp.Less,
        ComparisonKind.Equal => VkCompareOp.Equal,
        ComparisonKind.LessEqual => VkCompareOp.LessOrEqual,
        ComparisonKind.Greater => VkCompareOp.Greater,
        ComparisonKind.NotEqual => VkCompareOp.NotEqual,
        ComparisonKind.GreaterEqual => VkCompareOp.GreaterOrEqual,
        _ => VkCompareOp.Always,
    };

    private VkPipeline Build(EnginePipeline pipeline, VulkanShaderPass pass, ReadOnlySpan<VkFormat> colorFormats, VkFormat depthFormat)
    {
        var desc = pipeline.Description;

        var entryPoint = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("main");
        var stages = stackalloc PipelineShaderStageCreateInfo[2]
        {
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = pass.VertexModule,
                PName = entryPoint,
            },
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = pass.FragmentModule,
                PName = entryPoint,
            },
        };

        // the engine's real vertex layouts (the description's layout is stale)
        VertexInputBindingDescription binding;
        VertexInputAttributeDescription* attributes = stackalloc VertexInputAttributeDescription[6];
        uint attributeCount;
        if (pipeline.SmallLayout)
        {
            binding = new VertexInputBindingDescription(0, 20, VertexInputRate.Vertex);
            attributes[0] = new VertexInputAttributeDescription(0, 0, VkFormat.R32G32Sfloat, 0);
            attributes[1] = new VertexInputAttributeDescription(1, 0, VkFormat.R32G32Sfloat, 8);
            attributes[2] = new VertexInputAttributeDescription(2, 0, VkFormat.R8G8B8A8Unorm, 16);
            attributeCount = 3;
        }
        else
        {
            binding = new VertexInputBindingDescription(0, 48, VertexInputRate.Vertex);
            attributes[0] = new VertexInputAttributeDescription(0, 0, VkFormat.R32G32B32Sfloat, 0);
            attributes[1] = new VertexInputAttributeDescription(1, 0, VkFormat.R32G32B32Sfloat, 12);
            attributes[2] = new VertexInputAttributeDescription(2, 0, VkFormat.R32G32Sfloat, 24);
            attributes[3] = new VertexInputAttributeDescription(3, 0, VkFormat.R32G32Sfloat, 32);
            attributes[4] = new VertexInputAttributeDescription(4, 0, VkFormat.R8G8B8A8Unorm, 40);
            attributes[5] = new VertexInputAttributeDescription(5, 0, VkFormat.R8G8B8A8Unorm, 44);
            attributeCount = 6;
        }

        var vertexInput = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 1,
            PVertexBindingDescriptions = &binding,
            VertexAttributeDescriptionCount = attributeCount,
            PVertexAttributeDescriptions = attributes,
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = desc.PrimitiveTopology switch
            {
                Veldrid.PrimitiveTopology.TriangleList => VkPrimitiveTopology.TriangleList,
                Veldrid.PrimitiveTopology.TriangleStrip => VkPrimitiveTopology.TriangleStrip,
                Veldrid.PrimitiveTopology.LineList => VkPrimitiveTopology.LineList,
                Veldrid.PrimitiveTopology.LineStrip => VkPrimitiveTopology.LineStrip,
                Veldrid.PrimitiveTopology.PointList => VkPrimitiveTopology.PointList,
                _ => throw new ArgumentOutOfRangeException(),
            },
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            PolygonMode = desc.RasterizerState.FillMode == PolygonFillMode.Wireframe && supportsFillModeNonSolid
                ? PolygonMode.Line
                : PolygonMode.Fill,
            // native top-down framebuffer (negative-height viewport): GL-default-CCW front faces
            // are genuinely CCW in memory again. The cull mode swap mirrors the GL executor
            // (desc Front culls GL Back).
            FrontFace = Silk.NET.Vulkan.FrontFace.CounterClockwise,
            CullMode = desc.RasterizerState.CullMode switch
            {
                FaceCullMode.None => CullModeFlags.None,
                FaceCullMode.Front => CullModeFlags.BackBit,
                _ => CullModeFlags.FrontBit,
            },
            LineWidth = 1f,
            // Depth bias is enabled on every pipeline but driven dynamically (vkCmdSetDepthBias):
            // it defaults to 0 at pass begin (no effect on normal passes) and the cascaded shadow
            // pass sets a slope-scaled bias to suppress shadow acne. See VulkanCommandList.SetDepthBias.
            DepthBiasEnable = true,
        };

        var multisample = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        var depthStencil = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = depthFormat != VkFormat.Undefined && desc.DepthStencilState.DepthTestEnabled,
            DepthWriteEnable = depthFormat != VkFormat.Undefined && desc.DepthStencilState.DepthWriteEnabled,
            DepthCompareOp = ToVkCompareOp(desc.DepthStencilState.DepthComparison),
        };

        var blendAttachments = stackalloc PipelineColorBlendAttachmentState[Math.Max(1, colorFormats.Length)];
        for (int i = 0; i < colorFormats.Length; i++)
        {
            var state = i < desc.BlendState.AttachmentStates.Length
                ? desc.BlendState.AttachmentStates[i]
                : BlendAttachmentDescription.Disabled;
            blendAttachments[i] = new PipelineColorBlendAttachmentState
            {
                BlendEnable = state.BlendEnabled,
                SrcColorBlendFactor = ToVk(state.SourceColorFactor),
                DstColorBlendFactor = ToVk(state.DestinationColorFactor),
                ColorBlendOp = ToVk(state.ColorFunction),
                SrcAlphaBlendFactor = ToVk(state.SourceAlphaFactor),
                DstAlphaBlendFactor = ToVk(state.DestinationAlphaFactor),
                AlphaBlendOp = ToVk(state.AlphaFunction),
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            };
        }

        var colorBlend = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            AttachmentCount = (uint)colorFormats.Length,
            PAttachments = blendAttachments,
        };

        // DepthCompareOp + DepthWriteEnable are dynamic (Vulkan 1.3 core) so the engine can switch
        // the opaque pass to Equal + write-off after the depth prepass WITHOUT the material/pipeline
        // changing - the baked DepthComparison/DepthWriteEnabled above are just the defaults the
        // command list applies unless an override is active (see VulkanCommandList.FlushDrawState).
        var dynamicStates = stackalloc DynamicState[5]
        {
            DynamicState.Viewport, DynamicState.Scissor, DynamicState.DepthBias,
            DynamicState.DepthCompareOp, DynamicState.DepthWriteEnable,
        };
        var dynamicState = new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 5,
            PDynamicStates = dynamicStates,
        };

        fixed (VkFormat* pColorFormats = colorFormats)
        {
            var rendering = new PipelineRenderingCreateInfo
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = (uint)colorFormats.Length,
                PColorAttachmentFormats = pColorFormats,
                DepthAttachmentFormat = depthFormat,
            };

            var info = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &rendering,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisample,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlend,
                PDynamicState = &dynamicState,
                Layout = pass.PipelineLayout,
            };
            var result = ctx.vk.CreateGraphicsPipelines(ctx.Device, default, 1, in info, null, out var vkPipeline);
            Silk.NET.Core.Native.SilkMarshal.Free((nint)entryPoint);
            VulkanContext.Check(result, $"graphics pipeline ({pass.ShaderName})");
            return vkPipeline;
        }
    }

    private static VkBlendFactor ToVk(Veldrid.BlendFactor factor) => factor switch
    {
        Veldrid.BlendFactor.Zero => VkBlendFactor.Zero,
        Veldrid.BlendFactor.One => VkBlendFactor.One,
        Veldrid.BlendFactor.SourceAlpha => VkBlendFactor.SrcAlpha,
        Veldrid.BlendFactor.InverseSourceAlpha => VkBlendFactor.OneMinusSrcAlpha,
        Veldrid.BlendFactor.DestinationAlpha => VkBlendFactor.DstAlpha,
        Veldrid.BlendFactor.InverseDestinationAlpha => VkBlendFactor.OneMinusDstAlpha,
        Veldrid.BlendFactor.SourceColor => VkBlendFactor.SrcColor,
        Veldrid.BlendFactor.InverseSourceColor => VkBlendFactor.OneMinusSrcColor,
        Veldrid.BlendFactor.DestinationColor => VkBlendFactor.DstColor,
        Veldrid.BlendFactor.InverseDestinationColor => VkBlendFactor.OneMinusDstColor,
        Veldrid.BlendFactor.BlendFactor => VkBlendFactor.ConstantColor,
        Veldrid.BlendFactor.InverseBlendFactor => VkBlendFactor.OneMinusConstantColor,
        _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null),
    };

    private static BlendOp ToVk(BlendFunction function) => function switch
    {
        BlendFunction.Add => BlendOp.Add,
        BlendFunction.Subtract => BlendOp.Subtract,
        BlendFunction.ReverseSubtract => BlendOp.ReverseSubtract,
        BlendFunction.Minimum => BlendOp.Min,
        BlendFunction.Maximum => BlendOp.Max,
        _ => throw new ArgumentOutOfRangeException(nameof(function), function, null),
    };

    public void Dispose()
    {
        foreach (var pipeline in variants.Values)
            ctx.vk.DestroyPipeline(ctx.Device, pipeline, null);
        variants.Clear();
    }
}
