using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using TheEngine.Interfaces;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace TheEngine.Vulkan;

/// <summary>
/// Static set-3 global buffer: a single persistent, host-visible, persistently-mapped backing
/// shared by all frame slots, handed out in fixed-size slots via a slab free-list. Written only
/// when its contents change (tile load/unload), so unlike <see cref="VulkanGlobalBuffer{T}"/> it
/// is not re-uploaded per frame. See <see cref="IStaticGlobalBuffer{T}"/> for the safety model.
/// </summary>
internal sealed unsafe class VulkanStaticGlobalBuffer<T> : VulkanGlobalBufferBase, IStaticGlobalBuffer<T>
    where T : unmanaged
{
    private readonly VulkanRenderBackend backend;
    private readonly VulkanContext ctx;
    private readonly int slotElementCount;

    private VkBuffer buffer;
    private VmaAllocation memory;
    private T* mapped;
    private int capacitySlots;

    // slab allocator: nextSlot is the high-water mark of never-yet-allocated slots; freeSlots holds
    // returned slots (pushed only after DestroyLater confirms no in-flight frame still reads them).
    private int nextSlot;
    private readonly Stack<int> freeSlots = new();

    public int SlotElementCount => slotElementCount;

    internal VulkanStaticGlobalBuffer(VulkanRenderBackend backend, VulkanContext ctx, uint binding,
        int slotElementCount, int initialSlots)
        : base(binding)
    {
        this.backend = backend;
        this.ctx = ctx;
        this.slotElementCount = slotElementCount;
        Alloc(Math.Max(1, initialSlots));
    }

    // single backing, identical for every frame slot
    internal override void NotifyFrameSlot(int frameSlot) { }

    internal override void FillDescriptor(int frameSlot, out DescriptorBufferInfo info)
        => info = new DescriptorBufferInfo(buffer, 0, (ulong)(capacitySlots * slotElementCount * Unsafe.SizeOf<T>()));

    public int Allocate()
    {
        int slot;
        if (freeSlots.Count > 0)
            slot = freeSlots.Pop();
        else
        {
            if (nextSlot >= capacitySlots)
                Grow(nextSlot + 1);
            slot = nextSlot++;
        }
        return slot * slotElementCount;
    }

    public Span<T> GetSpan(int elementOffset)
        => new Span<T>(mapped + elementOffset, slotElementCount);

    public void Free(int elementOffset)
    {
        int slot = elementOffset / slotElementCount;
        // a draw recorded this frame (or in a still-pending frame) may reference this slot; don't
        // hand it to a new tenant until those submissions retire, or the new write corrupts an
        // in-flight read (same deferral the engine uses for buffers/textures/bindless slots).
        ctx.DestroyLater(() => freeSlots.Push(slot));
    }

    private void Grow(int neededSlots)
    {
        int newCap = Math.Max(neededSlots, capacitySlots * 2);
        var oldBuffer = buffer;
        var oldMemory = memory;
        var oldMapped = mapped;
        int oldBytes = capacitySlots * slotElementCount * Unsafe.SizeOf<T>();

        Alloc(newCap);

        // static contents must survive a grow (the dynamic buffer rewrites every frame; this one
        // does not), so copy the existing slots into the new backing before publishing it.
        if (oldBytes > 0)
            System.Buffer.MemoryCopy(oldMapped, mapped, (long)newCap * slotElementCount * Unsafe.SizeOf<T>(), oldBytes);

        // in-flight command buffers may still reference the old backing; defer its free.
        ctx.DestroyLater(() => Vma.DestroyBuffer(ctx.Allocator, oldBuffer, oldMemory));
        // GameSet3 is update-after-bind, so repointing the descriptor at the new backing mid-frame
        // is safe; the copied data means any draw using the new descriptor reads correct values.
        backend.RewriteGlobalBufferDescriptorAllSlots(this);
    }

    private void Alloc(int slots)
    {
        ulong size = (ulong)((long)slots * slotElementCount * Unsafe.SizeOf<T>());
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
        VkBuffer buf;
        VmaAllocation mem;
        VmaAllocationInfo info;
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &bufferInfo, &allocInfo, &buf, &mem, &info), "vma static global buffer");
        buffer = buf;
        memory = mem;
        mapped = (T*)info.pMappedData;
        capacitySlots = slots;
    }

    internal override void Dispose()
    {
        if (buffer.Handle != 0)
            Vma.DestroyBuffer(ctx.Allocator, buffer, memory);
    }
}
