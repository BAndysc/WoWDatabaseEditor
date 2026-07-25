using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Managers;
using TheEngine.Rendering;
using TheEngine.Structures;
using EnginePipeline = TheEngine.Resources.Pipeline;
using ITexture = TheEngine.Interfaces.ITexture;
using IMesh = TheEngine.Interfaces.IMesh;
using VkPipeline = Silk.NET.Vulkan.Pipeline;
using VkFormat = Silk.NET.Vulkan.Format;
using VkIndexType = Silk.NET.Vulkan.IndexType;
using VkCompareOp = Silk.NET.Vulkan.CompareOp;
using ComparisonKind = Veldrid.ComparisonKind;

namespace TheEngine.Vulkan;

/// <summary>
/// The Vulkan executor <see cref="ICommandList"/>: records every command into the
/// backend's current frame command buffer. Pipeline variants are resolved lazily at
/// draw time against the active rendering pass' attachment formats; material resources
/// become a freshly allocated set 1 descriptor set per bind; SceneData/ObjectData
/// snapshots live in the frame ring bound through set 0 dynamic offsets.
/// </summary>
internal sealed unsafe class VulkanCommandList : ICommandList
{
    private readonly VulkanRenderBackend backend;
    private readonly VulkanContext ctx;
    private readonly TextureManager textureManager;
    private readonly Vk vk;

    private bool inPass;
    private EnginePipeline? currentPipeline;
    private VulkanShaderPass? currentPass;
    private VkPipeline boundVkPipeline;
    private Mesh? currentMesh;
    private VkIndexType? currentIndexType;

    // active rendering pass info (for pipeline variants + scissor flipping)
    private readonly VkFormat[] passColorFormats = new VkFormat[2];
    private int passColorCount;
    private bool passActuallyBegan;
    private VkFormat passDepthFormat;
    private int passWidth, passHeight;

    private readonly uint[] set0Offsets = new uint[2];
    private Rect2D? userScissor;

    // depthCompareOp + depthWriteEnable are dynamic (set per-draw in FlushDrawState from the bound
    // pipeline's baked state, unless an override is active). lastDepth* track what's currently set so
    // we only re-issue on change; reset to "unknown" each command buffer so the first draw applies it.
    private ComparisonKind? depthOverrideComparison;
    private bool? depthOverrideWrite;
    private VkCompareOp lastDepthCompareOp = (VkCompareOp)(-1);
    private int lastDepthWriteEnable = -1;
    private Rect2D appliedScissor;

    public int ShaderSwitches { get; private set; }
    public int MeshSwitches { get; private set; }
    public int Set1CacheHits { get; private set; }
    public int Set1CacheMisses { get; private set; }
    // Number of times the set1/bindless/set3 CmdBindDescriptorSets was skipped because the exact
    // same descriptor sets were already bound (a run of same-shader draws). High vs Set1CacheHits
    // means the draw loop is collapsing to "switch mesh -> draw" with no per-draw descriptor rebind.
    public int Set1BindSkips { get; private set; }

    // The set1/bindless/set3 descriptor sets currently bound on the command buffer (graphics bind
    // point). Reset on command-buffer restart (OnBackendFrameBegin); only WriteAndBindSet1 binds
    // these, so tracking them here lets a run of same-shader draws skip the redundant rebind.
    private DescriptorSet boundSet1;
    private DescriptorSet boundBindless;
    private DescriptorSet boundSet3;

    public bool InRenderingPass => inPass;

    private int lastValidationErrors;

    private sealed class TransientPool
    {
        public readonly List<VulkanBuffer<byte>> Buffers = new();
        public int Used;
    }

    private readonly Dictionary<BufferTypeEnum, TransientPool> transientPools = new();

    public VulkanCommandList(VulkanRenderBackend backend, VulkanContext ctx, TextureManager textureManager)
    {
        this.backend = backend;
        this.ctx = ctx;
        this.textureManager = textureManager;
        vk = ctx.vk;
    }

    private CommandBuffer Cmd => backend.CurrentCb;

    /// <summary>Called by the backend right after the frame command buffer (re)starts.</summary>
    public void OnBackendFrameBegin()
    {
        boundVkPipeline = default;
        // descriptor bindings live on the command buffer, so a restart invalidates them
        boundSet1 = default;
        boundBindless = default;
        boundSet3 = default;
        lastDepthCompareOp = (VkCompareOp)(-1);
        lastDepthWriteEnable = -1;
        depthOverrideComparison = null;
        depthOverrideWrite = null;
        // outside any rendering pass, so layout transitions for newly-registered
        // bindless textures are legal here
        backend.FlushBindlessTextures(Cmd);
        // set 3 is now included in every WriteAndBindSet1 call (sets 1,2,3 together via
        // pass.PipelineLayout) to avoid the cross-layout-incompatibility invalidation that
        // would occur if it were bound once here via PipelineLayout0.
    }

    public void Begin()
    {
        currentPipeline = null;
        currentPass = null;
        currentMesh = null;
        currentIndexType = null;
        inPass = false;
        userScissor = null;
        ShaderSwitches = 0;
        MeshSwitches = 0;
        Set1CacheHits = 0;
        Set1CacheMisses = 0;
        Set1BindSkips = 0;
        // set=1 descriptor sets live in per-frame pools that get reset below the executor's
        // BeginFrame, so a cached handle from last frame would be stale. Persistent (frame-scoped)
        // buffers from last frame are equally stale (transient), so clear them too.
        cachedSet1Pass = null;
        cachedMaterial = null;
        persistentBuffers.Clear();
        foreach (var pool in transientPools.Values)
            pool.Used = 0;
    }

    public void End()
    {
        Debug.Assert(!inPass, "End called inside a rendering pass");
    }

    // ------------------------------------------------------------- passes

    public void BeginRenderingPass(in RenderPassDescriptor descriptor)
    {
        backend.AssertInFrame();
        Debug.Assert(!inPass, "BeginRenderingPass called while another pass is active");
        inPass = true;

        var colorAttachments = stackalloc RenderingAttachmentInfo[2];
        RenderingAttachmentInfo depthAttachment = default;
        bool hasDepth = false;

        if (descriptor.Target is { } target)
        {
            var rt = (VulkanRenderTexture)textureManager.GetTextureByHandle(target.Handle)!;
            passWidth = rt.Width;
            passHeight = rt.Height;
            passColorCount = rt.Colors.Length;
            foreach (var color in rt.Colors)
                color.TransitionTo(Cmd, ImageLayout.ColorAttachmentOptimal);
            rt.Depth?.TransitionTo(Cmd, ImageLayout.DepthAttachmentOptimal);

            for (int i = 0; i < rt.Colors.Length; i++)
            {
                var clear = new ClearValue();
                if (i == 0)
                    clear.Color = new ClearColorValue(descriptor.ClearColor.Red, descriptor.ClearColor.Green, descriptor.ClearColor.Blue, descriptor.ClearColor.Alpha);
                // attachment 1 is the R32ui picking buffer - integer clear to 0 (the zero-initialized ClearValue)
                colorAttachments[i] = new RenderingAttachmentInfo
                {
                    SType = StructureType.RenderingAttachmentInfo,
                    ImageView = rt.Colors[i].View,
                    ImageLayout = ImageLayout.ColorAttachmentOptimal,
                    LoadOp = descriptor.ColorLoadOp == LoadOp.Clear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                    ClearValue = clear,
                };
                passColorFormats[i] = rt.Colors[i].Format;
            }

            if (rt.Depth != null)
            {
                hasDepth = true;
                passDepthFormat = rt.Depth.Format;
                depthAttachment = new RenderingAttachmentInfo
                {
                    SType = StructureType.RenderingAttachmentInfo,
                    ImageView = rt.Depth.View,
                    ImageLayout = ImageLayout.DepthAttachmentOptimal,
                    LoadOp = (descriptor.DepthLoadOp ?? descriptor.ColorLoadOp) == LoadOp.Clear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                    ClearValue = new ClearValue { DepthStencil = new ClearDepthStencilValue(1, 0) },
                };
            }
            else
                passDepthFormat = VkFormat.Undefined;
        }
        else
        {
            // the swapchain pass (FinalizeRendering): single color attachment, no depth
            if (!backend.HasImage)
            {
                // window minimized: keep the pass bookkeeping consistent, draws will be skipped
                passWidth = Math.Max(1, descriptor.Width);
                passHeight = Math.Max(1, descriptor.Height);
                passColorCount = 0;
                passDepthFormat = VkFormat.Undefined;
                passActuallyBegan = false;
                return;
            }
            var wasUndefined = backend.PresentLayout == ImageLayout.Undefined;
            backend.TransitionSwapchainImage(Cmd, ImageLayout.ColorAttachmentOptimal);
            passWidth = (int)backend.PresentExtent.Width;
            passHeight = (int)backend.PresentExtent.Height;
            passColorCount = 1;
            passColorFormats[0] = backend.PresentFormat;
            passDepthFormat = VkFormat.Undefined;
            colorAttachments[0] = new RenderingAttachmentInfo
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageView = backend.PresentView,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
                LoadOp = descriptor.ColorLoadOp == LoadOp.Clear ? AttachmentLoadOp.Clear
                    : wasUndefined ? AttachmentLoadOp.DontCare : AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                ClearValue = new ClearValue(new ClearColorValue(descriptor.ClearColor.Red, descriptor.ClearColor.Green, descriptor.ClearColor.Blue, descriptor.ClearColor.Alpha)),
            };
        }

        var renderingInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)passWidth, (uint)passHeight)),
            LayerCount = 1,
            ColorAttachmentCount = (uint)passColorCount,
            PColorAttachments = colorAttachments,
            PDepthAttachment = hasDepth ? &depthAttachment : null,
        };
        passActuallyBegan = true;
        vk.CmdBeginRendering(Cmd, in renderingInfo);

        // negative-height viewport (native Vulkan): flips the NDC->framebuffer Y mapping so the
        // GL-style +Y-up projection renders top-down into memory (row 0 == top of screen). The
        // dynamic-resolution scale shrinks the viewport like glViewport did.
        // The final swapchain pass targeting the Avalonia compositor's interop image must flip back
        // (positive height): the compositor samples the imported image with the opposite Y origin.
        float vpH = passHeight * descriptor.ViewportScale;
        bool flipPresent = descriptor.Target is null && backend.PresentFlipsY;
        var viewport = flipPresent
            ? new Viewport(0, 0, passWidth * descriptor.ViewportScale, vpH, 0, 1)
            : new Viewport(0, vpH, passWidth * descriptor.ViewportScale, -vpH, 0, 1);
        vk.CmdSetViewport(Cmd, 0, 1, in viewport);
        appliedScissor = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)passWidth, (uint)passHeight));
        vk.CmdSetScissor(Cmd, 0, 1, in appliedScissor);
        // every pipeline has DepthBiasEnable=true (dynamic): reset to no bias for normal passes.
        // The cascaded shadow pass overrides this via SetDepthBias before its draws.
        vk.CmdSetDepthBias(Cmd, 0f, 0f, 0f);
    }

    /// <summary>Sets the dynamic depth bias (vkCmdSetDepthBias) for subsequent draws in this pass.
    /// Used by the cascaded shadow pass to push depth away from the light and suppress acne; reset
    /// to 0 at the start of every rendering pass.</summary>
    public void SetDepthBias(float constantFactor, float slopeFactor)
    {
        backend.AssertInFrame();
        vk.CmdSetDepthBias(Cmd, constantFactor, 0f, slopeFactor);
    }

    /// <summary>Overrides the depth compare op / write enable for subsequent draws, regardless of the
    /// bound pipeline's baked state (passing null restores that baked state). Lets the engine run the
    /// opaque pass as Equal + write-off after the depth prepass without changing any material.</summary>
    public void SetDepthStateOverride(ComparisonKind? comparison, bool? writeEnabled)
    {
        backend.AssertInFrame();
        depthOverrideComparison = comparison;
        depthOverrideWrite = writeEnabled;
    }

    public void EndRenderingPass()
    {
        backend.AssertInFrame();
        Debug.Assert(inPass, "EndRenderingPass called without an active pass");
        inPass = false;
        if (passActuallyBegan)
            vk.CmdEndRendering(Cmd);
    }

    // ------------------------------------------------------------- transfer

    private VulkanTexture? ResolveAttachment(ITexture texture, bool depth)
    {
        var native = textureManager.GetTextureByHandle(texture.Handle);
        return native switch
        {
            VulkanRenderTexture rt => depth ? rt.Depth : rt.Colors[0],
            VulkanTexture tex => depth == tex.IsDepth ? tex : null,
            _ => null,
        };
    }

    public void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter)
    {
        backend.AssertInFrame();
        Debug.Assert(!inPass, "Blit is only legal outside a rendering pass");
        if ((mask & BlitMask.Color) != 0)
            BlitOne(ResolveAttachment(source, false), ResolveAttachment(destination, false),
                srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1,
                filter == BlitFilter.Linear ? Filter.Linear : Filter.Nearest);
        if ((mask & BlitMask.Depth) != 0)
            BlitOne(ResolveAttachment(source, true), ResolveAttachment(destination, true),
                srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, Filter.Nearest);
    }

    private void BlitOne(VulkanTexture? src, VulkanTexture? dst, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, Filter filter)
    {
        if (src == null || dst == null)
            return;
        src.TransitionTo(Cmd, ImageLayout.TransferSrcOptimal);
        dst.TransitionTo(Cmd, ImageLayout.TransferDstOptimal);
        var blit = new ImageBlit
        {
            SrcSubresource = new ImageSubresourceLayers(src.Aspect, 0, 0, 1),
            DstSubresource = new ImageSubresourceLayers(dst.Aspect, 0, 0, 1),
        };
        // top-down memory: blit window coords == memory rows directly (row 0 = top)
        blit.SrcOffsets[0] = new Offset3D(srcX0, srcY0, 0);
        blit.SrcOffsets[1] = new Offset3D(srcX1, srcY1, 1);
        blit.DstOffsets[0] = new Offset3D(dstX0, dstY0, 0);
        blit.DstOffsets[1] = new Offset3D(dstX1, dstY1, 1);
        vk.CmdBlitImage(Cmd, src.Image, ImageLayout.TransferSrcOptimal, dst.Image, ImageLayout.TransferDstOptimal, 1, in blit, filter);
    }

    public void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to)
    {
        backend.AssertInFrame();
        Debug.Assert(!inPass, "Barrier is only legal outside a rendering pass");
        var native = textureManager.GetTextureByHandle(texture.Handle);
        if (native is VulkanRenderTexture rt)
        {
            foreach (var color in rt.Colors)
                color.TransitionTo(Cmd, UsageToLayout(to, color));
            // ShaderRead includes the depth attachment: transparent shaders (water) sample
            // the opaque copy's depth. RenderTarget skips it - pass begin transitions it.
            if (to is ResourceUsage.TransferSource or ResourceUsage.TransferDestination or ResourceUsage.ShaderRead)
                rt.Depth?.TransitionTo(Cmd, UsageToLayout(to, rt.Depth));
        }
        else if (native is VulkanTexture tex)
            tex.TransitionTo(Cmd, UsageToLayout(to, tex));
    }

    private static ImageLayout UsageToLayout(ResourceUsage usage, VulkanTexture texture) => usage switch
    {
        ResourceUsage.RenderTarget => texture.AttachmentLayout,
        ResourceUsage.ShaderRead => ImageLayout.ShaderReadOnlyOptimal,
        ResourceUsage.TransferSource => ImageLayout.TransferSrcOptimal,
        _ => ImageLayout.TransferDstOptimal,
    };

    // ------------------------------------------------------------- pipeline + materials

    public void SetPipeline(EnginePipeline pipeline, IShaderPass shaderPass)
    {
        backend.AssertInFrame();
        if (!ReferenceEquals(currentPass, shaderPass))
            ShaderSwitches++;
        currentPipeline = pipeline;
        currentPass = (VulkanShaderPass)shaderPass;
    }

    public void SetBindlessTextureIndex(int packedIndex)
    {
        backend.AssertInFrame();
        if (currentPass == null)
            return;
        vk.CmdPushConstants(Cmd, currentPass.PipelineLayout, ShaderStageFlags.FragmentBit, 0, sizeof(int), &packedIndex);
    }

    public void SetScissor(int x, int y, int width, int height)
    {
        backend.AssertInFrame();
        // recorded in top-left convention; flipped against the pass height at draw time
        userScissor = new Rect2D(new Offset2D(x, y), new Extent2D((uint)Math.Max(0, width), (uint)Math.Max(0, height)));
    }

    public void SetBuffer(GlobalUniformHandle uniform, INativeBuffer buffer)
    {
        backend.AssertInFrame();
        for (int i = 0; i < pendingBuffers.Count; ++i)
        {
            if (pendingBuffers[i].uniform == uniform)
            {
                pendingBuffers[i] = (uniform, buffer);
                return;
            }
        }
        pendingBuffers.Add((uniform, buffer));
    }

    public void BindMaterialResources(Material material)
    {
        backend.AssertInFrame();
        Debug.Assert(currentPass != null, "BindMaterialResources requires a pipeline bound via SetPipeline");
        var pass = currentPass!;

        // No whole-bind early-out here: the caller (render stage) decides when a bind is needed, and
        // WriteAndBindSet1 below still avoids the expensive work when nothing changed - it reuses the
        // cached descriptor set (Set1CacheMatches) and skips the vkCmdBindDescriptorSets when the same
        // set1/2/3 are already bound (BindSets1To3IfChanged). The old top-level comparison mostly just
        // failed (batching already merges identical-material runs) and cost a comparison per batch.
        scratchBuffers.Clear();
        // Resolve only the buffer resources this shader pass consumes by walking pass.Bindings,
        // pulling each from the queued per-draw buffers first, then the frame-persistent buffers
        // (e.g. the instancing SSBOs, set once per render range), then the material's own SSBO.
        // Textures are no longer resolved here - every sampled texture is bindless now (set 2,
        // indexed by an int in the material data), so set 1 holds only buffers.
        foreach (var binding in pass.Bindings)
        {
            if (TryGetPendingBuffer(binding.Uniform, out var pending))
            {
                scratchBuffers.Add((binding.Uniform, pending));
            }
            else if (TryGetPersistentBuffer(binding.Uniform, out var persistent))
            {
                scratchBuffers.Add((binding.Uniform, persistent));
            }
            else if (binding.Uniform == MaterialDataArrayUniform && material.Pipeline.MaterialArrayBuffer != null)
            {
                // the per-type MaterialData SSBO (binding 0), read straight off the pipeline (shared by
                // all materials of this type) - no per-material field or per-draw dictionary. Kept in
                // scratchBuffers so the set-1 cache still keys on it. Anything else unresolved falls
                // back to DefaultStorageBuffer below.
                scratchBuffers.Add((binding.Uniform, material.Pipeline.MaterialArrayBuffer));
            }
        }
        pendingBuffers.Clear();
        WriteAndBindSet1(pass, material);
    }

    private bool TryGetPendingBuffer(GlobalUniformHandle uniform, out INativeBuffer buffer)
    {
        for (int i = 0; i < pendingBuffers.Count; ++i)
        {
            if (pendingBuffers[i].uniform == uniform)
            {
                buffer = pendingBuffers[i].buffer;
                return true;
            }
        }
        buffer = null!;
        return false;
    }

    private bool TryGetPersistentBuffer(GlobalUniformHandle uniform, out INativeBuffer buffer)
    {
        for (int i = 0; i < persistentBuffers.Count; ++i)
        {
            if (persistentBuffers[i].uniform == uniform)
            {
                buffer = persistentBuffers[i].buffer;
                return true;
            }
        }
        buffer = null!;
        return false;
    }

    private readonly List<(GlobalUniformHandle uniform, INativeBuffer buffer)> pendingBuffers = new();
    // Frame-persistent buffers survive across draws (unlike pendingBuffers, which are consumed per
    // draw). The object draw stage sets the per-frame instancing SSBOs here once per render range
    // instead of re-queuing all five every batch. Cleared in Begin and via ClearPersistentBuffers.
    private readonly List<(GlobalUniformHandle uniform, INativeBuffer buffer)> persistentBuffers = new();

    private readonly List<(GlobalUniformHandle uniform, INativeBuffer buffer)> scratchBuffers = new();

    // the set-1 binding-0 MaterialData SSBO uniform, resolved straight off Pipeline.MaterialArrayBuffer
    private static readonly GlobalUniformHandle MaterialDataArrayUniform = Material.GetUniformLocation("MaterialDataArray");

    public void SetPersistentBuffer(GlobalUniformHandle uniform, INativeBuffer buffer)
    {
        for (int i = 0; i < persistentBuffers.Count; ++i)
        {
            if (persistentBuffers[i].uniform == uniform)
            {
                persistentBuffers[i] = (uniform, buffer);
                return;
            }
        }
        persistentBuffers.Add((uniform, buffer));
    }

    public void ClearPersistentBuffers()
    {
        if (persistentBuffers.Count == 0)
            return;
        persistentBuffers.Clear();
    }

    // Caches the most recently bound set=1 so consecutive draws with the same material and
    // the same resolved buffers/textures (e.g. a run of instanced batches sharing one
    // material and the global per-frame instancing buffers) can skip
    // vkAllocateDescriptorSets/vkUpdateDescriptorSets and just rebind the same set.
    private VulkanShaderPass? cachedSet1Pass;
    // For pass.HasMaterialData materials the cache keys on (material, generation) - a cheap int
    // compare instead of memcmp-ing the material bytes every draw. For materials whose data lives
    // in a per-type MaterialDataArray SSBO (HasMaterialData == false) there are no per-material
    // bytes: the shared SSBO reference in cachedSet1Buffers is the key, so cachedMaterial is left
    // null and the cache can hit across different materials of the same type.
    private Material? cachedMaterial;
    private uint cachedMaterialGen;
    private DescriptorSet cachedSet1;
    private readonly List<(GlobalUniformHandle uniform, INativeBuffer buffer)> cachedSet1Buffers = new();

    private bool Set1CacheMatches(VulkanShaderPass pass, Material material)
    {
        if (!ReferenceEquals(cachedSet1Pass, pass))
            return false;
        if (pass.HasMaterialData && (!ReferenceEquals(cachedMaterial, material) || cachedMaterialGen != material.ResourceGeneration))
            return false;
        if (cachedSet1Buffers.Count != scratchBuffers.Count)
            return false;
        for (int i = 0; i < scratchBuffers.Count; i++)
            if (!cachedSet1Buffers[i].Equals(scratchBuffers[i]))
                return false;
        return true;
    }

    private void StoreSet1Cache(VulkanShaderPass pass, Material material, DescriptorSet set)
    {
        cachedSet1Pass = pass;
        cachedMaterial = pass.HasMaterialData ? material : null;
        cachedMaterialGen = material.ResourceGeneration;
        cachedSet1Buffers.Clear();
        cachedSet1Buffers.AddRange(scratchBuffers);
        cachedSet1 = set;
    }

    private void WriteAndBindSet1(VulkanShaderPass pass, Material material)
    {
        var materialData = pass.HasMaterialData ? material.MaterialDataBytes : ReadOnlySpan<byte>.Empty;

        if (Set1CacheMatches(pass, material))
        {
            Set1CacheHits++;
            foreach (var (_, buffer) in scratchBuffers)
                (buffer as IVulkanBuffer)?.MarkUsed();
            BindSets1To3IfChanged(pass, cachedSet1);
            return;
        }

        Set1CacheMisses++;
        var set = backend.AllocateSet1(pass.Set1Layout);

        // set 1 is buffers only now: an optional MaterialData UBO (binding 0) plus std430 SSBO
        // bindings (MaterialDataArray, the instancing SSBOs, the glyph/line vertex-pull buffers).
        // 32 covers the largest.
        const int maxWrites = 32;
        var writes = stackalloc WriteDescriptorSet[maxWrites];
        var bufferInfos = stackalloc DescriptorBufferInfo[2];
        var storageBufferInfos = stackalloc DescriptorBufferInfo[maxWrites];
        int writeCount = 0, storageCount = 0;

        if (pass.HasMaterialData)
        {
            int size = Math.Max(pass.MaterialDataSize, Math.Max(16, materialData.Length));
            var (ring, offset, ptr) = backend.RingAlloc(size, backend.UboAlignment);
            new Span<byte>((void*)ptr, size).Clear();
            materialData.CopyTo(new Span<byte>((void*)ptr, size));
            bufferInfos[0] = new DescriptorBufferInfo(ring, offset, (ulong)size);
            writes[writeCount++] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = set,
                DstBinding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                PBufferInfo = &bufferInfos[0],
            };
        }

        foreach (var binding in pass.Bindings)
        {
            // set 1 holds only std430 SSBO bindings now (all textures are bindless in set 2, and the
            // samplerBuffer/texel path is gone - glyph/line buffers are SSBOs too). Reflection only
            // registers StorageBuffer bindings here.
            Debug.Assert(binding.Type == DescriptorType.StorageBuffer, "unexpected non-SSBO set-1 binding");
            IVulkanBuffer? resolved = null;
            foreach (var (uniform, buffer) in scratchBuffers)
            {
                if (uniform != binding.Uniform)
                    continue;
                resolved = buffer as IVulkanBuffer;
                break;
            }
            resolved ??= backend.DefaultStorageBuffer;
            resolved.MarkUsed();
            storageBufferInfos[storageCount] = new DescriptorBufferInfo(resolved.Buffer, 0, (ulong)resolved.SizeInBytes);
            writes[writeCount++] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = set,
                DstBinding = binding.Binding,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                PBufferInfo = &storageBufferInfos[storageCount],
            };
            storageCount++;
        }

        if (writeCount > 0)
            vk.UpdateDescriptorSets(ctx.Device, (uint)writeCount, writes, 0, null);
        // A freshly allocated set never equals the bound one, so this always binds on a cache miss
        // (which is also the only place the set1 layout can change - i.e. a shader switch).
        BindSets1To3IfChanged(pass, set);

        StoreSet1Cache(pass, material, set);
    }

    /// <summary>
    /// Binds set 1 (material), set 2 (bindless) and set 3 (game globals) together, but skips the
    /// CmdBindDescriptorSets entirely when all three are already the bound sets. Set 2 and set 3
    /// are frame-constant, so within a run of same-shader draws (which resolve to the same cached
    /// set 1 - material data is a shared per-type SSBO and textures are bindless) the descriptors
    /// are already bound and the draw loop collapses to "switch mesh -> draw". The set1 layout only
    /// changes on a shader switch, which always misses the set1 cache and allocates a new set, so a
    /// matching handle here guarantees the same pass/layout as what is bound.
    /// </summary>
    private void BindSets1To3IfChanged(VulkanShaderPass pass, DescriptorSet set1)
    {
        var bindless = backend.BindlessSet;
        var set3 = backend.CurrentGameSet3;
        if (set1.Handle == boundSet1.Handle && bindless.Handle == boundBindless.Handle && set3.Handle == boundSet3.Handle)
        {
            Set1BindSkips++;
            return;
        }
        var sets = stackalloc DescriptorSet[3] { set1, bindless, set3 };
        vk.CmdBindDescriptorSets(Cmd, PipelineBindPoint.Graphics, pass.PipelineLayout, 1, 3, sets, 0, null);
        boundSet1 = set1;
        boundBindless = bindless;
        boundSet3 = set3;
    }

    // ------------------------------------------------------------- global uniforms

    public void BindUniformBuffer(int slot, INativeBuffer buffer)
    {
        // global UBO slots live in the frame ring; persistent buffers are host-mapped,
        // so a snapshot copy gives identical semantics
        if (buffer is VulkanBuffer<byte> byteBuffer)
            BindTransientUniformBuffer(slot, byteBuffer.MappedSpan);
        else
            throw new NotSupportedException("BindUniformBuffer expects a vulkan buffer");
    }

    public void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged
        => BindTransientUniformBuffer(slot, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1)));

    public void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data)
    {
        backend.AssertInFrame();
        Debug.Assert(slot is 0 or 1, "only the SceneData/ObjectData slots exist in set 0");
        var (_, offset, ptr) = backend.RingAlloc(data.Length, backend.UboAlignment);
        data.CopyTo(new Span<byte>((void*)ptr, data.Length));
        set0Offsets[slot] = offset;
        var set0 = backend.CurrentSet0;
        fixed (uint* offsets = set0Offsets)
            vk.CmdBindDescriptorSets(Cmd, PipelineBindPoint.Graphics, backend.PipelineLayout0, 0, 1, &set0, 2, offsets);
    }

    // ------------------------------------------------------------- forward+ lighting

    public void UploadLightData(ReadOnlySpan<GpuPointLight> lights) => backend.UploadLights(lights);

    public void UploadDecalData(ReadOnlySpan<GpuDecal> decals) => backend.UploadDecals(decals);

    public void DispatchTileCullCompute(int depthTextureIndex, int screenWidth, int screenHeight, int tilesX, int tilesY, bool isSceneView = false)
    {
        backend.AssertInFrame();
        Debug.Assert(!inPass, "DispatchTileCullCompute is only legal outside a rendering pass");
        var shader = backend.TileCullShader;
        vk.CmdBindPipeline(Cmd, PipelineBindPoint.Compute, shader.Pipeline);

        var set0 = backend.CurrentSet0;
        fixed (uint* offsets = set0Offsets)
            vk.CmdBindDescriptorSets(Cmd, PipelineBindPoint.Compute, shader.PipelineLayout, 0, 1, &set0, 2, offsets);

        var bindlessSet = backend.BindlessSet;
        vk.CmdBindDescriptorSets(Cmd, PipelineBindPoint.Compute, shader.PipelineLayout, 2, 1, &bindlessSet, 0, null);

        var pushConstants = new LightCullPushConstants
        {
            DepthTextureIndex = depthTextureIndex,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            IsSceneView = isSceneView ? 1 : 0,
        };
        vk.CmdPushConstants(Cmd, shader.PipelineLayout, ShaderStageFlags.ComputeBit, 0, (uint)Unsafe.SizeOf<LightCullPushConstants>(), &pushConstants);

        vk.CmdDispatch(Cmd, (uint)tilesX, (uint)tilesY, 1);

        // LightGrid/LightIndexList and DecalGrid/DecalIndexList: ShaderWrite (compute) ->
        // ShaderRead (fragment), so the opaque pass that follows sees this dispatch's writes.
        // Also gate subsequent ComputeShader writes: this is called once per view (game view,
        // then scene view) per frame, both writing into the same buffers at different gridSet
        // offsets - without this, the second dispatch can race the first's writes (confirmed by
        // the Vulkan validation layer as a WRITE_AFTER_WRITE hazard on vkCmdDispatch).
        var barrier = new MemoryBarrier2
        {
            SType = StructureType.MemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.ComputeShaderBit,
            SrcAccessMask = AccessFlags2.ShaderWriteBit,
            DstStageMask = PipelineStageFlags2.FragmentShaderBit | PipelineStageFlags2.ComputeShaderBit,
            DstAccessMask = AccessFlags2.ShaderReadBit | AccessFlags2.ShaderWriteBit,
        };
        var dep = new DependencyInfo
        {
            SType = StructureType.DependencyInfo,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &barrier,
        };
        vk.CmdPipelineBarrier2(Cmd, in dep);
    }

    // ------------------------------------------------------------- transient buffers

    public INativeBuffer UploadTransientBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
        => UploadTransient(BufferTypeEnum.StructuredBuffer, data);

    public INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged
    {
        Debug.Assert(type == BufferTypeEnum.Vertex || type == BufferTypeEnum.Index || type == BufferTypeEnum.IndirectCommands, "use the parameterless overload for structured (std430 SSBO) data");
        return UploadTransient(type, data);
    }

    private INativeBuffer UploadTransient<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (!transientPools.TryGetValue(type, out var pool))
            pool = transientPools[type] = new TransientPool();
        if (pool.Used == pool.Buffers.Count)
            pool.Buffers.Add(new VulkanBuffer<byte>(backend, ctx, type, Math.Max(4, data.Length * Unsafe.SizeOf<T>())));
        var buffer = pool.Buffers[pool.Used++];
        // UpdateBuffer orphans the backing if a frame in flight still reads it
        buffer.UpdateBuffer(MemoryMarshal.AsBytes(data));
        return buffer;
    }

    // ------------------------------------------------------------- geometry

    public void BindMesh(IMesh? mesh)
    {
        backend.AssertInFrame();
        var concreteMesh = (Mesh?)mesh;
        if (currentMesh == concreteMesh)
            return;
        MeshSwitches++;
        currentMesh = concreteMesh;
        if (concreteMesh == null)
            return;
        currentIndexType = concreteMesh.IndexType;
        BindVertexBufferInternal((IVulkanBuffer)concreteMesh.VerticesBuffer!);
        BindIndexBufferInternal((IVulkanBuffer)concreteMesh.IndicesBuffer!, concreteMesh.IndexType);
    }

    public void BindVertexBuffer(INativeBuffer buffer)
    {
        backend.AssertInFrame();
        currentMesh = null;
        MeshSwitches++;
        BindVertexBufferInternal((IVulkanBuffer)buffer);
    }

    public void BindIndexBuffer(INativeBuffer buffer, VkIndexType indexType)
    {
        backend.AssertInFrame();
        currentMesh = null;
        currentIndexType = indexType;
        BindIndexBufferInternal((IVulkanBuffer)buffer, indexType);
    }

    private void BindVertexBufferInternal(IVulkanBuffer buffer)
    {
        buffer.MarkUsed();
        var vkBuffer = buffer.Buffer;
        ulong offset = 0;
        vk.CmdBindVertexBuffers(Cmd, 0, 1, in vkBuffer, in offset);
    }

    private void BindIndexBufferInternal(IVulkanBuffer buffer, VkIndexType indexType)
    {
        buffer.MarkUsed();
        vk.CmdBindIndexBuffer(Cmd, buffer.Buffer, 0, indexType);
    }

    // ------------------------------------------------------------- draws

    private bool FlushDrawState()
    {
        if (passColorCount == 0 && passDepthFormat == VkFormat.Undefined)
            return false; // minimized-window swapchain pass: nothing to draw into (depth-only passes have passColorCount == 0 too, but a valid passDepthFormat)
        Debug.Assert(currentPipeline != null && currentPass != null, "draw without a pipeline bound");
        var variant = backend.PipelineCache.Get(currentPipeline!, currentPass!,
            passColorFormats.AsSpan(0, passColorCount), passDepthFormat);
        if (variant.Handle != boundVkPipeline.Handle)
        {
            vk.CmdBindPipeline(Cmd, PipelineBindPoint.Graphics, variant);
            boundVkPipeline = variant;
        }

        // dynamic depth compare/write: the override (e.g. the opaque pass's Equal + write-off after
        // the prepass) wins, otherwise the pipeline's baked state. Only needed when there's a depth
        // attachment; re-issued only on change.
        if (passDepthFormat != VkFormat.Undefined)
        {
            var dss = currentPipeline!.Description.DepthStencilState;
            var compare = VulkanPipelineCache.ToVkCompareOp(depthOverrideComparison ?? dss.DepthComparison);
            bool write = depthOverrideWrite ?? dss.DepthWriteEnabled;
            if (compare != lastDepthCompareOp)
            {
                vk.CmdSetDepthCompareOp(Cmd, compare);
                lastDepthCompareOp = compare;
            }
            int wi = write ? 1 : 0;
            if (wi != lastDepthWriteEnable)
            {
                vk.CmdSetDepthWriteEnable(Cmd, write);
                lastDepthWriteEnable = wi;
            }
        }

        Rect2D wanted;
        if (currentPipeline!.Description.RasterizerState.ScissorTestEnabled && userScissor.HasValue)
        {
            var s = userScissor.Value;
            // native top-down memory: scissor is recorded top-left and used directly, no flip
            int x = Math.Clamp(s.Offset.X, 0, passWidth);
            int y = Math.Clamp(s.Offset.Y, 0, passHeight);
            uint w = (uint)Math.Min((int)s.Extent.Width, passWidth - x);
            uint h = (uint)Math.Min((int)s.Extent.Height, passHeight - y);
            wanted = new Rect2D(new Offset2D(x, y), new Extent2D(w, h));
        }
        else
            wanted = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)passWidth, (uint)passHeight));

        if (wanted.Offset.X != appliedScissor.Offset.X || wanted.Offset.Y != appliedScissor.Offset.Y ||
            wanted.Extent.Width != appliedScissor.Extent.Width || wanted.Extent.Height != appliedScissor.Extent.Height)
        {
            appliedScissor = wanted;
            vk.CmdSetScissor(Cmd, 0, 1, in wanted);
        }
        return true;
    }

    public void Draw(int vertexCount, int firstVertex)
    {
        backend.AssertInFrame();
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        if (!FlushDrawState())
            return;
        vk.CmdDraw(Cmd, (uint)vertexCount, 1, (uint)firstVertex, 0);
    }

    public void DrawIndexed(int indexCount, int firstIndex, int baseVertex)
    {
        backend.AssertInFrame();
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        if (!FlushDrawState())
            return;
        vk.CmdDrawIndexed(Cmd, (uint)indexCount, 1, (uint)firstIndex, baseVertex, 0);
    }

    public void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance)
    {
        backend.AssertInFrame();
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        if (!FlushDrawState())
            return;
        vk.CmdDrawIndexed(Cmd, (uint)indexCount, (uint)instanceCount, (uint)firstIndex, baseVertex, (uint)firstInstance);
    }

    public void DrawIndexedIndirect(INativeBuffer indirectBuffer, int offset, int drawCount, int stride)
    {
        backend.AssertInFrame();
        Debug.Assert(inPass, "draws are only legal inside a rendering pass");
        if (!FlushDrawState())
            return;
        var buffer = (IVulkanBuffer)indirectBuffer;
        buffer.MarkUsed();
        vk.CmdDrawIndexedIndirect(Cmd, buffer.Buffer, (ulong)offset, (uint)drawCount, (uint)stride);
    }

    // ------------------------------------------------------------- readback / debug

    public void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination)
    {
        Debug.Assert(!inPass, "ReadPixels is only legal outside a rendering pass");
        var native = textureManager.GetTextureByHandle(texture.Handle);
        var color = native switch
        {
            VulkanRenderTexture rt => rt.Colors[colorAttachment],
            VulkanTexture tex => tex,
            _ => null,
        };
        if (color == null)
        {
            destination.Clear();
            return;
        }
        // synchronous picking readback between frames: wait for the GPU, one-shot copy
        vk.QueueWaitIdle(ctx.Queue);
        backend.ReadPixelsSync(color, x, y, width, height, destination);
    }

    public int DeferredReadSlot => backend.FrameInFlightIndex;

    public uint? ReadPixelDeferred(ITexture texture, int colorAttachment, int x, int y, int channel = 0)
    {
        Debug.Assert(!inPass, "ReadPixelDeferred is only legal outside a rendering pass");
        var native = textureManager.GetTextureByHandle(texture.Handle);
        var color = native switch
        {
            VulkanRenderTexture rt => rt.Colors[colorAttachment],
            VulkanTexture tex => tex,
            _ => null,
        };
        if (color == null)
            return null;
        return backend.ReadPixelDeferred(Cmd, color, x, y, channel);
    }

    public void InsertDebugMarker(string label)
    {
        backend.AssertInFrame();
        if (ctx.DebugUtilsExt == null)
            return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(label + "\0");
        fixed (byte* pLabel = bytes)
        {
            var labelInfo = new DebugUtilsLabelEXT { SType = StructureType.DebugUtilsLabelExt, PLabelName = pLabel };
            ctx.DebugUtilsExt.CmdInsertDebugUtilsLabel(Cmd, in labelInfo);
        }
    }

    public void CheckError(string context)
    {
        if (ctx.ValidationErrors != lastValidationErrors)
        {
            Console.WriteLine($"[vk] {ctx.ValidationErrors - lastValidationErrors} validation error(s) before '{context}'");
            lastValidationErrors = ctx.ValidationErrors;
        }
    }

    public void Dispose() => DisposeResources();

    internal void DisposeResources()
    {
        foreach (var pool in transientPools.Values)
        {
            foreach (var buffer in pool.Buffers)
                buffer.Dispose();
            pool.Buffers.Clear();
        }
        transientPools.Clear();
    }
}
