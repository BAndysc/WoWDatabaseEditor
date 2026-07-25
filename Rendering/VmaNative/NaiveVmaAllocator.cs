using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Vulkan;

// Fully-managed replacement for the VMA bindings: a sub-allocating Vulkan memory allocator.
//
// The previous "naive" backend did one vkAllocateMemory + vkMapMemory per resource. The engine
// creates and destroys a staging buffer on every upload (CreateStaging, texture/buffer uploads),
// so on MoltenVK that meant hundreds of driver allocations/maps per frame => ~1 fps.
//
// This allocator instead carves resources out of a small number of large device-memory "blocks":
//   * One pool per (memoryTypeIndex, linear?) pair. Buffers are linear, images are OPTIMAL-tiled,
//     so keeping them in separate blocks sidesteps bufferImageGranularity aliasing entirely.
//   * Each block is one vkAllocateMemory; host-visible blocks are persistently mapped once. A
//     resource's mapped pointer is just blockMapped + suballocationOffset.
//   * Within a block, free space is a coalescing first-fit free list (a sorted run of regions that
//     fully tile the block). Adjacent free regions merge on free, so fragmentation self-heals.
//   * Allocations larger than the preferred block size get their own exactly-sized "dedicated"
//     block, which is released the moment it becomes empty; ordinary blocks are kept (one empty
//     block is retained per pool to avoid alloc/free thrash) and only torn down at DestroyAllocator.
//
// The engine only ever calls the six functions below, so this is all that's needed. All host pools
// are HOST_COHERENT, so no explicit flush/invalidate is required.
//
// Replaces the compiled VMABindings/Generated.cs (excluded from the build in VmaNative.csproj).
// The opaque handle structs (VmaAllocator/VmaAllocation/...) still come from VmaHandles.cs.

namespace VMASharp;

// Minimal mirrors of the VMA POD types — only the fields the engine actually sets/reads.
// These are never passed to native code, so layout is irrelevant.
public enum VmaMemoryUsage
{
    Unknown = 0,
    GpuOnly = 1,
    CpuOnly = 2,
    CpuToGpu = 3,
    GpuToCpu = 4,
}

[Flags]
public enum VmaAllocationCreateFlagBits
{
    None = 0,
    VmaAllocationCreateMappedBit = 0x00000004,
}

public unsafe struct VmaVulkanFunctions
{
    public delegate* unmanaged<Instance, byte*, IntPtr> vkGetInstanceProcAddr;
    public delegate* unmanaged<Device, byte*, IntPtr> vkGetDeviceProcAddr;
}

public unsafe struct VmaAllocatorCreateInfo
{
    public PhysicalDevice physicalDevice;
    public Device device;
    public Instance instance;
    public uint vulkanApiVersion;
    public VmaVulkanFunctions* pVulkanFunctions;
}

public struct VmaAllocationCreateInfo
{
    public VmaAllocationCreateFlagBits flags;
    public VmaMemoryUsage usage;
    public MemoryPropertyFlags requiredFlags;
}

public unsafe struct VmaAllocationInfo
{
    public void* pMappedData;
}

public static unsafe class Vma
{
    // The engine only ever creates a single allocator, so the state is held statically.
    private static readonly object gate = new();
    private static Vk vk = null!;
    private static Device device;
    private static PhysicalDeviceMemoryProperties memProps;
    private static Pool?[] pools = Array.Empty<Pool?>(); // indexed by memoryTypeIndex*2 + (linear?1:0)

    private const ulong MinBlockSize = 32UL * 1024 * 1024;  // 32 MB
    private const ulong MaxBlockSize = 256UL * 1024 * 1024; // 256 MB

    // Per-frame instrumentation - we own this allocator, so it counts its own churn. Read and reset
    // once per frame by the engine. "Allocations"/"Frees" are resource-level (CreateBuffer/CreateImage
    // vs DestroyBuffer/DestroyImage); "DeviceAllocations"/"DeviceFrees" are the actual vkAllocateMemory/
    // vkFreeMemory for whole blocks - those should reach 0 in steady state (suballocation reuses blocks).
    public static int Allocations;
    public static int Frees;
    public static int DeviceAllocations;
    public static int DeviceFrees;

    public static void ResetFrameStats()
    {
        Allocations = 0;
        Frees = 0;
        DeviceAllocations = 0;
        DeviceFrees = 0;
    }

    // One free/used region inside a block. Regions of a block tile it contiguously, sorted by offset.
    private sealed class Region
    {
        public ulong Offset;
        public ulong Size;
        public bool Free;
        public Block Block = null!;
        public LinkedListNode<Region> Node = null!;       // back-reference into Block.Regions for O(1) free
        public LinkedListNode<Region>? FreeNode;          // node in Block.FreeRegions while Free, else null
        public GCHandle Handle;                           // the caller-facing VmaAllocation handle while allocated, so DestroyAllocator can reclaim stragglers
    }

    private sealed unsafe class Block
    {
        public DeviceMemory Memory;
        public ulong Size;
        public byte* Mapped;   // null unless the block is host-visible (then persistently mapped)
        public bool Dedicated; // exactly-sized for one oversized allocation; freed when empty
        public Pool Pool = null!;
        public readonly LinkedList<Region> Regions = new();     // every region, ordered by offset (for coalescing)
        public readonly LinkedList<Region> FreeRegions = new(); // only the free regions (for fast suballocation)

        public bool IsEmpty => Regions.Count == 1 && Regions.First!.Value.Free;
    }

    private sealed class Pool
    {
        public uint MemoryTypeIndex;
        public bool HostVisible;
        public ulong BlockSize;
        public readonly List<Block> Blocks = new();
    }

    public static Result CreateAllocator(VmaAllocatorCreateInfo* pCreateInfo, VmaAllocator* pAllocator)
    {
        vk = Vk.GetApi();
        device = pCreateInfo->device;
        vk.GetPhysicalDeviceMemoryProperties(pCreateInfo->physicalDevice, out memProps);
        pools = new Pool?[memProps.MemoryTypeCount * 2];
        *pAllocator = new VmaAllocator(1); // non-null sentinel; we ignore it afterwards
        return Result.Success;
    }

    public static void DestroyAllocator(VmaAllocator allocator)
    {
        lock (gate)
        {
            foreach (var pool in pools)
            {
                if (pool == null)
                    continue;
                foreach (var block in pool.Blocks)
                {
                    // reclaim the GCHandles of allocations the caller never destroyed, otherwise
                    // they root their Region (and Block) forever
                    foreach (var region in block.Regions)
                        if (region.Handle.IsAllocated)
                            region.Handle.Free();
                    DestroyBlock(block);
                }
                pool.Blocks.Clear();
            }
            Array.Clear(pools);
        }
    }

    public static Result CreateBuffer(VmaAllocator allocator,
        BufferCreateInfo* pBufferCreateInfo, VmaAllocationCreateInfo* pAllocInfo,
        VkBuffer* pBuffer, VmaAllocation* pAllocation, VmaAllocationInfo* pAllocationInfo)
    {
        VkBuffer buffer;
        var res = vk.CreateBuffer(device, pBufferCreateInfo, null, &buffer);
        if (res != Result.Success)
            return res;

        vk.GetBufferMemoryRequirements(device, buffer, out var req);
        // Buffers are linear-tiled.
        if (!TryAllocate(req, pAllocInfo, linear: true, out var region, out var mapped))
        {
            vk.DestroyBuffer(device, buffer, null);
            return Result.ErrorOutOfDeviceMemory;
        }

        res = vk.BindBufferMemory(device, buffer, region.Block.Memory, region.Offset);
        if (res != Result.Success)
        {
            Free(region);
            vk.DestroyBuffer(device, buffer, null);
            return res;
        }

        *pBuffer = buffer;
        *pAllocation = StoreAlloc(region);
        if (pAllocationInfo != null)
            pAllocationInfo->pMappedData = mapped;
        Interlocked.Increment(ref Allocations);
        return Result.Success;
    }

    public static void DestroyBuffer(VmaAllocator allocator, VkBuffer buffer, VmaAllocation allocation)
    {
        if (buffer.Handle != 0)
        {
            vk.DestroyBuffer(device, buffer, null);
            Interlocked.Increment(ref Frees);
        }
        FreeAlloc(allocation);
    }

    public static Result CreateImage(VmaAllocator allocator,
        ImageCreateInfo* pImageCreateInfo, VmaAllocationCreateInfo* pAllocInfo,
        VkImage* pImage, VmaAllocation* pAllocation, VmaAllocationInfo* pAllocationInfo)
    {
        VkImage image;
        var res = vk.CreateImage(device, pImageCreateInfo, null, &image);
        if (res != Result.Success)
            return res;

        vk.GetImageMemoryRequirements(device, image, out var req);
        // Images use OPTIMAL tiling (non-linear); keep them in their own blocks.
        bool linear = pImageCreateInfo->Tiling == ImageTiling.Linear;
        if (!TryAllocate(req, pAllocInfo, linear, out var region, out var mapped))
        {
            vk.DestroyImage(device, image, null);
            return Result.ErrorOutOfDeviceMemory;
        }

        res = vk.BindImageMemory(device, image, region.Block.Memory, region.Offset);
        if (res != Result.Success)
        {
            Free(region);
            vk.DestroyImage(device, image, null);
            return res;
        }

        *pImage = image;
        *pAllocation = StoreAlloc(region);
        if (pAllocationInfo != null)
            pAllocationInfo->pMappedData = mapped;
        Interlocked.Increment(ref Allocations);
        return Result.Success;
    }

    public static void DestroyImage(VmaAllocator allocator, VkImage image, VmaAllocation allocation)
    {
        if (image.Handle != 0)
        {
            vk.DestroyImage(device, image, null);
            Interlocked.Increment(ref Frees);
        }
        FreeAlloc(allocation);
    }

    // ---------------------------------------------------------------- allocation

    private static bool TryAllocate(MemoryRequirements req, VmaAllocationCreateInfo* ci, bool linear,
        out Region region, out byte* mapped)
    {
        region = null!;
        mapped = null;

        var required = ci->requiredFlags;
        bool wantMapped = (ci->flags & VmaAllocationCreateFlagBits.VmaAllocationCreateMappedBit) != 0;
        if (required == 0)
            required = ci->usage == VmaMemoryUsage.GpuOnly
                ? MemoryPropertyFlags.DeviceLocalBit
                : MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
        if (wantMapped)
            required |= MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;

        int typeIndex = FindMemoryType(req.MemoryTypeBits, required);
        if (typeIndex < 0)
            return false;

        ulong align = Math.Max(req.Alignment, 1);

        lock (gate)
        {
            var pool = GetPool((uint)typeIndex, linear);

            // First-fit across existing blocks.
            foreach (var block in pool.Blocks)
                if (TrySuballocate(block, req.Size, align, out region))
                {
                    mapped = block.Mapped != null ? block.Mapped + region.Offset : null;
                    return true;
                }

            // Need a fresh block. Oversized allocations get a dedicated, exactly-sized block.
            bool dedicated = req.Size > pool.BlockSize;
            ulong blockSize = dedicated ? AlignUp(req.Size, align) : pool.BlockSize;
            var fresh = CreateBlock(pool, blockSize, dedicated);
            if (fresh == null)
                return false;

            if (!TrySuballocate(fresh, req.Size, align, out region))
            {
                // Should be impossible (empty block big enough), but stay safe.
                pool.Blocks.Remove(fresh);
                DestroyBlock(fresh);
                return false;
            }
            mapped = fresh.Mapped != null ? fresh.Mapped + region.Offset : null;
            return true;
        }
    }

    private static Pool GetPool(uint typeIndex, bool linear)
    {
        int key = (int)typeIndex * 2 + (linear ? 1 : 0);
        var pool = pools[key];
        if (pool != null)
            return pool;

        var flags = memProps.MemoryTypes[(int)typeIndex].PropertyFlags;
        uint heapIndex = memProps.MemoryTypes[(int)typeIndex].HeapIndex;
        ulong heapSize = memProps.MemoryHeaps[(int)heapIndex].Size;
        pool = new Pool
        {
            MemoryTypeIndex = typeIndex,
            HostVisible = (flags & MemoryPropertyFlags.HostVisibleBit) != 0,
            // VMA's heuristic: ~1/8 of the heap, clamped to a sane window.
            BlockSize = Math.Clamp(heapSize / 8, MinBlockSize, MaxBlockSize),
        };
        pools[key] = pool;
        return pool;
    }

    private static Block? CreateBlock(Pool pool, ulong size, bool dedicated)
    {
        var alloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = size,
            MemoryTypeIndex = pool.MemoryTypeIndex,
        };
        DeviceMemory mem;
        if (vk.AllocateMemory(device, &alloc, null, &mem) != Result.Success)
            return null;

        byte* mapped = null;
        if (pool.HostVisible)
        {
            void* p;
            if (vk.MapMemory(device, mem, 0, Vk.WholeSize, 0, &p) != Result.Success)
            {
                vk.FreeMemory(device, mem, null);
                return null;
            }
            mapped = (byte*)p;
        }

        var block = new Block
        {
            Memory = mem,
            Size = size,
            Mapped = mapped,
            Dedicated = dedicated,
            Pool = pool,
        };
        // The whole block starts as one free region.
        var region = new Region { Offset = 0, Size = size, Free = true, Block = block };
        region.Node = block.Regions.AddFirst(region);
        region.FreeNode = block.FreeRegions.AddFirst(region);
        pool.Blocks.Add(block);
        Interlocked.Increment(ref DeviceAllocations);
        return block;
    }

    private static void DestroyBlock(Block block)
    {
        if (block.Mapped != null)
            vk.UnmapMemory(device, block.Memory);
        vk.FreeMemory(device, block.Memory, null);
        Interlocked.Increment(ref DeviceFrees);
    }

    // First-fit within a single block; splits the chosen free region into [pad?][used][rest?].
    // Only the block's free regions are scanned, so a full block costs O(1) to reject.
    private static bool TrySuballocate(Block block, ulong size, ulong align, out Region used)
    {
        used = null!;
        for (var fn = block.FreeRegions.First; fn != null; fn = fn.Next)
        {
            var r = fn.Value; // always free by construction

            ulong aligned = AlignUp(r.Offset, align);
            ulong pad = aligned - r.Offset;
            if (r.Size < pad + size)
                continue;

            ulong regionEnd = r.Offset + r.Size;
            ulong usedEnd = aligned + size;

            // Front padding (from alignment) stays as a free region; otherwise reuse this node.
            LinkedListNode<Region> usedNode;
            if (pad > 0)
            {
                r.Size = pad; // r remains free (and stays on the free list), now covering only the padding
                var usedRegion = new Region { Offset = aligned, Size = size, Free = false, Block = block };
                usedNode = block.Regions.AddAfter(r.Node, usedRegion);
            }
            else
            {
                r.Free = false;
                r.Size = size;
                block.FreeRegions.Remove(fn); // no longer free
                r.FreeNode = null;
                usedNode = r.Node;
            }
            usedNode.Value.Node = usedNode;

            // Trailing remainder becomes a free region.
            if (usedEnd < regionEnd)
            {
                var rest = new Region { Offset = usedEnd, Size = regionEnd - usedEnd, Free = true, Block = block };
                rest.Node = block.Regions.AddAfter(usedNode, rest);
                rest.FreeNode = block.FreeRegions.AddLast(rest);
            }

            used = usedNode.Value;
            return true;
        }
        return false;
    }

    private static void Free(Region region)
    {
        lock (gate)
        {
            var block = region.Block;
            var node = region.Node;
            node.Value.Free = true; // not yet on the free list; added (or merged into a neighbour) below

            // Coalesce with the following free region (drop it from both lists).
            var next = node.Next;
            if (next != null && next.Value.Free)
            {
                node.Value.Size += next.Value.Size;
                if (next.Value.FreeNode != null)
                    block.FreeRegions.Remove(next.Value.FreeNode);
                block.Regions.Remove(next);
            }
            // Coalesce with the preceding free region (already on the free list; absorb node into it).
            var prev = node.Previous;
            if (prev != null && prev.Value.Free)
            {
                prev.Value.Size += node.Value.Size;
                block.Regions.Remove(node);
                node = prev;
            }

            // Ensure the surviving region is on the free list (true unless we merged into prev).
            if (node.Value.FreeNode == null)
                node.Value.FreeNode = block.FreeRegions.AddLast(node.Value);

            if (block.IsEmpty)
                MaybeReleaseBlock(block);
        }
    }

    // Release dedicated blocks immediately; for ordinary blocks keep at most one empty block per
    // pool so steady-state churn never re-allocates from the driver.
    private static void MaybeReleaseBlock(Block block)
    {
        var pool = block.Pool;
        if (!block.Dedicated)
        {
            int empties = 0;
            foreach (var b in pool.Blocks)
                if (b.IsEmpty)
                    empties++;
            if (empties <= 1)
                return; // keep this one as a warm spare
        }
        pool.Blocks.Remove(block);
        DestroyBlock(block);
    }

    private static int FindMemoryType(uint typeBits, MemoryPropertyFlags required)
    {
        for (uint i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[(int)i].PropertyFlags & required) == required)
                return (int)i;
        }
        return -1;
    }

    private static ulong AlignUp(ulong value, ulong align) => (value + align - 1) & ~(align - 1);

    // ---------------------------------------------------------------- handle marshalling

    // A VmaAllocation handle is a GCHandle to the owning Region, which knows its block and offset.
    private static VmaAllocation StoreAlloc(Region region)
    {
        var gch = GCHandle.Alloc(region);
        region.Handle = gch;
        return new VmaAllocation(GCHandle.ToIntPtr(gch));
    }

    private static void FreeAlloc(VmaAllocation allocation)
    {
        if (allocation.Handle == 0)
            return;
        var gch = GCHandle.FromIntPtr(allocation.Handle);
        var region = (Region)gch.Target!;
        region.Handle = default;
        Free(region);
        gch.Free();
    }
}
