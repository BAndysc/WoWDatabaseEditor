using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using TheEngine.Resources;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using VkFormat = Silk.NET.Vulkan.Format;

namespace TheEngine.Vulkan;

/// <summary>
/// Async GPU upload service: a dedicated thread stages texture data and copies it on the transfer
/// queue, parallel to rendering and without the per-texture <c>vkQueueWaitIdle</c> stall of the old
/// <see cref="VulkanContext.OneShot"/> path.
///
/// Each copy signals an increasing value on one timeline semaphore. A texture is only handed back to
/// the game (<see cref="ProcessCompletions"/>) once its value completed, and the graphics submit waits
/// on the timeline (<see cref="CompletedValue"/>) so the cross-queue write is visible before first use.
/// </summary>
internal sealed unsafe class VulkanUploadQueue : IDisposable
{
    private sealed class Request
    {
        public int Width;
        public int Height;
        public Rgba32[][] Mips = null!; // provided level(s); Mips[0] is the base. GenerateMips blit-fills the rest.
        public bool GenerateMips;
        public FilteringMode Filtering;
        public WrapMode Wrapping;
        public UploadCompletionSource Source = null!;
    }

    private sealed class InFlight
    {
        public ulong Value;
        public CommandBuffer Cmd;
        public VkBuffer Staging;
        public VmaAllocation StagingMemory;
        public VulkanTexture Texture = null!;
        public UploadCompletionSource Source = null!;
    }

    // Pooled IValueTaskSource: makes an upload awaitable with no per-texture TaskCompletionSource/closure
    // allocation. Completed on the render thread in ProcessCompletions; RunContinuationsAsynchronously is
    // left false so the awaiter resumes inline on the completer (the render thread, where AddTexture runs).
    private sealed class UploadCompletionSource : IValueTaskSource<INativeTexture>
    {
        private ManualResetValueTaskSourceCore<INativeTexture> core;
        private VulkanUploadQueue owner = null!;

        public void Init(VulkanUploadQueue o) { owner = o; core.Reset(); }
        public ValueTask<INativeTexture> Task => new(this, core.Version);
        public void Complete(INativeTexture? tex) => core.SetResult(tex!);

        public INativeTexture GetResult(short token)
        {
            try { return core.GetResult(token); }
            finally { owner.ReturnSource(this); }
        }
        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => core.OnCompleted(continuation, state, token, flags);
    }

    private readonly VulkanContext ctx;
    private readonly CommandPool pool;
    private readonly VkSemaphore timeline;
    private readonly Thread thread;
    private readonly BlockingCollection<Request> requests = new(new ConcurrentQueue<Request>());
    private readonly List<InFlight> inFlight = new();
    private readonly ConcurrentQueue<InFlight> ready = new();
    private readonly ConcurrentStack<UploadCompletionSource> sourcePool = new();
    private readonly ConcurrentStack<Request> requestPool = new();
    private readonly ConcurrentStack<InFlight> inFlightPool = new();
    private ulong nextValue;
    private volatile bool disposed;

    private UploadCompletionSource RentSource()
    {
        if (!sourcePool.TryPop(out var s)) s = new UploadCompletionSource();
        s.Init(this);
        return s;
    }
    private void ReturnSource(UploadCompletionSource s) => sourcePool.Push(s);
    private Request RentRequest() => requestPool.TryPop(out var r) ? r : new Request();
    private void ReturnRequest(Request r) { r.Mips = null!; r.Source = null!; requestPool.Push(r); }
    private InFlight RentInFlight() => inFlightPool.TryPop(out var f) ? f : new InFlight();
    private void ReturnInFlight(InFlight f) { f.Texture = null!; f.Source = null!; inFlightPool.Push(f); }

    public VulkanUploadQueue(VulkanContext ctx)
    {
        this.ctx = ctx;

        // command buffers submitted to TransferQueue must come from a pool of that queue's family
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = ctx.TransferQueueFamily,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        VulkanContext.Check(ctx.vk.CreateCommandPool(ctx.Device, in poolInfo, null, out pool), "upload pool");

        var typeInfo = new SemaphoreTypeCreateInfo
        {
            SType = StructureType.SemaphoreTypeCreateInfo,
            SemaphoreType = SemaphoreType.Timeline,
            InitialValue = 0,
        };
        var semInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo, PNext = &typeInfo };
        VulkanContext.Check(ctx.vk.CreateSemaphore(ctx.Device, in semInfo, null, out timeline), "upload timeline");

        thread = new Thread(Run) { Name = "VulkanUploadQueue", IsBackground = true };
        thread.Start();
    }

    /// <summary>The timeline semaphore and the value the render thread's graphics submit should wait
    /// on so every completed upload's write is visible before the queue samples it. Safe to call from
    /// the render thread; <c>vkGetSemaphoreCounterValue</c> needs no external synchronization.</summary>
    public VkSemaphore TimelineSemaphore => timeline;

    public ulong CompletedValue
    {
        get
        {
            ulong v = 0;
            ctx.vk.GetSemaphoreCounterValue(ctx.Device, timeline, &v);
            return v;
        }
    }

    /// <summary>Queues a texture upload and returns a ValueTask that completes (on the render thread, via
    /// <see cref="ProcessCompletions"/>) once the GPU copy is done. Callable from any thread.</summary>
    public ValueTask<INativeTexture> UploadTexture(int width, int height, Rgba32[][] mips, bool generateMips, FilteringMode filtering, WrapMode wrapping)
    {
        var src = RentSource();
        if (disposed)
        {
            src.Complete(null);
            return src.Task;
        }
        var req = RentRequest();
        req.Width = width;
        req.Height = height;
        req.Mips = mips;
        req.GenerateMips = generateMips;
        req.Filtering = filtering;
        req.Wrapping = wrapping;
        req.Source = src;
        requests.Add(req);
        return src.Task;
    }

    /// <summary>Render-thread pump: completes the awaiters of uploads finished on the GPU. Call once per
    /// frame (e.g. from BeginFrame), outside a rendering pass.</summary>
    public void ProcessCompletions()
    {
        while (ready.TryDequeue(out var f))
        {
            var src = f.Source;
            var tex = f.Texture;
            ReturnInFlight(f);
            src.Complete(tex); // resumes the awaiter inline on this (render) thread
        }
    }

    private void Run()
    {
        while (!disposed)
        {
            Reclaim();
            // While uploads are in flight, poll often to reclaim staging promptly; when fully idle
            // block until the next request arrives.
            int timeoutMs = inFlight.Count > 0 ? 1 : -1;
            if (!requests.TryTake(out var req, timeoutMs))
                continue;
            var src = req.Source;
            try
            {
                Submit(req);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[VulkanUploadQueue] upload failed: {e}");
                src.Complete(null); // unblock the awaiter; caller falls back to EmptyTexture
            }
            ReturnRequest(req);
        }
    }

    private void Submit(Request req)
    {
        int width = req.Width, height = req.Height;
        var mips = req.Mips;
        int provided = mips.Length;
        uint mipLevels = req.GenerateMips ? MipCount(width, height) : (uint)provided;
        var usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit;
        // A separate transfer family writes the image and the graphics family samples it: concurrent
        // sharing avoids queue-family ownership transfers (same-family fallback needs none).
        VulkanTexture texture;
        if (ctx.TransferQueueFamily != ctx.QueueFamily)
        {
            uint* fams = stackalloc uint[2] { ctx.QueueFamily, ctx.TransferQueueFamily };
            texture = new VulkanTexture(ctx, width, height, VkFormat.R8G8B8A8Unorm, mipLevels, usage,
                ImageAspectFlags.ColorBit, 1, new ReadOnlySpan<uint>(fams, 2));
        }
        else
            texture = new VulkanTexture(ctx, width, height, VkFormat.R8G8B8A8Unorm, mipLevels, usage,
                ImageAspectFlags.ColorBit);
        texture.SetFiltering(req.Filtering);
        texture.SetWrapping(req.Wrapping);

        // One staging buffer holds the provided level(s); one copy command moves them.
        long total = 0;
        for (int m = 0; m < provided; m++)
            total += (long)Math.Max(1, width >> m) * Math.Max(1, height >> m) * 4;
        var (staging, stagingMemory, mapped) = CreateStaging(total);

        BufferImageCopy* regions = stackalloc BufferImageCopy[provided];
        long offset = 0;
        for (int m = 0; m < provided; m++)
        {
            int w = Math.Max(1, width >> m);
            int h = Math.Max(1, height >> m);
            int bytes = w * h * 4;
            fixed (Rgba32* srcPix = mips[m])
                Unsafe.CopyBlockUnaligned((byte*)mapped + offset, srcPix, (uint)bytes);
            regions[m] = new BufferImageCopy
            {
                BufferOffset = (ulong)offset,
                ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, (uint)m, 0, 1),
                ImageExtent = new Extent3D((uint)w, (uint)h, 1),
            };
            offset += bytes;
        }

        var cmd = AllocCmd();
        var begin = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        ctx.vk.BeginCommandBuffer(cmd, in begin);
        texture.TransitionTo(cmd, ImageLayout.TransferDstOptimal);
        ctx.vk.CmdCopyBufferToImage(cmd, staging, texture.Image, ImageLayout.TransferDstOptimal, (uint)provided, regions);
        if (mipLevels > provided)
            GenerateMips(cmd, texture, fromLevel: provided - 1);
        // Leave the image ready to sample; same queue family ⇒ no ownership transfer, and the graphics
        // queue waits on the timeline (CompletedValue) so this layout/write is visible before use.
        texture.TransitionTo(cmd, ImageLayout.ShaderReadOnlyOptimal);
        ctx.vk.EndCommandBuffer(cmd);

        ulong value = ++nextValue;
        var cmdInfo = new CommandBufferSubmitInfo { SType = StructureType.CommandBufferSubmitInfo, CommandBuffer = cmd };
        var signal = new SemaphoreSubmitInfo
        {
            SType = StructureType.SemaphoreSubmitInfo,
            Semaphore = timeline,
            Value = value,
            StageMask = PipelineStageFlags2.AllCommandsBit,
        };
        var submit = new SubmitInfo2
        {
            SType = StructureType.SubmitInfo2,
            CommandBufferInfoCount = 1,
            PCommandBufferInfos = &cmdInfo,
            SignalSemaphoreInfoCount = 1,
            PSignalSemaphoreInfos = &signal,
        };
        // Dedicated transfer queue ⇒ this thread is its only user (no lock). Shared-queue fallback ⇒
        // serialize against the render thread's submits.
        if (ctx.HasDedicatedTransferQueue)
            VulkanContext.Check(ctx.vk.QueueSubmit2(ctx.TransferQueue, 1, in submit, default), "upload submit");
        else
            lock (ctx.QueueSubmitLock)
                VulkanContext.Check(ctx.vk.QueueSubmit2(ctx.TransferQueue, 1, in submit, default), "upload submit");

        var f = RentInFlight();
        f.Value = value;
        f.Cmd = cmd;
        f.Staging = staging;
        f.StagingMemory = stagingMemory;
        f.Texture = texture;
        f.Source = req.Source;
        inFlight.Add(f);
    }

    /// <summary>Frees staging buffers / command buffers of completed uploads and hands the finished
    /// textures to the render thread. Runs on the upload thread (sole owner of the command pool).</summary>
    private void Reclaim()
    {
        if (inFlight.Count == 0)
            return;
        ulong completed = CompletedValue;
        for (int i = inFlight.Count - 1; i >= 0; i--)
        {
            var f = inFlight[i];
            if (f.Value > completed)
                continue;
            ctx.vk.FreeCommandBuffers(ctx.Device, pool, 1, in f.Cmd);
            Vma.DestroyBuffer(ctx.Allocator, f.Staging, f.StagingMemory);
            ready.Enqueue(f); // ProcessCompletions completes the awaiter and recycles f
            inFlight.RemoveAt(i);
        }
    }

    private static uint MipCount(int width, int height)
        => 1 + (uint)Math.Floor(Math.Log2(Math.Max(width, height)));

    /// <summary>Blit-generates mip levels below the provided ones. Leaves every level in
    /// TransferDstOptimal (matching the whole-range transition the caller applies afterwards).</summary>
    private void GenerateMips(CommandBuffer cmd, VulkanTexture texture, int fromLevel)
    {
        int mipWidth = Math.Max(1, texture.Width >> fromLevel);
        int mipHeight = Math.Max(1, texture.Height >> fromLevel);
        for (uint level = (uint)fromLevel + 1; level < texture.MipLevels; level++)
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

    private CommandBuffer AllocCmd()
    {
        var alloc = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = pool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        VulkanContext.Check(ctx.vk.AllocateCommandBuffers(ctx.Device, in alloc, out var cmd), "upload cmd alloc");
        return cmd;
    }

    private (VkBuffer buffer, VmaAllocation memory, IntPtr mapped) CreateStaging(long bytes)
    {
        var info = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)bytes,
            Usage = BufferUsageFlags.TransferSrcBit,
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
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &info, &allocInfo, &buffer, &memory, &allocationInfo), "upload staging");
        return (buffer, memory, (IntPtr)allocationInfo.pMappedData);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        requests.CompleteAdding();
        thread.Join();
        // Drain anything still in flight (GPU is idle by shutdown time).
        ctx.vk.DeviceWaitIdle(ctx.Device);
        foreach (var f in inFlight)
        {
            ctx.vk.FreeCommandBuffers(ctx.Device, pool, 1, in f.Cmd);
            Vma.DestroyBuffer(ctx.Allocator, f.Staging, f.StagingMemory);
        }
        inFlight.Clear();
        ctx.vk.DestroySemaphore(ctx.Device, timeline, null);
        ctx.vk.DestroyCommandPool(ctx.Device, pool, null);
    }
}
