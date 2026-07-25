using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Silk.NET.Vulkan;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Rendering;
using TheEngine.Structures;
using TheEngine.Utils;
using TheMaths;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using VkFormat = Silk.NET.Vulkan.Format;
using VkSampler = Silk.NET.Vulkan.Sampler;

namespace TheEngine.Vulkan;

/// <summary>
/// The Vulkan backend: resource factories, the per-frame lifecycle (2 frames in flight)
/// and the shared services the executor command list uses (frame ring buffer with the
/// set 0 dynamic-offset descriptor set, per-frame descriptor pools, pipeline variants,
/// samplers). Rendering convention: native Vulkan - the projection emits ndc z in [0,1]
/// (no VK_CLIP remap) and a negative-height viewport stores render targets top-down, so
/// no present/screenshot Y-flip and front faces use the natural CCW winding.
/// </summary>
internal sealed unsafe class VulkanRenderBackend : IRenderBackend
{
    private const int FramesInFlight = 2;
    private const long RingSize = 32 * 1024 * 1024;
    private const int Set0Range = 512;

    internal readonly VulkanContext ctx;
    private readonly IWindowHost windowHost;
    internal readonly IVulkanPresentTarget presentTarget;
    internal readonly VulkanSamplerCache SamplerCache;
    internal readonly VulkanPipelineCache PipelineCache;
    internal DescriptorSetLayout Set0Layout;
    internal PipelineLayout PipelineLayout0;
    internal readonly uint UboAlignment;
    // std430 SSBO bound as the fallback for any unresolved set-1 storage-buffer binding
    internal readonly VulkanBuffer<Vector4> DefaultStorageBuffer;

    // set 3: app-registered frame-global storage buffers, bound once per frame.
    // Layout is fixed at MaxGlobalBuffers bindings; unregistered slots use dummyGlobalBuffer.
    internal DescriptorSetLayout GameSet3Layout;
    private DescriptorPool gameSet3Pool;
    private VkBuffer dummyGlobalBuffer;
    private VmaAllocation dummyGlobalMemory;
    private const int MaxGlobalBuffers = 16;
    // indexed by binding (callers pick an explicit binding so it matches the shader's
    // `set = 3, binding = N` declaration); null entries fall back to dummyGlobalBuffer.
    private readonly VulkanGlobalBufferBase?[] globalBuffers = new VulkanGlobalBufferBase?[MaxGlobalBuffers];

    // bindless texture array (set 2): SampledImage (binding 0) + Sampler (binding 1)
    // are split into two separate descriptor arrays, written once per registered
    // texture/sampler and bound once per draw alongside set 1. A material stores a
    // single packed int per texture: (samplerSlot << TextureIndexBits) | textureSlot.
    // MoltenVK reports maxPerStageDescriptorUpdateAfterBindSamplers == 1024 but
    // maxPerStageDescriptorUpdateAfterBindSampledImages ~= 1e6, so splitting raises the
    // effective texture limit from 1024 to BindlessTextureCapacity while the handful of
    // distinct sampler states stay well under the 1024 sampler cap.
    internal const uint BindlessTextureCapacity = 16384;
    internal const uint BindlessSamplerCapacity = 128;
    internal const int TextureIndexBits = 20;
    internal const int TextureIndexMask = (1 << TextureIndexBits) - 1;
    internal DescriptorSetLayout BindlessLayout;
    internal DescriptorSet BindlessSet;
    // empty layout used as set 1's placeholder in compute pipeline layouts: compute shaders
    // don't use the per-draw material/instancing set (set 1), but the bindless arrays they
    // do use are declared at set=2 in theengine.cginc, so set 1 must still be present.
    internal DescriptorSetLayout EmptyLayout;
    internal VulkanComputeShader TileCullShader = null!;
    private DescriptorPool bindlessPool;
    private readonly Dictionary<VulkanTexture, int> bindlessSlots = new();
    private readonly List<VulkanTexture> pendingBindlessWrites = new();
    // slots whose texture was disposed and whose descriptor has been repointed to the
    // fallback; reused by GetBindlessTextureSlot so the array doesn't grow without bound.
    private readonly Stack<int> freeBindlessSlots = new();
    private int nextBindlessSlot;
    // a permanent 1x1 texture every unused/retired bindless slot points at, so the
    // descriptor array never references a freed image (which GPU-faults on MoltenVK's
    // resident argument buffers). Created in the constructor, freed only at shutdown.
    private VulkanTexture bindlessFallback = null!;
    private readonly Dictionary<(FilteringMode, WrapMode, bool mips), int> bindlessSamplerSlots = new();
    private readonly List<(int slot, VkSampler sampler)> pendingBindlessSamplerWrites = new();
    private int nextBindlessSamplerSlot;

    private VulkanCommandList? executor;
    internal long TotalBufferBytesTracker;
    // Async content uploads on the transfer queue (textures), off the render thread. Pumped each
    // frame in BeginFrame; its timeline is waited on in EndFrame so the graphics queue never samples
    // an image before its copy completed.
    internal VulkanUploadQueue UploadQueue = null!;

    private sealed class Frame
    {
        public CommandPool Pool;
        public CommandBuffer Cmd;
        public VkSemaphore ImageAvailable;
        public Fence Fence;
        public bool Submitted;
        public ulong SubmissionId;
        public readonly List<DescriptorPool> DescriptorPools = new();
        public int PoolIndex;
        public VkBuffer Ring;
        public VmaAllocation RingMemory;
        public byte* RingMapped;
        public long RingOffset;
        public DescriptorSet Set0;
        public DescriptorSet GameSet3;

        // Forward+ tiled light culling storage buffers (set 0, bindings 2-4): host-visible/
        // coherent like the ring, fixed-size for the frame's lifetime so the descriptors
        // written once in CreateFrame stay valid (no backing rotation to chase).
        public VkBuffer LightBuffer;
        public VmaAllocation LightMemory;
        public byte* LightMapped;
        public VkBuffer LightGridBuffer;
        public VmaAllocation LightGridMemory;
        public VkBuffer LightIndexBuffer;
        public VmaAllocation LightIndexMemory;

        // Forward+ tiled decal culling storage buffers (set 0, bindings 5-7); same fixed-size,
        // host-visible/coherent shape as the light buffers above.
        public VkBuffer DecalBuffer;
        public VmaAllocation DecalMemory;
        public byte* DecalMapped;
        public VkBuffer DecalGridBuffer;
        public VmaAllocation DecalGridMemory;
        public VkBuffer DecalIndexBuffer;
        public VmaAllocation DecalIndexMemory;
        public QueryPool TimestampPool;
        public bool TimestampsWritten;
    }

    private static readonly bool ProfileGpu = Environment.GetEnvironmentVariable("THEENGINE_PROFILE") == "1";
    /// <summary>Last measured GPU frame time in milliseconds (only valid when THEENGINE_PROFILE=1).</summary>
    public float LastGpuMs { get; private set; }
    /// <summary>CPU ms spent in BeginFrame's WaitForFences (GPU sync). Profile-only.</summary>
    public float LastFenceWaitMs { get; private set; }
    /// <summary>CPU ms spent in BeginFrame's AcquireNextImage (present/vsync sync). Profile-only.</summary>
    public float LastAcquireMs { get; private set; }
    /// <summary>CPU ms spent in BeginFrame's low-latency vsync throttle (vkWaitForPresentKHR).
    /// Always measured; high values here with vsync on mean the throttle is doing its job
    /// (idle-waiting for the previous present to hit the display instead of queueing frames).</summary>
    public float LastPresentWaitMs { get; private set; }
    /// <summary>CPU ms spent reading back the GPU timestamp query (profiling-only artifact).</summary>
    public float LastQueryReadbackMs { get; private set; }
    /// <summary>CPU ms spent in ProcessPendingDestroys (vkFree/vkDestroy of retired resources). Profile-only.</summary>
    public float LastDestroyMs { get; private set; }
    /// <summary>Deferred destroys executed this frame / still queued (profiling).</summary>
    public int LastDestroyedCount => ctx.LastDestroyedCount;
    public int PendingDestroyQueueLength => ctx.PendingDestroyQueueLength;

    private readonly Frame[] frames = new Frame[FramesInFlight];
    private int frameIndex;
    private DescriptorPool set0Pool;

    internal CommandBuffer CurrentCb => frames[frameIndex].Cmd;
    internal DescriptorSet CurrentSet0 => frames[frameIndex].Set0;
    internal DescriptorSet CurrentGameSet3 => frames[frameIndex].GameSet3;
    internal uint CurrentImageIndex { get; private set; }
    internal bool HasImage { get; private set; }

    // the final "swapchain pass" reads these through the present target (KHR swapchain or external image)
    internal Extent2D PresentExtent => presentTarget.Extent;
    internal VkFormat PresentFormat => presentTarget.Format;
    internal ImageView PresentView => presentTarget.GetView(CurrentImageIndex);
    internal ImageLayout PresentLayout => presentTarget.GetLayout(CurrentImageIndex);
    internal bool PresentFlipsY => presentTarget.FlipY;

    public string Name => "Vulkan (" + ctx.DeviceName + ")";

    public bool SupportsVSyncControl => presentTarget.SupportsVSyncControl;

    public bool VSync
    {
        get => presentTarget.VSync;
        set => presentTarget.VSync = value;
    }

    public long TotalBufferBytes => TotalBufferBytesTracker;

    public VulkanRenderBackend(VulkanContext ctx, SurfaceKHR surface, IWindowHost windowHost)
        : this(ctx, new KhrSwapchainPresentTarget(ctx, surface, (uint)Math.Max(1, windowHost.WindowWidth), (uint)Math.Max(1, windowHost.WindowHeight)), windowHost)
    {
    }

    public VulkanRenderBackend(VulkanContext ctx, IVulkanPresentTarget presentTarget, IWindowHost windowHost)
    {
        this.ctx = ctx;
        ctx.Backend = this;
        this.windowHost = windowHost;
        this.presentTarget = presentTarget;
        UboAlignment = (uint)Math.Max(16, (int)ctx.DeviceProperties.Limits.MinUniformBufferOffsetAlignment);
        SamplerCache = new VulkanSamplerCache(ctx);

        ctx.vk.GetPhysicalDeviceFeatures(ctx.PhysicalDevice, out var features);
        PipelineCache = new VulkanPipelineCache(ctx, features.FillModeNonSolid);

        CreateSet0Layout();
        CreateBindlessLayout();
        var emptyLayoutInfo = new DescriptorSetLayoutCreateInfo { SType = StructureType.DescriptorSetLayoutCreateInfo };
        VulkanContext.Check(ctx.vk.CreateDescriptorSetLayout(ctx.Device, in emptyLayoutInfo, null, out EmptyLayout), "empty layout");
        CreateGameSet3Layout();
        // PipelineLayout0 spans all 4 sets so it can anchor set-0 dynamic-offset binds and
        // the set-3 game-frame-bound bind from the same compatible layout root.
        var layouts4 = stackalloc DescriptorSetLayout[4] { Set0Layout, EmptyLayout, BindlessLayout, GameSet3Layout };
        // MUST match the push-constant range on every graphics shader's pass.PipelineLayout (see
        // VulkanShaderPass.CreateLayouts): pipeline-layout compatibility requires identical push
        // constant ranges, so without this the set-0 bind issued through PipelineLayout0 is
        // disturbed the moment a pass pipeline is bound ("set 0 not bound" validation errors).
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = sizeof(int),
        };
        var fullLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 4,
            PSetLayouts = layouts4,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstantRange,
        };
        VulkanContext.Check(ctx.vk.CreatePipelineLayout(ctx.Device, in fullLayoutInfo, null, out PipelineLayout0), "pipeline layout 0");
        TileCullShader = new VulkanComputeShader(ctx, Set0Layout, EmptyLayout, BindlessLayout, "internalShaders/tile_cull.comp", (uint)Unsafe.SizeOf<LightCullPushConstants>());
        for (int i = 0; i < FramesInFlight; i++)
            frames[i] = CreateFrame();

        DefaultStorageBuffer = new VulkanBuffer<Vector4>(this, ctx, BufferTypeEnum.StructuredBuffer, 64);

        // create the permanent fallback and point every bindless slot at it up front, so the
        // array is fully valid before any real texture is registered and stays valid after any
        // texture is retired (the retired slot is repointed here in ReleaseBindlessTexture).
        bindlessFallback = NewSampledTexture(1, 1, VkFormat.R8G8B8A8Unorm, 1);
        ClearTexture(bindlessFallback); // leaves it in ShaderReadOnlyOptimal
        FillBindlessSlots(0, BindlessTextureCapacity, bindlessFallback.View);
        // same reasoning for the sampler array: a freshly-registered sampler's descriptor write
        // is only queued (pendingBindlessSamplerWrites) and isn't applied until the next
        // FlushBindlessTextures, so any slot used in the same frame it's first allocated would
        // otherwise be read uninitialized.
        FillBindlessSamplerSlots(0, BindlessSamplerCapacity, SamplerCache.Get(FilteringMode.Linear, WrapMode.Repeat, false));

        UploadQueue = new VulkanUploadQueue(ctx);
    }

    /// <summary>Writes <paramref name="view"/> into a contiguous run of bindless SampledImage slots.</summary>
    private void FillBindlessSlots(uint start, uint count, ImageView view)
    {
        if (count == 0)
            return;
        var infos = new DescriptorImageInfo[count];
        for (int i = 0; i < count; i++)
            infos[i] = new DescriptorImageInfo { ImageView = view, ImageLayout = ImageLayout.ShaderReadOnlyOptimal };
        fixed (DescriptorImageInfo* pInfos = infos)
        {
            var write = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = BindlessSet,
                DstBinding = 0,
                DstArrayElement = start,
                DescriptorCount = count,
                DescriptorType = DescriptorType.SampledImage,
                PImageInfo = pInfos,
            };
            ctx.vk.UpdateDescriptorSets(ctx.Device, 1, &write, 0, null);
        }
    }

    /// <summary>Writes <paramref name="sampler"/> into a contiguous run of bindless Sampler slots.</summary>
    private void FillBindlessSamplerSlots(uint start, uint count, VkSampler sampler)
    {
        if (count == 0)
            return;
        var infos = new DescriptorImageInfo[count];
        for (int i = 0; i < count; i++)
            infos[i] = new DescriptorImageInfo { Sampler = sampler, ImageLayout = ImageLayout.ShaderReadOnlyOptimal };
        fixed (DescriptorImageInfo* pInfos = infos)
        {
            var write = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = BindlessSet,
                DstBinding = 1,
                DstArrayElement = start,
                DescriptorCount = count,
                DescriptorType = DescriptorType.Sampler,
                PImageInfo = pInfos,
            };
            ctx.vk.UpdateDescriptorSets(ctx.Device, 1, &write, 0, null);
        }
    }

    /// <summary>Reclaims a disposed texture's bindless slot. The descriptor is repointed at the
    /// permanent fallback immediately so every subsequently-recorded frame binds a valid image;
    /// the disposed texture's own image stays alive (deferred free, keyed to the current
    /// submission) for the in-flight frames that already encoded it, and the fallback is
    /// permanently resident, so neither the old nor the new descriptor value can fault.
    /// Only the slot's return to the free-list is deferred, so the slot is not handed to a new
    /// texture (a different image) until those in-flight frames have completed.</summary>
    internal void ReleaseBindlessTexture(VulkanTexture tex)
    {
        if (!bindlessSlots.TryGetValue(tex, out var slot))
            return;
        bindlessSlots.Remove(tex);
        // never write a descriptor for a texture that's going away
        pendingBindlessWrites.Remove(tex);
        FillBindlessSlots((uint)slot, 1, bindlessFallback.View);
        ctx.DestroyLater(() => freeBindlessSlots.Push(slot));
    }

    private void CreateSet0Layout()
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[8];
        // binding 0 (SceneData) also needs ComputeBit: the light-cull compute pass reads
        // projectionInv/zNear/zFar/tilesX/tilesY from it. Binding 1 (ObjectData) is unused
        // by compute, but must still be bound (set 0 always has 2 dynamic offsets).
        bindings[0] = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[1] = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
        };
        // Forward+ tiled light culling: Light SSBO (read by the cull compute pass and the
        // forward fragment shader), LightGrid/LightIndexList (written by the cull compute
        // pass, read by the forward fragment shader).
        bindings[2] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.LIGHT_BUFFER_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[3] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.LIGHT_GRID_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[4] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.LIGHT_INDEX_LIST_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        // Forward+ tiled decal culling: same shape as the light bindings above (Decal SSBO
        // read by both stages, DecalGrid/DecalIndexList written by the merged cull compute
        // pass and read by the forward fragment shader).
        bindings[5] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.DECAL_BUFFER_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[6] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.DECAL_GRID_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[7] = new DescriptorSetLayoutBinding
        {
            Binding = Constants.DECAL_INDEX_LIST_BINDING,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        var info = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 8,
            PBindings = bindings,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorSetLayout(ctx.Device, in info, null, out Set0Layout), "set0 layout");

        var poolSizes = stackalloc DescriptorPoolSize[2]
        {
            new DescriptorPoolSize(DescriptorType.UniformBufferDynamic, FramesInFlight * 2),
            new DescriptorPoolSize(DescriptorType.StorageBuffer, FramesInFlight * 6),
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = FramesInFlight,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorPool(ctx.Device, in poolInfo, null, out set0Pool), "set0 pool");
    }

    private void CreateBindlessLayout()
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[2];
        // ComputeBit is included so the light-cull pass can sample the depth texture
        // through the same bindless arrays as the forward fragment shaders.
        bindings[0] = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.SampledImage,
            DescriptorCount = BindlessTextureCapacity,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        bindings[1] = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = BindlessSamplerCapacity,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.ComputeBit,
        };
        var bindingFlags = stackalloc DescriptorBindingFlags[2]
        {
            DescriptorBindingFlags.PartiallyBoundBit | DescriptorBindingFlags.UpdateAfterBindBit,
            DescriptorBindingFlags.PartiallyBoundBit | DescriptorBindingFlags.UpdateAfterBindBit,
        };
        var flagsInfo = new DescriptorSetLayoutBindingFlagsCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            BindingCount = 2,
            PBindingFlags = bindingFlags,
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            PNext = &flagsInfo,
            Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
            BindingCount = 2,
            PBindings = bindings,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorSetLayout(ctx.Device, in layoutInfo, null, out BindlessLayout), "bindless layout");

        var poolSizes = stackalloc DescriptorPoolSize[2]
        {
            new DescriptorPoolSize(DescriptorType.SampledImage, BindlessTextureCapacity),
            new DescriptorPoolSize(DescriptorType.Sampler, BindlessSamplerCapacity),
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.UpdateAfterBindBit,
            MaxSets = 1,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorPool(ctx.Device, in poolInfo, null, out bindlessPool), "bindless pool");

        var bindlessLayout = BindlessLayout;
        var setAlloc = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = bindlessPool,
            DescriptorSetCount = 1,
            PSetLayouts = &bindlessLayout,
        };
        VulkanContext.Check(ctx.vk.AllocateDescriptorSets(ctx.Device, in setAlloc, out BindlessSet), "bindless set alloc");
    }

    /// <summary>Assigns (or returns the existing) bindless array index for a texture,
    /// packed as (samplerSlot &lt;&lt; TextureIndexBits) | textureSlot. The descriptor
    /// write and any required layout transition are deferred to
    /// <see cref="FlushBindlessTextures"/>, since textures are often registered outside
    /// frame recording. The sampler slot is resolved eagerly from the texture's current
    /// filtering/wrapping, so SetFiltering/SetWrapping must be called before this.</summary>
    public int GetBindlessTextureSlot(INativeTexture texture)
    {
        var tex = texture switch
        {
            VulkanRenderTexture rt => rt.Colors[0],
            VulkanTexture t => t,
            _ => throw new ArgumentException("not a vulkan texture"),
        };
        int slot = ReserveBindlessSlot(tex);
        int samplerSlot = GetBindlessSamplerSlot(tex.Filtering, tex.Wrapping, tex.MipLevels > 1);
        return (samplerSlot << TextureIndexBits) | slot;
    }

    /// <summary>Assigns (or returns the existing) bindless texture-array slot for a texture and
    /// queues its descriptor write for the next <see cref="FlushBindlessTextures"/>. Sampled
    /// textures call this at creation (see FinalizeUpload) so every texture always has a slot;
    /// render textures get theirs lazily the first time they're sampled.</summary>
    private int ReserveBindlessSlot(VulkanTexture tex)
    {
        if (!bindlessSlots.TryGetValue(tex, out var slot))
        {
            // reuse a retired slot before growing into a fresh one
            if (!freeBindlessSlots.TryPop(out slot))
            {
                slot = nextBindlessSlot++;
                if ((uint)slot >= BindlessTextureCapacity)
                    throw new Exception("Bindless texture capacity exceeded");
            }
            bindlessSlots[tex] = slot;
            pendingBindlessWrites.Add(tex);
        }
        return slot;
    }

    /// <summary>Assigns (or returns the existing) bindless sampler array index for a
    /// given filtering/wrapping/mips combination. The descriptor write is deferred to
    /// <see cref="FlushBindlessTextures"/>.</summary>
    private int GetBindlessSamplerSlot(FilteringMode filtering, WrapMode wrapping, bool mips)
    {
        var key = (filtering, wrapping, mips);
        if (bindlessSamplerSlots.TryGetValue(key, out var slot))
            return slot;
        slot = nextBindlessSamplerSlot++;
        if ((uint)slot >= BindlessSamplerCapacity)
            throw new Exception("Bindless sampler capacity exceeded");
        bindlessSamplerSlots[key] = slot;
        pendingBindlessSamplerWrites.Add((slot, SamplerCache.Get(filtering, wrapping, mips)));
        return slot;
    }

    /// <summary>Writes descriptors for textures/samplers registered since the last flush.
    /// Must be called outside a rendering pass (it may transition image layouts).</summary>
    internal void FlushBindlessTextures(CommandBuffer cmd)
    {
        AssertInFrame();
        foreach (var tex in pendingBindlessWrites)
        {
            if (tex.CurrentLayout != ImageLayout.ShaderReadOnlyOptimal)
                tex.TransitionTo(cmd, ImageLayout.ShaderReadOnlyOptimal);
            var imageInfo = new DescriptorImageInfo
            {
                ImageView = tex.View,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };
            var write = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = BindlessSet,
                DstBinding = 0,
                DstArrayElement = (uint)bindlessSlots[tex],
                DescriptorCount = 1,
                DescriptorType = DescriptorType.SampledImage,
                PImageInfo = &imageInfo,
            };
            ctx.vk.UpdateDescriptorSets(ctx.Device, 1, &write, 0, null);
        }
        pendingBindlessWrites.Clear();

        foreach (var (slot, sampler) in pendingBindlessSamplerWrites)
        {
            var samplerInfo = new DescriptorImageInfo
            {
                Sampler = sampler,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };
            var write = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = BindlessSet,
                DstBinding = 1,
                DstArrayElement = (uint)slot,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.Sampler,
                PImageInfo = &samplerInfo,
            };
            ctx.vk.UpdateDescriptorSets(ctx.Device, 1, &write, 0, null);
        }
        pendingBindlessSamplerWrites.Clear();

        // every slot was pre-pointed at the fallback in the constructor (and retired slots are
        // repointed there in ReleaseBindlessTexture), so the "partially bound" array never has
        // an uninitialized entry that MoltenVK's resident argument buffer could GPU-fault on.
    }

    private void CreateGameSet3Layout()
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[MaxGlobalBuffers];
        for (uint i = 0; i < MaxGlobalBuffers; i++)
            bindings[i] = new DescriptorSetLayoutBinding
            {
                Binding = i,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            };
        // update-after-bind: a global buffer (RewriteGlobalBufferDescriptor) can be rewritten
        // mid-session while GameSet3 is already bound across the frame's draws (it grows on
        // demand from VulkanGlobalBuffer.GrowSlot) - without this, that rewrite is a "descriptor
        // set updated without UPDATE_AFTER_BIND" hazard on whatever command buffer has it bound.
        var bindingFlags = stackalloc DescriptorBindingFlags[MaxGlobalBuffers];
        for (uint i = 0; i < MaxGlobalBuffers; i++)
            bindingFlags[i] = DescriptorBindingFlags.PartiallyBoundBit | DescriptorBindingFlags.UpdateAfterBindBit;
        var flagsInfo = new DescriptorSetLayoutBindingFlagsCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            BindingCount = (uint)MaxGlobalBuffers,
            PBindingFlags = bindingFlags,
        };
        var info = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            PNext = &flagsInfo,
            Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
            BindingCount = (uint)MaxGlobalBuffers,
            PBindings = bindings,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorSetLayout(ctx.Device, in info, null, out GameSet3Layout), "gameSet3 layout");

        var poolSizes = stackalloc DescriptorPoolSize[1]
        {
            new DescriptorPoolSize(DescriptorType.StorageBuffer, (uint)(FramesInFlight * MaxGlobalBuffers)),
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.UpdateAfterBindBit,
            MaxSets = FramesInFlight,
            PoolSizeCount = 1,
            PPoolSizes = poolSizes,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorPool(ctx.Device, in poolInfo, null, out gameSet3Pool), "gameSet3 pool");

        // dummy 4-byte buffer fills unregistered slots so all bindings remain valid
        ulong dummySize = 4;
        var bufInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = dummySize,
            Usage = BufferUsageFlags.StorageBufferBit,
            SharingMode = SharingMode.Exclusive,
        };
        var allocInfo = new VmaAllocationCreateInfo { usage = VmaMemoryUsage.GpuOnly };
        VkBuffer dummyBuf;
        VmaAllocation dummyMem;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &bufInfo, &allocInfo, &dummyBuf, &dummyMem, null), "gameSet3 dummy buffer");
        dummyGlobalBuffer = dummyBuf;
        dummyGlobalMemory = dummyMem;
    }

    /// <summary>Rewrites the set-3 descriptor for <paramref name="buf"/> in frame slot <paramref name="frameSlot"/>
    /// after the buffer has grown. Called from <see cref="VulkanGlobalBuffer{T}"/>.</summary>
    internal void RewriteGlobalBufferDescriptor(VulkanGlobalBufferBase buf, int frameSlot)
    {
        buf.FillDescriptor(frameSlot, out var bufInfo);
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = frames[frameSlot].GameSet3,
            DstBinding = buf.Binding,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = (DescriptorBufferInfo*)Unsafe.AsPointer(ref bufInfo),
        };
        ctx.vk.UpdateDescriptorSets(ctx.Device, 1, &write, 0, null);
    }

    /// <summary>Rewrites <paramref name="buf"/>'s descriptor in every frame slot (after registration
    /// or a grow). Called from <see cref="VulkanStaticGlobalBuffer{T}"/> and registration below.</summary>
    internal void RewriteGlobalBufferDescriptorAllSlots(VulkanGlobalBufferBase buf)
    {
        for (int slot = 0; slot < FramesInFlight; slot++)
            RewriteGlobalBufferDescriptor(buf, slot);
    }

    private void RegisterGlobalBuffer(uint binding, VulkanGlobalBufferBase buf)
    {
        if (binding >= MaxGlobalBuffers)
            throw new InvalidOperationException($"Global buffer binding {binding} exceeds the {MaxGlobalBuffers}-binding set-3 layout.");
        if (globalBuffers[binding] != null)
            throw new InvalidOperationException($"Global buffer binding {binding} is already registered.");
        globalBuffers[binding] = buf;
        // frames are already allocated at this point
        RewriteGlobalBufferDescriptorAllSlots(buf);
    }

    public IGlobalBuffer<T> CreateGlobalBuffer<T>(uint binding, int initialCapacity = 64) where T : unmanaged
    {
        var buf = new VulkanGlobalBuffer<T>(this, ctx, binding, initialCapacity, FramesInFlight);
        RegisterGlobalBuffer(binding, buf);
        return buf;
    }

    public IStaticGlobalBuffer<T> CreateStaticGlobalBuffer<T>(uint binding, int slotElementCount, int initialSlots = 8) where T : unmanaged
    {
        var buf = new VulkanStaticGlobalBuffer<T>(this, ctx, binding, slotElementCount, initialSlots);
        RegisterGlobalBuffer(binding, buf);
        return buf;
    }

    private Frame CreateFrame()
    {
        var frame = new Frame();
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = ctx.QueueFamily,
            Flags = CommandPoolCreateFlags.TransientBit,
        };
        VulkanContext.Check(ctx.vk.CreateCommandPool(ctx.Device, in poolInfo, null, out frame.Pool), "frame command pool");
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = frame.Pool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        VulkanContext.Check(ctx.vk.AllocateCommandBuffers(ctx.Device, in allocInfo, out frame.Cmd), "frame command buffer");
        var semInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        VulkanContext.Check(ctx.vk.CreateSemaphore(ctx.Device, in semInfo, null, out frame.ImageAvailable), "imageAvailable");
        var fenceInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
        VulkanContext.Check(ctx.vk.CreateFence(ctx.Device, in fenceInfo, null, out frame.Fence), "frame fence");

        // the frame ring: a single host-visible buffer for everything transient with
        // dynamic-offset semantics (SceneData/ObjectData snapshots, material UBO data)
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = RingSize,
            Usage = BufferUsageFlags.UniformBufferBit,
            SharingMode = SharingMode.Exclusive,
        };
        var ringAllocInfo = new VmaAllocationCreateInfo
        {
            flags = VmaAllocationCreateFlagBits.VmaAllocationCreateMappedBit,
            usage = VmaMemoryUsage.CpuToGpu,
            requiredFlags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        };
        VkBuffer ringBuffer;
        VmaAllocation ringAllocation;
        VmaAllocationInfo ringAllocationInfo;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &bufferInfo, &ringAllocInfo, &ringBuffer, &ringAllocation, &ringAllocationInfo), "vma frame ring");
        frame.Ring = ringBuffer;
        frame.RingMemory = ringAllocation;
        frame.RingMapped = (byte*)ringAllocationInfo.pMappedData;

        if (ProfileGpu)
        {
            var qpInfo = new QueryPoolCreateInfo
            {
                SType = StructureType.QueryPoolCreateInfo,
                QueryType = QueryType.Timestamp,
                QueryCount = 2,
            };
            VulkanContext.Check(ctx.vk.CreateQueryPool(ctx.Device, in qpInfo, null, out frame.TimestampPool), "timestamp pool");
        }

        var set0Layout = Set0Layout;
        var setAlloc = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = set0Pool,
            DescriptorSetCount = 1,
            PSetLayouts = &set0Layout,
        };
        VulkanContext.Check(ctx.vk.AllocateDescriptorSets(ctx.Device, in setAlloc, out frame.Set0), "set0 alloc");

        var bufferInfos = stackalloc DescriptorBufferInfo[2];
        var writes = stackalloc WriteDescriptorSet[2];
        for (uint i = 0; i < 2; i++)
        {
            bufferInfos[i] = new DescriptorBufferInfo(frame.Ring, 0, Set0Range);
            writes[i] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.Set0,
                DstBinding = i,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                PBufferInfo = &bufferInfos[i],
            };
        }
        ctx.vk.UpdateDescriptorSets(ctx.Device, 2, writes, 0, null);

        // Forward+ storage buffers: fixed-size, host-visible/coherent (like the ring), so the
        // descriptors written here stay valid for the frame's lifetime - no rotation to chase.
        // LightGrid/LightIndexList and DecalGrid/DecalIndexList are sized FORWARD_PLUS_GRID_SLOTS
        // times larger than one view's worth of tiles needs: slot 0 holds the main view's culling
        // result, slot 1 the editor scene view's (see SceneData.gridSet / theengine.cginc), so the
        // two views' results can coexist within the same frame without adding extra SSBO bindings
        // (this device's maxPerStageDescriptorStorageBuffers limit has no headroom for that).
        ulong lightBufferSize = (ulong)Constants.FORWARD_PLUS_MAX_LIGHTS * (ulong)Unsafe.SizeOf<GpuPointLight>();
        ulong lightGridSize = (ulong)Constants.FORWARD_PLUS_GRID_SLOTS * Constants.FORWARD_PLUS_MAX_TILES_X * (ulong)Constants.FORWARD_PLUS_MAX_TILES_Y * 2 * sizeof(uint);
        ulong lightIndexSize = (ulong)Constants.FORWARD_PLUS_GRID_SLOTS * Constants.FORWARD_PLUS_MAX_TILES_X * (ulong)Constants.FORWARD_PLUS_MAX_TILES_Y * (ulong)Constants.FORWARD_PLUS_MAX_LIGHTS_PER_TILE * sizeof(uint);

        CreateStorageBuffer(lightBufferSize, out frame.LightBuffer, out frame.LightMemory, out var lightMapped);
        frame.LightMapped = (byte*)lightMapped;
        CreateStorageBuffer(lightGridSize, out frame.LightGridBuffer, out frame.LightGridMemory, out _);
        CreateStorageBuffer(lightIndexSize, out frame.LightIndexBuffer, out frame.LightIndexMemory, out _);

        ulong decalBufferSize = (ulong)Constants.FORWARD_PLUS_MAX_DECALS * (ulong)Unsafe.SizeOf<GpuDecal>();
        ulong decalGridSize = (ulong)Constants.FORWARD_PLUS_GRID_SLOTS * Constants.FORWARD_PLUS_MAX_TILES_X * (ulong)Constants.FORWARD_PLUS_MAX_TILES_Y * 2 * sizeof(uint);
        ulong decalIndexSize = (ulong)Constants.FORWARD_PLUS_GRID_SLOTS * Constants.FORWARD_PLUS_MAX_TILES_X * (ulong)Constants.FORWARD_PLUS_MAX_TILES_Y * (ulong)Constants.FORWARD_PLUS_MAX_DECALS_PER_TILE * sizeof(uint);

        CreateStorageBuffer(decalBufferSize, out frame.DecalBuffer, out frame.DecalMemory, out var decalMapped);
        frame.DecalMapped = (byte*)decalMapped;
        CreateStorageBuffer(decalGridSize, out frame.DecalGridBuffer, out frame.DecalGridMemory, out _);
        CreateStorageBuffer(decalIndexSize, out frame.DecalIndexBuffer, out frame.DecalIndexMemory, out _);

        var lightBufferInfos = stackalloc DescriptorBufferInfo[6]
        {
            new DescriptorBufferInfo(frame.LightBuffer, 0, lightBufferSize),
            new DescriptorBufferInfo(frame.LightGridBuffer, 0, lightGridSize),
            new DescriptorBufferInfo(frame.LightIndexBuffer, 0, lightIndexSize),
            new DescriptorBufferInfo(frame.DecalBuffer, 0, decalBufferSize),
            new DescriptorBufferInfo(frame.DecalGridBuffer, 0, decalGridSize),
            new DescriptorBufferInfo(frame.DecalIndexBuffer, 0, decalIndexSize),
        };
        var lightWrites = stackalloc WriteDescriptorSet[6];
        var lightBindings = stackalloc uint[6]
        {
            Constants.LIGHT_BUFFER_BINDING, Constants.LIGHT_GRID_BINDING, Constants.LIGHT_INDEX_LIST_BINDING,
            Constants.DECAL_BUFFER_BINDING, Constants.DECAL_GRID_BINDING, Constants.DECAL_INDEX_LIST_BINDING,
        };
        for (uint i = 0; i < 6; i++)
            lightWrites[i] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.Set0,
                DstBinding = lightBindings[i],
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                PBufferInfo = &lightBufferInfos[i],
            };
        ctx.vk.UpdateDescriptorSets(ctx.Device, 6, lightWrites, 0, null);

        // set 3: allocate and fill all bindings with the dummy buffer; CreateGlobalBuffer
        // overwrites the relevant binding slots for each registered global buffer.
        var gameSet3LayoutLocal = GameSet3Layout;
        var gameSet3Alloc = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = gameSet3Pool,
            DescriptorSetCount = 1,
            PSetLayouts = &gameSet3LayoutLocal,
        };
        VulkanContext.Check(ctx.vk.AllocateDescriptorSets(ctx.Device, in gameSet3Alloc, out frame.GameSet3), "gameSet3 alloc");

        var dummyInfo = new DescriptorBufferInfo(dummyGlobalBuffer, 0, 4);
        var gameSet3Writes = stackalloc WriteDescriptorSet[MaxGlobalBuffers];
        for (uint i = 0; i < MaxGlobalBuffers; i++)
            gameSet3Writes[i] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.GameSet3,
                DstBinding = i,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                PBufferInfo = &dummyInfo,
            };
        ctx.vk.UpdateDescriptorSets(ctx.Device, (uint)MaxGlobalBuffers, gameSet3Writes, 0, null);

        return frame;
    }

    /// <summary>Allocates a fixed-size host-visible/coherent buffer with StorageBufferBit usage.</summary>
    private void CreateStorageBuffer(ulong size, out VkBuffer buffer, out VmaAllocation memory, out void* mapped)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = BufferUsageFlags.StorageBufferBit,
            SharingMode = SharingMode.Exclusive,
        };
        var allocInfo = new VmaAllocationCreateInfo
        {
            flags = VmaAllocationCreateFlagBits.VmaAllocationCreateMappedBit,
            usage = VmaMemoryUsage.CpuToGpu,
            requiredFlags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        };
        VkBuffer createdBuffer;
        VmaAllocation allocation;
        VmaAllocationInfo allocationInfo;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &bufferInfo, &allocInfo, &createdBuffer, &allocation, &allocationInfo), "vma storage buffer");
        buffer = createdBuffer;
        memory = allocation;
        mapped = allocationInfo.pMappedData;
    }

    /// <summary>Copies point lights into the current frame's Light SSBO (host-visible, persistently mapped).</summary>
    internal void UploadLights(ReadOnlySpan<GpuPointLight> lights)
    {
        AssertInFrame();
        var frame = frames[frameIndex];
        int count = Math.Min(lights.Length, Constants.FORWARD_PLUS_MAX_LIGHTS);
        var dst = new Span<GpuPointLight>(frame.LightMapped, count);
        lights.Slice(0, count).CopyTo(dst);
    }

    /// <summary>Copies decals into the current frame's Decal SSBO (host-visible, persistently mapped).</summary>
    internal void UploadDecals(ReadOnlySpan<GpuDecal> decals)
    {
        AssertInFrame();
        var frame = frames[frameIndex];
        int count = Math.Min(decals.Length, Constants.FORWARD_PLUS_MAX_DECALS);
        var dst = new Span<GpuDecal>(frame.DecalMapped, count);
        decals.Slice(0, count).CopyTo(dst);
    }

    public ICommandList CreateExecutor(TextureManager textureManager)
        => executor = new VulkanCommandList(this, ctx, textureManager);

    public void BeginFrame()
    {
        InFrame = true;
        var frame = frames[frameIndex];
        // the fence-wait timing is just a CPU stopwatch around an existing blocking call, so it is
        // always measured (cheap) - it's the clearest signal of whether the frame is CPU/GPU-bound
        // vs idle-waiting on the previous frame. The GPU timestamp readback below stays gated.
        var swProfile = CheapStopWatch.StartNew();
        if (frame.Submitted)
        {
            var fence = frame.Fence;
            VulkanContext.Check(ctx.vk.WaitForFences(ctx.Device, 1, in fence, true, ulong.MaxValue), "wait frame fence");
            LastFenceWaitMs = (float)swProfile.Elapsed.TotalMilliseconds;
            ctx.vk.ResetFences(ctx.Device, 1, in fence);
            ctx.CompletedSubmission = Math.Max(ctx.CompletedSubmission, frame.SubmissionId);
            frame.Submitted = false;

            // GPU finished this frame slot's previous submission: read its timestamp pair
            if (ProfileGpu && frame.TimestampsWritten)
            {
                var swQ = CheapStopWatch.StartNew();
                ulong* ts = stackalloc ulong[2];
                if (ctx.vk.GetQueryPoolResults(ctx.Device, frame.TimestampPool, 0, 2, 2 * sizeof(ulong), ts, sizeof(ulong),
                        QueryResultFlags.Result64Bit) == Result.Success)
                    LastGpuMs = (ts[1] - ts[0]) * ctx.DeviceProperties.Limits.TimestampPeriod / 1_000_000f;
                LastQueryReadbackMs = (float)swQ.Elapsed.TotalMilliseconds;
            }
        }
        else
        {
            LastFenceWaitMs = 0; // nothing in flight in this slot yet - no wait
        }
        var swDestroy = ProfileGpu ? CheapStopWatch.StartNew() : default(CheapStopWatch);
        ctx.ProcessPendingDestroys();
        if (ProfileGpu) LastDestroyMs = (float)swDestroy!.Elapsed.TotalMilliseconds;

        // Hand finished async uploads (textures whose GPU copy completed) back to the game on the
        // render thread, before any of this frame's rendering can reference them.
        UploadQueue.ProcessCompletions();

        foreach (var gb in globalBuffers)
            gb?.NotifyFrameSlot(frameIndex);

        ctx.vk.ResetCommandPool(ctx.Device, frame.Pool, 0);
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        VulkanContext.Check(ctx.vk.BeginCommandBuffer(frame.Cmd, in beginInfo), "begin frame cb");

        if (ProfileGpu)
        {
            ctx.vk.CmdResetQueryPool(frame.Cmd, frame.TimestampPool, 0, 2);
            ctx.vk.CmdWriteTimestamp(frame.Cmd, PipelineStageFlags.TopOfPipeBit, frame.TimestampPool, 0);
            frame.TimestampsWritten = true;
        }

        foreach (var pool in frame.DescriptorPools)
            ctx.vk.ResetDescriptorPool(ctx.Device, pool, 0);
        frame.PoolIndex = 0;
        frame.RingOffset = 0;

        var swThrottle = CheapStopWatch.StartNew();
        ThrottleLatency();
        LastPresentWaitMs = (float)swThrottle.Elapsed.TotalMilliseconds;

        var swAcquire = CheapStopWatch.StartNew();
        AcquireImage(frame);
        LastAcquireMs = (float)swAcquire.Elapsed.TotalMilliseconds;
        executor?.OnBackendFrameBegin();
    }

    // ---- low-latency vsync (VK_KHR_present_wait) ----
    // With FIFO and N swapchain images the driver buffers up to N-1 finished frames; the spinning
    // render loop keeps that queue full, so input sampled this frame shows up ~2 vsyncs later.
    // Waiting until the PREVIOUS present actually reached the display (maxPending=0) caps the queue:
    // input is sampled ~one refresh before it can appear. The cost: once the previous frame is on
    // glass its GPU work is done, so nothing overlaps - the whole frame (CPU+GPU) must fit in one
    // refresh interval or vblanks get missed that the deeper queue would still have made. Hence the
    // throttle self-monitors: consecutive waits return ~one refresh period apart when every vblank
    // is hit; intervals well above the rolling estimate are missed vblanks, and repeated misses back
    // off to maxPending=1 (the old pipelined pacing) for a cooldown before probing again.
    private long lastPresentWaitReturn;
    private double refreshPeriodEstimateMs;
    private int presentWaitMisses;
    private int presentWaitWindow;
    private int presentWaitRelaxedCooldown;
    private int consecutiveHighIntervals;

    private void ThrottleLatency()
    {
        if (!presentTarget.CanThrottlePresentQueue)
        {
            lastPresentWaitReturn = 0;
            return;
        }
        bool aggressive = presentWaitRelaxedCooldown <= 0;
        if (!aggressive)
            presentWaitRelaxedCooldown--;
        presentTarget.ThrottlePresentQueue(aggressive ? 0 : 1);

        // pacing telemetry: in either mode a saturated present queue completes one wait per vblank,
        // so the interval between consecutive returns estimates the refresh period
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        long prev = lastPresentWaitReturn;
        lastPresentWaitReturn = now;
        if (prev == 0)
            return;
        double intervalMs = (now - prev) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        if (intervalMs is <= 2 or >= 100)
            return; // hidden-window spins, loading hitches, debugger pauses - not pacing samples
        if (refreshPeriodEstimateMs == 0)
        {
            refreshPeriodEstimateMs = intervalMs;
            return;
        }
        bool missedVblank = intervalMs >= refreshPeriodEstimateMs * 1.4;
        if (!missedVblank)
        {
            refreshPeriodEstimateMs = refreshPeriodEstimateMs * 0.9 + intervalMs * 0.1;
            consecutiveHighIntervals = 0;
        }
        else if (++consecutiveHighIntervals >= 120)
        {
            // seconds of uniformly long intervals isn't frame misses - the refresh rate itself
            // changed (window moved to another monitor); drop the estimate and re-learn
            refreshPeriodEstimateMs = 0;
            consecutiveHighIntervals = 0;
            presentWaitMisses = 0;
            presentWaitWindow = 0;
            return;
        }
        if (!aggressive)
            return;
        if (missedVblank)
            presentWaitMisses++;
        if (++presentWaitWindow >= 60)
        {
            if (presentWaitMisses >= 6)
                presentWaitRelaxedCooldown = 240; // ~4s of pipelined pacing before probing again
            presentWaitMisses = 0;
            presentWaitWindow = 0;
        }
    }

    public bool InFrame { get; set; }

    /// <summary>Debug guard: throws if a frame-only operation (anything that records into the current
    /// frame's command buffer or sub-allocates from its ring/descriptor pools) runs while no frame is
    /// open. Catches the caller in the stack trace instead of letting it corrupt frame state and
    /// GPU-fault later. The <see cref="CallerMemberName"/> names the offending method.</summary>
    internal void AssertInFrame([CallerMemberName] string caller = "")
    {
        if (!InFrame)
            throw new InvalidOperationException($"{caller} was called while Backend.InFrame == false (outside BeginFrame/EndFrame)");
    }

    private void AcquireImage(Frame frame)
    {
        HasImage = false;
        uint width = (uint)Math.Max(1, windowHost.WindowWidth);
        uint height = (uint)Math.Max(1, windowHost.WindowHeight);
        presentTarget.EnsureSize(width, height);
        if (presentTarget.Acquire(frame.ImageAvailable, out var imageIndex))
        {
            CurrentImageIndex = imageIndex;
            HasImage = true;
        }
    }

    public void EndFrame()
    {
        AssertInFrame();
        var frame = frames[frameIndex];

        if (HasImage)
            TransitionSwapchainImage(frame.Cmd, presentTarget.FinalLayout);

        if (ProfileGpu)
            ctx.vk.CmdWriteTimestamp(frame.Cmd, PipelineStageFlags.BottomOfPipeBit, frame.TimestampPool, 1);

        VulkanContext.Check(ctx.vk.EndCommandBuffer(frame.Cmd), "end frame cb");

        // wait/signal semaphores depend on the present target: the KHR path waits on the acquire
        // semaphore and signals the per-image renderFinished; the external (compositor interop) path
        // waits/signals nothing here - the interop wrapper bracketing the frame owns synchronization,
        // and submission order on the shared queue provides the rest. A zero handle means "none".
        var waitSem = presentTarget.WaitSemaphore;
        var signalSem = presentTarget.SignalSemaphore;
        bool hasWait = HasImage && waitSem.Handle != 0;
        bool hasSignal = HasImage && signalSem.Handle != 0;

        var cmdInfo = new CommandBufferSubmitInfo { SType = StructureType.CommandBufferSubmitInfo, CommandBuffer = frame.Cmd };
        // Wait on the upload timeline at the already-completed value: textures only become reachable
        // after their upload completed (ProcessCompletions), so this adds the cross-queue memory
        // dependency that makes their writes visible to the graphics queue without ever stalling.
        ulong uploadValue = UploadQueue.CompletedValue;
        var waits = stackalloc SemaphoreSubmitInfo[2];
        uint waitCount = 0;
        if (hasWait)
            waits[waitCount++] = new SemaphoreSubmitInfo
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = waitSem,
                StageMask = PipelineStageFlags2.AllCommandsBit,
            };
        if (uploadValue > 0)
            waits[waitCount++] = new SemaphoreSubmitInfo
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = UploadQueue.TimelineSemaphore,
                Value = uploadValue,
                StageMask = PipelineStageFlags2.AllCommandsBit,
            };
        var signalInfo = new SemaphoreSubmitInfo
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = signalSem,
            StageMask = PipelineStageFlags2.AllCommandsBit,
        };
        var submit = new SubmitInfo2
        {
            SType = StructureType.SubmitInfo2,
            CommandBufferInfoCount = 1,
            PCommandBufferInfos = &cmdInfo,
            WaitSemaphoreInfoCount = waitCount,
            PWaitSemaphoreInfos = waits,
            SignalSemaphoreInfoCount = hasSignal ? 1u : 0u,
            PSignalSemaphoreInfos = &signalInfo,
        };
        // With a dedicated transfer queue the upload thread submits to a different VkQueue; with the
        // single-queue fallback both threads target ctx.Queue and must serialize.
        if (ctx.HasDedicatedTransferQueue)
            VulkanContext.Check(ctx.vk.QueueSubmit2(ctx.Queue, 1, in submit, frame.Fence), "frame submit");
        else
            lock (ctx.QueueSubmitLock)
                VulkanContext.Check(ctx.vk.QueueSubmit2(ctx.Queue, 1, in submit, frame.Fence), "frame submit");
        frame.SubmissionId = ctx.CurrentSubmission;
        ctx.CurrentSubmission++;
        frame.Submitted = true;

        if (HasImage)
            presentTarget.Present(CurrentImageIndex);

        frameIndex = (frameIndex + 1) % FramesInFlight;
        // cleared only now (not at EndFrame entry): the submit/present teardown above is still part of
        // the frame, so frame-only guards must stay satisfied through it.
        InFrame = false;
    }

    internal void TransitionSwapchainImage(CommandBuffer cmd, ImageLayout newLayout)
    {
        AssertInFrame();
        var current = presentTarget.GetLayout(CurrentImageIndex);
        if (current == newLayout)
            return;
        var barrier = new ImageMemoryBarrier2
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = AccessFlags2.MemoryWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryWriteBit | AccessFlags2.MemoryReadBit,
            OldLayout = current,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = presentTarget.GetImage(CurrentImageIndex),
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
        };
        var dep = new DependencyInfo
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier,
        };
        ctx.vk.CmdPipelineBarrier2(cmd, in dep);
        presentTarget.SetLayout(CurrentImageIndex, newLayout);
    }

    /// <summary>Suballocates from the frame ring; the returned slice is valid for the current frame only.</summary>
    internal (VkBuffer buffer, uint offset, IntPtr ptr) RingAlloc(int size, uint alignment)
    {
        AssertInFrame();
        var frame = frames[frameIndex];
        long offset = (frame.RingOffset + alignment - 1) / alignment * alignment;
        // Set0Range bytes of tail slack keep dynamic offset + range within the buffer
        if (offset + size + Set0Range > RingSize)
            throw new Exception($"frame ring exhausted ({offset + size} > {RingSize})");
        frame.RingOffset = offset + size;
        return (frame.Ring, (uint)offset, (IntPtr)(frame.RingMapped + offset));
    }

    internal DescriptorSet AllocateSet1(DescriptorSetLayout layout)
    {
        AssertInFrame();
        var frame = frames[frameIndex];
        while (true)
        {
            if (frame.PoolIndex == frame.DescriptorPools.Count)
                frame.DescriptorPools.Add(CreateDescriptorPool());
            var pool = frame.DescriptorPools[frame.PoolIndex];
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = pool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout,
            };
            var result = ctx.vk.AllocateDescriptorSets(ctx.Device, in allocInfo, out var set);
            if (result == Result.Success)
                return set;
            if (result is Result.ErrorOutOfPoolMemory or Result.ErrorFragmentedPool)
            {
                frame.PoolIndex++;
                continue;
            }
            VulkanContext.Check(result, "set1 alloc");
        }
    }

    private DescriptorPool CreateDescriptorPool()
    {
        var sizes = stackalloc DescriptorPoolSize[4]
        {
            new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 8192),
            new DescriptorPoolSize(DescriptorType.UniformTexelBuffer, 4096),
            new DescriptorPoolSize(DescriptorType.UniformBuffer, 4096),
            new DescriptorPoolSize(DescriptorType.StorageBuffer, 4096),
        };
        var info = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 4096,
            PoolSizeCount = 4,
            PPoolSizes = sizes,
        };
        VulkanContext.Check(ctx.vk.CreateDescriptorPool(ctx.Device, in info, null, out var pool), "descriptor pool");
        return pool;
    }

    // ---------------------------------------------------------------- buffers

    public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size) where T : unmanaged
        => new VulkanBuffer<T>(this, ctx, bufferType, size * Unsafe.SizeOf<T>());

    public INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data) where T : unmanaged
    {
        var buffer = new VulkanBuffer<T>(this, ctx, bufferType, Math.Max(4, data.Length * Unsafe.SizeOf<T>()));
        buffer.UpdateBuffer(data);
        return buffer;
    }

    // ---------------------------------------------------------------- textures

    private static uint MipCount(int width, int height)
        => 1 + (uint)Math.Floor(Math.Log2(Math.Max(width, height)));

    public INativeTexture CreateTexture(int width, int height, Rgba32[][] pixels, bool generateMips)
    {
        fixed (Rgba32* level0 = pixels[0])
        {
            if (pixels.Length > 1)
            {
                // pre-built mip chain
                var texture = NewSampledTexture(width, height, VkFormat.R8G8B8A8Unorm, (uint)pixels.Length);
                for (int level = 0; level < pixels.Length; level++)
                {
                    int mipWidth = Math.Max(1, width >> level);
                    int mipHeight = Math.Max(1, height >> level);
                    fixed (Rgba32* pLevel = pixels[level])
                        UploadLevel(texture, level, mipWidth, mipHeight, pLevel, mipWidth * mipHeight * 4);
                }
                FinalizeUpload(texture);
                return texture;
            }
            return CreateTexture(width, height, level0, generateMips);
        }
    }

    public INativeTexture CreateTexture(int width, int height, Rgba32* pixels, bool generateMips)
    {
        var mips = generateMips ? MipCount(width, height) : 1;
        var texture = NewSampledTexture(width, height, VkFormat.R8G8B8A8Unorm, mips);
        UploadLevel(texture, 0, width, height, pixels, width * height * 4);
        if (mips > 1)
            GenerateMips(texture);
        FinalizeUpload(texture);
        return texture;
    }

    public INativeTexture CreateTexture(int width, int height, Vector4[] pixels)
    {
        var texture = NewSampledTexture(width, height, VkFormat.R32G32B32A32Sfloat, 1);
        fixed (Vector4* p = pixels)
            UploadLevel(texture, 0, width, height, p, width * height * 16);
        FinalizeUpload(texture);
        return texture;
    }

    public INativeTexture CreateTexture(int width, int height, float[] pixels)
    {
        var texture = NewSampledTexture(width, height, VkFormat.R32Sfloat, 1);
        fixed (float* p = pixels)
            UploadLevel(texture, 0, width, height, p, width * height * 4);
        FinalizeUpload(texture);
        return texture;
    }

    public INativeTexture CreateTexture(int width, int height, uint[]? pixels, TextureFormat format)
    {
        VulkanTexture texture;
        switch (format)
        {
            case TextureFormat.R8G8B8A8:
                texture = new VulkanTexture(ctx, width, height, VkFormat.R8G8B8A8Unorm, 1,
                    ImageUsageFlags.SampledBit | ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                    ImageAspectFlags.ColorBit);
                break;
            case TextureFormat.R32ui:
                texture = new VulkanTexture(ctx, width, height, VkFormat.R32Uint, 1,
                    ImageUsageFlags.SampledBit | ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                    ImageAspectFlags.ColorBit);
                break;
            case TextureFormat.R32f:
                texture = new VulkanTexture(ctx, width, height, VkFormat.R32Sfloat, 1,
                    ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                    ImageAspectFlags.ColorBit);
                break;
            case TextureFormat.DepthComponent:
                texture = new VulkanTexture(ctx, width, height, VkFormat.D32Sfloat, 1,
                    ImageUsageFlags.SampledBit | ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                    ImageAspectFlags.DepthBit);
                ClearTexture(texture);
                return texture;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        if (pixels != null)
        {
            fixed (uint* p = pixels)
                UploadLevel(texture, 0, width, height, p, width * height * 4);
            FinalizeUpload(texture);
        }
        else
            ClearTexture(texture);
        return texture;
    }

    public INativeTexture CreateTextureArray(int width, int height, Rgba32[][][] pixels)
    {
        // pixels[layer][mip][texel], every layer carries the same full mip chain
        int layers = pixels.Length;
        int mips = pixels[0].Length;
        var texture = new VulkanTexture(ctx, width, height, VkFormat.R8G8B8A8Unorm, (uint)mips,
            ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit,
            ImageAspectFlags.ColorBit, (uint)layers);

        long total = 0;
        for (int m = 0; m < mips; m++)
            total += (long)Math.Max(1, width >> m) * Math.Max(1, height >> m) * 4 * layers;
        var (staging, stagingMemory, mapped) = CreateStaging((int)total);
        try
        {
            var regions = new BufferImageCopy[mips * layers];
            long offset = 0;
            int regionIndex = 0;
            for (int m = 0; m < mips; m++)
            {
                int mipWidth = Math.Max(1, width >> m);
                int mipHeight = Math.Max(1, height >> m);
                int mipBytes = mipWidth * mipHeight * 4;
                for (int l = 0; l < layers; l++)
                {
                    fixed (Rgba32* src = pixels[l][m])
                        Unsafe.CopyBlockUnaligned((byte*)mapped + offset, src, (uint)mipBytes);
                    regions[regionIndex++] = new BufferImageCopy
                    {
                        BufferOffset = (ulong)offset,
                        ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, (uint)m, (uint)l, 1),
                        ImageExtent = new Extent3D((uint)mipWidth, (uint)mipHeight, 1),
                    };
                    offset += mipBytes;
                }
            }
            ctx.OneShot(cmd =>
            {
                texture.TransitionTo(cmd, ImageLayout.TransferDstOptimal);
                fixed (BufferImageCopy* pRegions = regions)
                    ctx.vk.CmdCopyBufferToImage(cmd, staging, texture.Image, ImageLayout.TransferDstOptimal, (uint)regions.Length, pRegions);
                texture.TransitionTo(cmd, ImageLayout.ShaderReadOnlyOptimal);
            });
        }
        finally
        {
            Vma.DestroyBuffer(ctx.Allocator, staging, stagingMemory);
        }
        return texture;
    }

    public System.Threading.Tasks.ValueTask<INativeTexture> CreateTextureAsync(int width, int height, Rgba32[][] mips, bool generateMips, FilteringMode filtering, WrapMode wrapping)
        => UploadQueue.UploadTexture(width, height, mips, generateMips, filtering, wrapping);

    private VulkanTexture NewSampledTexture(int width, int height, VkFormat format, uint mips)
        => new(ctx, width, height, format, mips,
            ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            ImageAspectFlags.ColorBit);

    private void UploadLevel(VulkanTexture texture, int level, int width, int height, void* data, int bytes)
    {
        var (staging, stagingMemory, mapped) = CreateStaging(bytes);
        try
        {
            Unsafe.CopyBlockUnaligned((void*)mapped, data, (uint)bytes);
            ctx.OneShot(cmd =>
            {
                texture.TransitionTo(cmd, ImageLayout.TransferDstOptimal);
                var region = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(texture.Aspect, (uint)level, 0, 1),
                    ImageExtent = new Extent3D((uint)width, (uint)height, 1),
                };
                ctx.vk.CmdCopyBufferToImage(cmd, staging, texture.Image, ImageLayout.TransferDstOptimal, 1, in region);
            });
        }
        finally
        {
            Vma.DestroyBuffer(ctx.Allocator, staging, stagingMemory);
        }
    }

    private void GenerateMips(VulkanTexture texture)
    {
        ctx.OneShot(cmd =>
        {
            // level 0 is TransferDst after the upload; walk the chain blitting i-1 -> i
            int mipWidth = texture.Width;
            int mipHeight = texture.Height;
            for (uint level = 1; level < texture.MipLevels; level++)
            {
                BarrierMip(cmd, texture, level - 1, ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal);
                int nextWidth = Math.Max(1, mipWidth / 2);
                int nextHeight = Math.Max(1, mipHeight / 2);
                var blit = new ImageBlit
                {
                    SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, level - 1, 0, 1),
                    DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, level, 0, 1),
                };
                blit.SrcOffsets[0] = new Offset3D(0, 0, 0);
                blit.SrcOffsets[1] = new Offset3D(mipWidth, mipHeight, 1);
                blit.DstOffsets[0] = new Offset3D(0, 0, 0);
                blit.DstOffsets[1] = new Offset3D(nextWidth, nextHeight, 1);
                ctx.vk.CmdBlitImage(cmd, texture.Image, ImageLayout.TransferSrcOptimal, texture.Image, ImageLayout.TransferDstOptimal, 1, in blit, Filter.Linear);
                BarrierMip(cmd, texture, level - 1, ImageLayout.TransferSrcOptimal, ImageLayout.TransferDstOptimal);
                mipWidth = nextWidth;
                mipHeight = nextHeight;
            }
        });
    }

    private void BarrierMip(CommandBuffer cmd, VulkanTexture texture, uint level, ImageLayout from, ImageLayout to)
    {
        var barrier = new ImageMemoryBarrier2
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = AccessFlags2.MemoryWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryWriteBit | AccessFlags2.MemoryReadBit,
            OldLayout = from,
            NewLayout = to,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = texture.Image,
            SubresourceRange = new ImageSubresourceRange(texture.Aspect, level, 1, 0, 1),
        };
        var dep = new DependencyInfo { SType = StructureType.DependencyInfo, ImageMemoryBarrierCount = 1, PImageMemoryBarriers = &barrier };
        ctx.vk.CmdPipelineBarrier2(cmd, in dep);
    }

    private void FinalizeUpload(VulkanTexture texture)
    {
        ctx.OneShot(cmd => texture.TransitionTo(cmd, ImageLayout.ShaderReadOnlyOptimal));
        // bindless is the default access model: a sampled texture gets its slot the moment it's
        // ready, so it's always reachable by index (and its descriptor is flushed before the next
        // pass). The texture is already in ShaderReadOnlyOptimal, so the deferred write won't
        // re-transition it.
        ReserveBindlessSlot(texture);
    }

    private void ClearTexture(VulkanTexture texture)
    {
        ctx.OneShot(cmd =>
        {
            texture.TransitionTo(cmd, ImageLayout.TransferDstOptimal);
            var range = new ImageSubresourceRange(texture.Aspect, 0, texture.MipLevels, 0, 1);
            if (texture.IsDepth)
            {
                var depthClear = new ClearDepthStencilValue(1, 0);
                ctx.vk.CmdClearDepthStencilImage(cmd, texture.Image, ImageLayout.TransferDstOptimal, in depthClear, 1, in range);
            }
            else
            {
                var clear = new ClearColorValue();
                ctx.vk.CmdClearColorImage(cmd, texture.Image, ImageLayout.TransferDstOptimal, in clear, 1, in range);
            }
            texture.TransitionTo(cmd, texture.IsDepth ? ImageLayout.DepthAttachmentOptimal : ImageLayout.ShaderReadOnlyOptimal);
        });
    }

    private (VkBuffer buffer, VmaAllocation memory, IntPtr mapped) CreateStaging(int bytes)
    {
        var info = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)bytes,
            Usage = BufferUsageFlags.TransferSrcBit | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive,
        };
        var allocInfo = new VmaAllocationCreateInfo
        {
            flags = VmaAllocationCreateFlagBits.VmaAllocationCreateMappedBit,
            usage = VmaMemoryUsage.CpuToGpu,
            requiredFlags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        };
        VkBuffer buffer;
        VmaAllocation memory;
        VmaAllocationInfo allocationInfo;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &info, &allocInfo, &buffer, &memory, &allocationInfo), "vma staging buffer");
        return (buffer, memory, (IntPtr)allocationInfo.pMappedData);
    }

    // ---------------------------------------------------------------- render textures

    public INativeTexture CreateRenderTexture(int width, int height, int colorAttachments = 1, INativeTexture? depthTexture = null)
    {
        var colors = new VulkanTexture[colorAttachments];
        for (int i = 0; i < colorAttachments; i++)
        {
            colors[i] = new VulkanTexture(ctx, width, height, i == 0 ? VkFormat.R8G8B8A8Unorm : VkFormat.R32Uint, 1,
                ImageUsageFlags.SampledBit | ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                ImageAspectFlags.ColorBit);
            colors[i].SetWrapping(WrapMode.ClampToEdge);
            ClearTexture(colors[i]);
        }
        VulkanTexture depth;
        bool ownsDepth = depthTexture == null;
        if (depthTexture == null)
        {
            depth = new VulkanTexture(ctx, width, height, VkFormat.D32Sfloat, 1,
                ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
                ImageAspectFlags.DepthBit);
            ClearTexture(depth);
        }
        else
            depth = (VulkanTexture)depthTexture;
        return new VulkanRenderTexture(colors, depth, ownsColors: true, ownsDepth: ownsDepth);
    }

    public INativeTexture CreateRenderTexture(INativeTexture colorAttachment, INativeTexture depthTexture, INativeTexture? colorAttachment1 = null)
    {
        var colors = colorAttachment1 != null
            ? new[] { (VulkanTexture)colorAttachment, (VulkanTexture)colorAttachment1 }
            : new[] { (VulkanTexture)colorAttachment };
        return new VulkanRenderTexture(colors, (VulkanTexture)depthTexture, ownsColors: false, ownsDepth: false);
    }

    // ---------------------------------------------------------------- misc

    public IShader LoadShader(string jsonPath, string[] includePaths)
        => new VulkanShader(ctx, Set0Layout, BindlessLayout, GameSet3Layout, jsonPath);

    public void ScreenshotRenderTexture(INativeTexture renderTexture, string fileName, int colorAttachmentIndex)
    {
        var color = renderTexture switch
        {
            VulkanRenderTexture rt => rt.Colors[colorAttachmentIndex],
            VulkanTexture tex => tex,
            _ => throw new ArgumentException("not a vulkan texture"),
        };
        // the previous frame may still be rendering into this texture
        ctx.vk.QueueWaitIdle(ctx.Queue);

        int bytes = color.Width * color.Height * 4;
        var (staging, stagingMemory, mapped) = CreateStaging(bytes);
        var pixels = new Rgba32[color.Width * color.Height];
        try
        {
            var previousLayout = color.CurrentLayout == ImageLayout.Undefined ? ImageLayout.ShaderReadOnlyOptimal : color.CurrentLayout;
            ctx.OneShot(cmd =>
            {
                color.TransitionTo(cmd, ImageLayout.TransferSrcOptimal);
                var region = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(color.Aspect, 0, 0, 1),
                    ImageExtent = new Extent3D((uint)color.Width, (uint)color.Height, 1),
                };
                ctx.vk.CmdCopyImageToBuffer(cmd, color.Image, ImageLayout.TransferSrcOptimal, staging, 1, in region);
                color.TransitionTo(cmd, previousLayout);
            });

            fixed (Rgba32* dst = pixels)
                Unsafe.CopyBlockUnaligned(dst, (void*)mapped, (uint)bytes);
        }
        finally
        {
            Vma.DestroyBuffer(ctx.Allocator, staging, stagingMemory);
        }

        using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(pixels, color.Width, color.Height);
        // native top-down memory: the texel rows are already in display order, no flip needed
        image.SaveAsPng(fileName);
    }

    // Deferred (stall-free) 1×1 picking readback: a tiny persistently-mapped staging buffer with one
    // 4-byte slot per (channel, frame-in-flight). Each call records this frame's copy into its slot
    // and returns the value the SAME slot got FramesInFlight frames ago - guaranteed complete,
    // because BeginFrame waited that frame's fence before reusing the slot (host-coherent memory, so
    // the fence wait also makes the write visible). Contrast with ReadPixelsSync's QueueWaitIdle.
    // Channels let independent per-frame readers (object-id pick, depth pick, ...) coexist.
    internal const int DeferredReadChannels = 4;
    private VkBuffer deferredPickBuffer;
    private VmaAllocation deferredPickMemory;
    private IntPtr deferredPickMapped;
    private readonly bool[] deferredPickSlotValid = new bool[DeferredReadChannels * FramesInFlight];

    internal int FrameInFlightIndex => frameIndex;

    internal uint? ReadPixelDeferred(CommandBuffer cmd, VulkanTexture color, int x, int y, int channel)
    {
        if ((uint)channel >= DeferredReadChannels)
            throw new ArgumentOutOfRangeException(nameof(channel));
        if (deferredPickBuffer.Handle == 0)
            (deferredPickBuffer, deferredPickMemory, deferredPickMapped) = CreateStaging(4 * DeferredReadChannels * FramesInFlight);

        int slot = channel * FramesInFlight + frameIndex;
        uint? completed = deferredPickSlotValid[slot]
            ? ((uint*)deferredPickMapped)[slot]
            : null;

        var previousLayout = color.CurrentLayout == ImageLayout.Undefined ? ImageLayout.ShaderReadOnlyOptimal : color.CurrentLayout;
        color.TransitionTo(cmd, ImageLayout.TransferSrcOptimal);
        var region = new BufferImageCopy
        {
            BufferOffset = (ulong)(4 * slot),
            ImageSubresource = new ImageSubresourceLayers(color.Aspect, 0, 0, 1),
            ImageOffset = new Offset3D(Math.Clamp(x, 0, color.Width - 1), Math.Clamp(y, 0, color.Height - 1), 0),
            ImageExtent = new Extent3D(1, 1, 1),
        };
        ctx.vk.CmdCopyImageToBuffer(cmd, color.Image, ImageLayout.TransferSrcOptimal, deferredPickBuffer, 1, in region);
        color.TransitionTo(cmd, previousLayout);
        deferredPickSlotValid[slot] = true;

        return completed;
    }

    /// <summary>Synchronous pixel readback for picking. The caller already waited for the queue.</summary>
    internal void ReadPixelsSync(VulkanTexture color, int x, int y, int width, int height, Span<uint> destination)
    {
        int bytes = width * height * 4;
        var (staging, stagingMemory, mapped) = CreateStaging(bytes);
        try
        {
            var previousLayout = color.CurrentLayout == ImageLayout.Undefined ? ImageLayout.ShaderReadOnlyOptimal : color.CurrentLayout;
            ctx.OneShot(cmd =>
            {
                color.TransitionTo(cmd, ImageLayout.TransferSrcOptimal);
                var region = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(color.Aspect, 0, 0, 1),
                    ImageOffset = new Offset3D(Math.Clamp(x, 0, color.Width - 1), Math.Clamp(y, 0, color.Height - 1), 0),
                    ImageExtent = new Extent3D((uint)Math.Min(width, color.Width), (uint)Math.Min(height, color.Height), 1),
                };
                ctx.vk.CmdCopyImageToBuffer(cmd, color.Image, ImageLayout.TransferSrcOptimal, staging, 1, in region);
                color.TransitionTo(cmd, previousLayout);
            });
            fixed (uint* dst = destination)
                Unsafe.CopyBlockUnaligned(dst, (void*)mapped, (uint)Math.Min(bytes, destination.Length * 4));
        }
        finally
        {
            Vma.DestroyBuffer(ctx.Allocator, staging, stagingMemory);
        }
    }

    public void CollectDisposedResources() => ctx.ProcessPendingDestroys();

    public void Dispose()
    {
        ctx.vk.DeviceWaitIdle(ctx.Device);
        UploadQueue.Dispose(); // stops the upload thread and frees its pool/semaphore/staging
        executor?.DisposeResources();
        DefaultStorageBuffer.Dispose();
        bindlessFallback.Dispose();
        TileCullShader.Dispose();
        ctx.FlushAllPendingDestroys();
        PipelineCache.Dispose();
        SamplerCache.Dispose();
        for (int i = 0; i < globalBuffers.Length; i++)
        {
            globalBuffers[i]?.Dispose();
            globalBuffers[i] = null;
        }
        Vma.DestroyBuffer(ctx.Allocator, dummyGlobalBuffer, dummyGlobalMemory);
        if (deferredPickBuffer.Handle != 0)
            Vma.DestroyBuffer(ctx.Allocator, deferredPickBuffer, deferredPickMemory);
        ctx.vk.DestroyDescriptorPool(ctx.Device, set0Pool, null);
        ctx.vk.DestroyDescriptorPool(ctx.Device, gameSet3Pool, null);
        ctx.vk.DestroyDescriptorPool(ctx.Device, bindlessPool, null);
        ctx.vk.DestroyDescriptorSetLayout(ctx.Device, GameSet3Layout, null);
        ctx.vk.DestroyDescriptorSetLayout(ctx.Device, BindlessLayout, null);
        ctx.vk.DestroyDescriptorSetLayout(ctx.Device, EmptyLayout, null);
        foreach (var frame in frames)
        {
            foreach (var pool in frame.DescriptorPools)
                ctx.vk.DestroyDescriptorPool(ctx.Device, pool, null);
            Vma.DestroyBuffer(ctx.Allocator, frame.Ring, frame.RingMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.LightBuffer, frame.LightMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.LightGridBuffer, frame.LightGridMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.LightIndexBuffer, frame.LightIndexMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.DecalBuffer, frame.DecalMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.DecalGridBuffer, frame.DecalGridMemory);
            Vma.DestroyBuffer(ctx.Allocator, frame.DecalIndexBuffer, frame.DecalIndexMemory);
            ctx.vk.DestroyFence(ctx.Device, frame.Fence, null);
            ctx.vk.DestroySemaphore(ctx.Device, frame.ImageAvailable, null);
            ctx.vk.DestroyCommandPool(ctx.Device, frame.Pool, null);
        }
        ctx.vk.DestroyPipelineLayout(ctx.Device, PipelineLayout0, null);
        ctx.vk.DestroyDescriptorSetLayout(ctx.Device, Set0Layout, null);
        presentTarget.Dispose();
        ctx.Dispose();
    }
}
