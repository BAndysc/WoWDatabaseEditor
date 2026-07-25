using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using TheEngine.Resources;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace TheEngine.Vulkan;

internal interface IVulkanBuffer
{
    VkBuffer Buffer { get; }
    long SizeInBytes { get; }
    void MarkUsed();
}

/// <summary>
/// Host-visible (UMA-friendly), persistently mapped GPU buffer. UpdateBuffer has GL
/// orphaning semantics: if any submitted or currently recorded frame may still read the
/// buffer, a fresh backing allocation replaces the old one (destroyed once the GPU is
/// provably done), so draws recorded before the update keep their data.
/// </summary>
internal sealed unsafe class VulkanBuffer<T> : INativeBuffer<T>, IVulkanBuffer where T : unmanaged
{
    private sealed class Backing
    {
        public VkBuffer Buffer;
        public VmaAllocation Allocation;
        public void* Mapped;
        public long Capacity;
        // submission in which this backing was last bound for drawing; the GPU is provably
        // done reading it once CompletedSubmission reaches this value, so it can be reused.
        public ulong lastUseSubmission;
    }

    private readonly VulkanContext ctx;
    private readonly VulkanRenderBackend backend;
    // A self-sizing ring of backings. Per-frame UpdateBuffer rotates to a backing the GPU is
    // already done with instead of allocating a fresh one and destroying the old every frame
    // (which on a no-VMA backend was hundreds of vkAllocateMemory/vkFreeMemory per frame). The
    // ring grows only until a free backing always exists (2-3 with double buffering), then stops.
    private readonly List<Backing> ring = new();
    private Backing? backing;
    private bool disposed;

    public BufferTypeEnum BufferType { get; }
    public int Length { get; private set; }
    public long SizeInBytes => backing?.Capacity ?? 0;

    public VkBuffer Buffer => backing!.Buffer;

    internal VulkanBuffer(VulkanRenderBackend backend, VulkanContext ctx, BufferTypeEnum bufferType, int initialBytes)
    {
        this.backend = backend;
        this.ctx = ctx;
        BufferType = bufferType;
        backing = AllocBacking(Math.Max(4, initialBytes), zero: true);
    }

    private BufferUsageFlags Usage => BufferType switch
    {
        BufferTypeEnum.Vertex => BufferUsageFlags.VertexBufferBit,
        BufferTypeEnum.Index => BufferUsageFlags.IndexBufferBit,
        BufferTypeEnum.ConstPixel or BufferTypeEnum.ConstVertex => BufferUsageFlags.UniformBufferBit,
        BufferTypeEnum.IndirectCommands => BufferUsageFlags.IndirectBufferBit,
        // structured buffers are all std430 SSBOs now
        _ => BufferUsageFlags.StorageBufferBit,
    };

    private Backing AllocBacking(long bytes, bool zero)
    {
        var b = new Backing { Capacity = bytes };
        var info = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)bytes,
            Usage = Usage | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive,
        };
        // VMA creates the buffer, sub-allocates host-visible+coherent memory and binds it in one
        // call. requiredFlags preserves the exact memory-type guarantee the engine relied on
        // (coherent => no manual flush); the Mapped flag keeps the allocation persistently mapped,
        // so pMappedData (offset already applied) is valid for the backing's whole lifetime.
        var allocInfo = new VmaAllocationCreateInfo
        {
            flags = VmaAllocationCreateFlagBits.VmaAllocationCreateMappedBit,
            usage = VmaMemoryUsage.CpuToGpu,
            requiredFlags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
        };
        VkBuffer createdBuffer;
        VmaAllocation allocation;
        VmaAllocationInfo allocationInfo;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &info, &allocInfo, &createdBuffer, &allocation, &allocationInfo), "vmaCreateBuffer");
        b.Buffer = createdBuffer;
        b.Allocation = allocation;
        b.Mapped = allocationInfo.pMappedData;
        if (zero)
            Unsafe.InitBlockUnaligned(b.Mapped, 0, (uint)bytes);

        // AllocBacking can run on a worker thread (off-render-thread buffer creation in the
        // content-loading pipeline), so the shared byte tracker must be updated atomically.
        System.Threading.Interlocked.Add(ref backend.TotalBufferBytesTracker, bytes);
        ring.Add(b);
        return b;
    }

    private void DestroyBacking(Backing b)
    {
        System.Threading.Interlocked.Add(ref backend.TotalBufferBytesTracker, -b.Capacity);
        var vk = ctx.vk;
        var device = ctx.Device;
        var allocator = ctx.Allocator;
        var buffer = b.Buffer;
        var allocation = b.Allocation;
        ctx.DestroyLater(() =>
        {
            Vma.DestroyBuffer(allocator, buffer, allocation); // destroys the VkBuffer and frees its allocation
        });
    }

    /// <summary>Marks the current backing as referenced by the frame being recorded, for orphaning decisions.</summary>
    public void MarkUsed()
    {
        if (backing != null)
            backing.lastUseSubmission = ctx.CurrentSubmission;
    }

    public void UpdateBuffer(ReadOnlySpan<T> newData)
    {
        long bytes = (long)newData.Length * Unsafe.SizeOf<T>();

        // Reuse the current backing if it fits and the GPU is provably done reading it.
        bool currentUsable = backing != null && bytes <= backing.Capacity
                             && backing.lastUseSubmission <= ctx.CompletedSubmission;
        if (!currentUsable)
        {
            // Need a different backing: the current one may still be read by an in-flight
            // frame (orphaning semantics), or it is too small. Prefer a free backing already
            // in the ring (GPU done with it and big enough) instead of allocating.
            Backing? reuse = null;
            foreach (var b in ring)
            {
                if (!ReferenceEquals(b, backing) && bytes <= b.Capacity
                    && b.lastUseSubmission <= ctx.CompletedSubmission)
                {
                    reuse = b;
                    break;
                }
            }
            if (reuse != null)
            {
                backing = reuse;
            }
            else
            {
                // No free backing fits — grow the ring. Size to at least the previous
                // capacity so a one-off growth doesn't bounce, and retire any free backings
                // that are now too small to keep the ring (and memory) bounded.
                long cap = Math.Max(Math.Max(4, bytes), backing?.Capacity ?? 0);
                for (int i = ring.Count - 1; i >= 0; i--)
                {
                    var b = ring[i];
                    if (b.Capacity < cap && b.lastUseSubmission <= ctx.CompletedSubmission)
                    {
                        ring.RemoveAt(i);
                        DestroyBacking(b);
                    }
                }
                backing = AllocBacking(cap, zero: false);
            }
        }
        fixed (T* src = newData)
            Unsafe.CopyBlockUnaligned(backing!.Mapped, src, (uint)bytes);
        Length = newData.Length;
    }

    public void UpdateBuffer(ref T newData)
        => UpdateBuffer(MemoryMarshal.CreateReadOnlySpan(ref newData, 1));

    /// <summary>Direct read access for the executor (copying global UBO contents into the frame ring).</summary>
    internal ReadOnlySpan<byte> MappedSpan => new(backing!.Mapped, (int)Math.Min(backing.Capacity, (long)Length * Unsafe.SizeOf<T>()));

    public void Activate(int slot)
    {
        // binding happens through the Vulkan command list (descriptors / vkCmdBindVertexBuffers);
        // this GL entry point is never called by the Vulkan executor
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        foreach (var b in ring)
            DestroyBacking(b);
        ring.Clear();
        backing = null;
    }

    ~VulkanBuffer()
    {
        Dispose();
    }
}
