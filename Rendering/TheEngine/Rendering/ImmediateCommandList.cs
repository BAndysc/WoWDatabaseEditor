using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGLBindings;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Managers;
using Veldrid;
using Pipeline = TheEngine.Resources.Pipeline;
using PixelFormat = OpenGLBindings.PixelFormat;

namespace TheEngine.Rendering;

/// <summary>
/// OpenGL implementation of <see cref="ICommandList"/>: every command executes
/// immediately on the GL context of the calling thread. Pipeline state is applied
/// as individual GL state calls when the bound pipeline actually changes.
/// </summary>
internal class ImmediateCommandList : ICommandList
{
    private readonly TheDevice resourceDevice;
    private readonly IDevice device;
    private readonly TextureManager textureManager;

    private PipelineHandle currentPipelineHandle;
    private Pipeline? currentPipeline;
    private ShaderPass? currentShader;
    private Mesh? currentMesh;
    // the index format is bound with the mesh or via BindIndexBuffer, like vkCmdBindIndexBuffer
    private IndexType? currentIndexType;
    private bool inRenderingPass;

    // On GL a transient slice is a whole buffer object whose storage is orphaned
    // (glBufferData) on every upload: draws already issued keep reading the previous
    // storage, so the pools below recycle buffer objects, not memory ranges.
    private readonly Dictionary<int, NativeBuffer<byte>> transientUniformBuffers = new();
    private class TransientPool
    {
        public readonly List<NativeBuffer<byte>> Buffers = new();
        public int Used;
    }
    private readonly Dictionary<(BufferTypeEnum type, BufferInternalFormat format), TransientPool> transientPools = new();

    public int ShaderSwitches { get; private set; }
    public int MeshSwitches { get; private set; }

    public bool InRenderingPass => inRenderingPass;

    public ImmediateCommandList(TheDevice device, TextureManager textureManager)
    {
        this.resourceDevice = device;
        this.device = device.device;
        this.textureManager = textureManager;
    }

    public void Dispose()
    {
        foreach (var buffer in transientUniformBuffers.Values)
            buffer.Dispose();
        transientUniformBuffers.Clear();
        foreach (var pool in transientPools.Values)
        {
            foreach (var buffer in pool.Buffers)
                buffer.Dispose();
            pool.Buffers.Clear();
        }
        transientPools.Clear();
    }

    public void Begin()
    {
        // better not assume state was saved from the previous frame...
        currentPipelineHandle = PipelineHandle.Empty;
        currentPipeline = null;
        currentShader = null;
        currentMesh = null;
        currentIndexType = null;
        inRenderingPass = false;
        scratchVaoBound = false;
        ShaderSwitches = 0;
        MeshSwitches = 0;
        foreach (var pool in transientPools.Values)
            pool.Used = 0;
    }

    public void End()
    {
        Debug.Assert(!inRenderingPass, "End called inside a rendering pass");
        // on GL every command has already executed; on Vulkan this is where the
        // command buffer gets ended and submitted
    }

    public void BeginRenderingPass(in RenderPassDescriptor descriptor)
    {
        Debug.Assert(!inRenderingPass, "BeginRenderingPass called while another pass is active");
        inRenderingPass = true;
        currentPassHeight = descriptor.Target?.Height ?? descriptor.Height;
        if (descriptor.Target is { } target)
        {
            var renderTexture = (RenderTexture)textureManager.GetTextureByHandle(target.Handle)!;
            renderTexture.ActivateFrameBuffer(descriptor.ViewportScale);
            if (descriptor.ColorLoadOp == LoadOp.Clear)
                renderTexture.Clear(descriptor.ClearColor.Red, descriptor.ClearColor.Green, descriptor.ClearColor.Blue, descriptor.ClearColor.Alpha);
        }
        else
        {
            device.BindFramebuffer(FramebufferTarget.Framebuffer, descriptor.DefaultFramebuffer);
            device.Viewport(0, 0, descriptor.Width, descriptor.Height);
            if (descriptor.ColorLoadOp == LoadOp.Clear)
            {
                device.ClearColor(descriptor.ClearColor.Red, descriptor.ClearColor.Green, descriptor.ClearColor.Blue, descriptor.ClearColor.Alpha);
                device.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }
        }
    }

    public void EndRenderingPass()
    {
        Debug.Assert(inRenderingPass, "EndRenderingPass called without an active pass");
        inRenderingPass = false;
    }

    public void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter)
    {
        Debug.Assert(!inRenderingPass, "Blit is only legal outside a rendering pass");
        ClearBufferMask glMask = 0;
        if (mask.HasFlag(BlitMask.Color))
            glMask |= ClearBufferMask.ColorBufferBit;
        if (mask.HasFlag(BlitMask.Depth))
            glMask |= ClearBufferMask.DepthBufferBit;
        var glFilter = filter == BlitFilter.Linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        textureManager.BlitFramebuffers(source, destination, srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, glMask, glFilter);
    }

    public void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to)
    {
        Debug.Assert(!inRenderingPass, "Barrier is only legal outside a rendering pass");
        // no-op on GL; the driver synchronizes. On Vulkan this becomes an image memory barrier.
    }

    public void SetPipeline(Pipeline pipeline, IShaderPass shaderPass)
    {
        if (currentPipelineHandle != pipeline.Handle)
        {
            currentPipelineHandle = pipeline.Handle;
            currentPipeline = pipeline;
            ApplyPipelineState(pipeline.Description);
        }

        if (currentShader != shaderPass)
        {
            ShaderSwitches++;
            currentShader = (ShaderPass)shaderPass;
            currentShader.Activate();
        }
    }

    public void BindMaterialResources(Material material, MaterialInstanceRenderData? instanceData = null)
    {
        Debug.Assert(currentShader != null, "BindMaterialResources requires a pipeline bound via SetPipeline");
        Debug.Assert(currentPipelineHandle == material.Pipeline.Handle, "the bound pipeline doesn't belong to this material");
        material.ActivateUniforms(currentShader!, instanceData);
    }

    public void BindMaterialResources(Material material, MaterialSnapshot snapshot)
    {
        Debug.Assert(currentShader != null, "BindMaterialResources requires a pipeline bound via SetPipeline");
        Debug.Assert(currentPipelineHandle == material.Pipeline.Handle, "the bound pipeline doesn't belong to this material");
        material.ActivateUniforms(currentShader!, snapshot);
    }

    public void BindUniformBuffer(int slot, INativeBuffer buffer)
    {
        buffer.Activate(slot);
    }

    public void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged
        => BindTransientUniformBuffer(slot, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)));

    public void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data)
    {
        if (!transientUniformBuffers.TryGetValue(slot, out var buffer))
            buffer = transientUniformBuffers[slot] = resourceDevice.CreateBuffer<byte>(BufferTypeEnum.ConstVertex, data.Length);
        // a single buffer object per slot is enough: the upload orphans the storage, so
        // draws issued before this call keep reading the previously bound contents
        buffer.UpdateBuffer(data);
        buffer.Activate(slot);
    }

    public INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged
        => UploadTransient(BufferTypeEnum.StructuredBuffer, format, data);

    public INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged
    {
        Debug.Assert(type == BufferTypeEnum.Vertex || type == BufferTypeEnum.Index, "use the BufferInternalFormat overload for structured data");
        return UploadTransient(type, BufferInternalFormat.None, data);
    }

    private INativeBuffer UploadTransient<T>(BufferTypeEnum type, BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (!transientPools.TryGetValue((type, format), out var pool))
            pool = transientPools[(type, format)] = new TransientPool();
        // unlike the uniform slots, the returned handle can be bound at any later point of
        // the frame, possibly alongside another slice - so every slice within a frame must
        // be a distinct buffer object
        if (pool.Used == pool.Buffers.Count)
            pool.Buffers.Add(resourceDevice.CreateBuffer<byte>(type, Math.Max(1, data.Length * Unsafe.SizeOf<T>()), format));
        var buffer = pool.Buffers[pool.Used++];
        buffer.UpdateBuffer(MemoryMarshal.AsBytes(data));
        return buffer;
    }

    public void BindMesh(IMesh? mesh)
    {
        var concreteMesh = (Mesh?)mesh;
        if (currentMesh != concreteMesh)
        {
            MeshSwitches++;
            currentMesh = concreteMesh;
            currentIndexType = concreteMesh?.IndexType;
            if (concreteMesh != null)
            {
                // the mesh binds its own VAO, displacing the scratch one
                concreteMesh.Activate();
                scratchVaoBound = false;
            }
        }
    }

    public void BindVertexBuffer(INativeBuffer buffer)
    {
        // direct binds bypass the mesh cache, so the next BindMesh must re-activate
        currentMesh = null;
        MeshSwitches++;
        var layouts = currentPipeline?.Description.ShaderSet.VertexLayouts;
        if (layouts is { Length: > 0 })
        {
            // vertex input is pipeline state (as on Vulkan); GL 4.1 has no separate
            // vertex-format state, so the layout is captured in a scratch VAO against
            // the concrete buffer object and re-specified only when either changes
            if (scratchVao == -1)
                scratchVao = device.GenVertexArray();
            if (!scratchVaoBound)
            {
                device.BindVertexArray(scratchVao);
                scratchVaoBound = true;
            }
            var handle = ((NativeBufferBase)buffer).BufferHandle;
            if (scratchVaoPipeline != currentPipelineHandle || scratchVaoVertexBuffer != handle)
            {
                buffer.Activate(0);
                ApplyVertexLayout(in layouts[0]);
                scratchVaoPipeline = currentPipelineHandle;
                scratchVaoVertexBuffer = handle;
            }
        }
        else
        {
            buffer.Activate(0);
        }
    }

    public void BindIndexBuffer(INativeBuffer buffer, IndexType indexType)
    {
        currentMesh = null;
        currentIndexType = indexType;
        // with the scratch VAO bound, the element-buffer binding is captured into it
        buffer.Activate(0);
    }

    private int scratchVao = -1;
    private bool scratchVaoBound;
    private PipelineHandle scratchVaoPipeline = PipelineHandle.Empty;
    private int scratchVaoVertexBuffer = -1;

    private void ApplyVertexLayout(in Veldrid.VertexLayoutDescription layout)
    {
        for (int i = 0; i < layout.Elements.Length; ++i)
        {
            ref readonly var element = ref layout.Elements[i];
            int size;
            VertexAttribPointerType type;
            bool normalized;
            switch (element.Format)
            {
                case Veldrid.VertexElementFormat.Float1: (size, type, normalized) = (1, VertexAttribPointerType.Float, false); break;
                case Veldrid.VertexElementFormat.Float2: (size, type, normalized) = (2, VertexAttribPointerType.Float, false); break;
                case Veldrid.VertexElementFormat.Float3: (size, type, normalized) = (3, VertexAttribPointerType.Float, false); break;
                case Veldrid.VertexElementFormat.Float4: (size, type, normalized) = (4, VertexAttribPointerType.Float, false); break;
                case Veldrid.VertexElementFormat.Byte4: (size, type, normalized) = (4, VertexAttribPointerType.UnsignedByte, false); break;
                case Veldrid.VertexElementFormat.Byte4_Norm: (size, type, normalized) = (4, VertexAttribPointerType.UnsignedByte, true); break;
                default: throw new ArgumentOutOfRangeException(nameof(layout), element.Format, "unsupported vertex element format");
            }
            device.VertexAttribPointer(i, size, type, normalized, (int)layout.Stride, new IntPtr(element.Offset));
            device.EnableVertexAttribArray(i);
        }
    }

    public void Draw(int vertexCount, int firstVertex)
    {
        Debug.Assert(inRenderingPass, "draws are only legal inside a rendering pass");
        device.DrawArrays(CurrentPrimitive, firstVertex, new IntPtr(vertexCount));
    }

    public void DrawIndexed(int indexCount, int firstIndex, int baseVertex)
    {
        Debug.Assert(inRenderingPass, "draws are only legal inside a rendering pass");
        var indexType = ToGlIndexType(currentIndexType!.Value, out var indexSize);
        if (baseVertex == 0)
            device.DrawElements(CurrentPrimitive, indexCount, indexType, new IntPtr(firstIndex * indexSize));
        else
            device.DrawElementsBaseVertex(CurrentPrimitive, indexCount, indexType, new IntPtr(firstIndex * indexSize), baseVertex);
    }

    public void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance)
    {
        Debug.Assert(inRenderingPass, "draws are only legal inside a rendering pass");
        var indexType = ToGlIndexType(currentIndexType!.Value, out var indexSize);
        device.DrawElementsInstanced(CurrentPrimitive, indexCount, indexType, new IntPtr(firstIndex * indexSize), instanceCount);
    }

    private int currentPassHeight;

    public void SetScissor(int x, int y, int width, int height)
    {
        // the rectangle is recorded in Vulkan convention (origin top-left);
        // GL scissor is bottom-left, so flip against the current pass target height
        device.Scissor(x, currentPassHeight - y - height, width, height);
    }

    public void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination)
    {
        Debug.Assert(!inRenderingPass, "ReadPixels is only legal outside a rendering pass");
        var renderTexture = (RenderTexture)textureManager.GetTextureByHandle(texture.Handle)!;
        renderTexture.ActivateSourceFrameBuffer(colorAttachment);
        device.ReadPixels(x, y, width, height, PixelFormat.RedInteger, PixelType.UnsignedInt, destination);
    }

    public void InsertDebugMarker(string label)
    {
        device.Debug(label);
    }

    public void CheckError(string context)
    {
        device.CheckError(context);
    }

#if DEBUG
    private ShaderPass? lastValidatedShader;
#endif

    public void ValidateState()
    {
#if DEBUG
        // glValidateProgram + glGetProgramiv is a synchronous driver round-trip, far too
        // expensive to pay per draw - validating once per shader switch catches the same
        // program/sampler issues at a fraction of the cost
        if (currentShader == lastValidatedShader)
            return;
        lastValidatedShader = currentShader;
        currentShader?.Validate();
#endif
    }

    private PrimitiveType CurrentPrimitive => ToGlPrimitive(currentPipeline?.Description.PrimitiveTopology ?? PrimitiveTopology.TriangleList);

    private static PrimitiveType ToGlPrimitive(PrimitiveTopology topology)
    {
        switch (topology)
        {
            case PrimitiveTopology.TriangleList:
                return PrimitiveType.Triangles;
            case PrimitiveTopology.TriangleStrip:
                return PrimitiveType.TriangleStrip;
            case PrimitiveTopology.LineList:
                return PrimitiveType.Lines;
            case PrimitiveTopology.LineStrip:
                return PrimitiveType.LineStrip;
            case PrimitiveTopology.PointList:
                return PrimitiveType.Points;
            default:
                throw new ArgumentOutOfRangeException(nameof(topology), topology, null);
        }
    }

    private static DrawElementsType ToGlIndexType(IndexType type, out int bytesSize)
    {
        switch (type)
        {
            case IndexType.Short:
                bytesSize = 2;
                return DrawElementsType.UnsignedShort;
            case IndexType.Int:
                bytesSize = 4;
                return DrawElementsType.UnsignedInt;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void ApplyPipelineState(in GraphicsPipelineDescription d)
    {
        // depth testing

        if (d.DepthStencilState.DepthTestEnabled)
        {
            device.Enable(EnableCap.DepthTest);
        }
        else
        {
            device.Disable(EnableCap.DepthTest);
        }

        if (d.DepthStencilState.DepthWriteEnabled)
            device.DepthMask(true);
        else
            device.DepthMask(false);

        device.DepthFunction(d.DepthStencilState.DepthComparison switch
        {
            ComparisonKind.Never => DepthFunction.Never,
            ComparisonKind.Less => DepthFunction.Less,
            ComparisonKind.Equal => DepthFunction.Equal,
            ComparisonKind.LessEqual => DepthFunction.Lequal,
            ComparisonKind.Greater => DepthFunction.Greater,
            ComparisonKind.NotEqual => DepthFunction.Notequal,
            ComparisonKind.GreaterEqual => DepthFunction.Gequal,
            ComparisonKind.Always => DepthFunction.Always,
            _ => throw new ArgumentOutOfRangeException()
        });

        // culling

        if (d.RasterizerState.CullMode == FaceCullMode.None)
        {
            device.Disable(EnableCap.CullFace);
        }
        else
        {
            device.Enable(EnableCap.CullFace);
            // yeah, it is reversed, counterclockwise vs clockwise?
            device.CullFace(d.RasterizerState.CullMode == FaceCullMode.Front ? CullFaceMode.Back : CullFaceMode.Front);
        }

        // scissor (the rectangle itself is dynamic state, set via SetScissor)

        if (d.RasterizerState.ScissorTestEnabled)
            device.Enable(EnableCap.ScissorTest);
        else
            device.Disable(EnableCap.ScissorTest);

        // blending

        var blend = d.BlendState.AttachmentStates[0];
        if (blend.BlendEnabled)
        {
            device.Enable(EnableCap.Blend);
            device.BlendEquation(blend.ColorFunction switch
            {
                BlendFunction.Add => BlendEquationMode.FuncAdd,
                BlendFunction.Subtract => BlendEquationMode.FuncSubtract,
                BlendFunction.ReverseSubtract => BlendEquationMode.FuncReverseSubtract,
                BlendFunction.Minimum => BlendEquationMode.Min,
                BlendFunction.Maximum => BlendEquationMode.Max,
                _ => throw new ArgumentOutOfRangeException()
            });
            device.BlendFuncSeparate(
                ToGlSrcFactor(blend.SourceColorFactor), ToGlDstFactor(blend.DestinationColorFactor),
                ToGlSrcFactor(blend.SourceAlphaFactor), ToGlDstFactor(blend.DestinationAlphaFactor));
        }
        else
        {
            device.Disable(EnableCap.Blend);
        }
    }

    private static BlendingFactorSrc ToGlSrcFactor(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => BlendingFactorSrc.Zero,
        BlendFactor.One => BlendingFactorSrc.One,
        BlendFactor.SourceAlpha => BlendingFactorSrc.SrcAlpha,
        BlendFactor.InverseSourceAlpha => BlendingFactorSrc.OneMinusSrcAlpha,
        BlendFactor.DestinationAlpha => BlendingFactorSrc.DstAlpha,
        BlendFactor.InverseDestinationAlpha => BlendingFactorSrc.OneMinusDstAlpha,
        BlendFactor.SourceColor => BlendingFactorSrc.SrcColor,
        BlendFactor.InverseSourceColor => BlendingFactorSrc.OneMinusSrcColor,
        BlendFactor.DestinationColor => BlendingFactorSrc.DstColor,
        BlendFactor.InverseDestinationColor => BlendingFactorSrc.OneMinusDstColor,
        _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
    };

    private static BlendingFactorDest ToGlDstFactor(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => BlendingFactorDest.Zero,
        BlendFactor.One => BlendingFactorDest.One,
        BlendFactor.SourceAlpha => BlendingFactorDest.SrcAlpha,
        BlendFactor.InverseSourceAlpha => BlendingFactorDest.OneMinusSrcAlpha,
        BlendFactor.DestinationAlpha => BlendingFactorDest.DstAlpha,
        BlendFactor.InverseDestinationAlpha => BlendingFactorDest.OneMinusDstAlpha,
        BlendFactor.SourceColor => BlendingFactorDest.SrcColor,
        BlendFactor.InverseSourceColor => BlendingFactorDest.OneMinusSrcColor,
        BlendFactor.DestinationColor => BlendingFactorDest.DstColor,
        BlendFactor.InverseDestinationColor => BlendingFactorDest.OneMinusDstColor,
        _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
    };
}
