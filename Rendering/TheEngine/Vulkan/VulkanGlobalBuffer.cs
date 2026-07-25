using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using TheEngine.Interfaces;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace TheEngine.Vulkan;

/// <summary>Non-generic base so the backend can manage heterogeneous global buffers.</summary>
internal abstract class VulkanGlobalBufferBase
{
    internal readonly uint Binding;
    internal VulkanGlobalBufferBase(uint binding) => Binding = binding;

    internal abstract unsafe void FillDescriptor(int frameSlot, out DescriptorBufferInfo info);
    internal abstract void NotifyFrameSlot(int frameSlot);
    internal abstract void Dispose();
}

/// <summary>
/// Frame-global persistently-mapped storage buffer: one physical backing per frame-in-flight
/// slot, each sized independently (grows on demand after the fence wait makes it safe).
/// </summary>
internal sealed unsafe class VulkanGlobalBuffer<T> : VulkanGlobalBufferBase, IGlobalBuffer<T>
    where T : unmanaged
{
    private readonly VulkanRenderBackend backend;
    private readonly VulkanContext ctx;

    private readonly VkBuffer[] buffers;
    private readonly VmaAllocation[] memories;
    private readonly T*[] mapped;
    private readonly int[] capacities;
    private int currentFrameSlot;

    internal VulkanGlobalBuffer(VulkanRenderBackend backend, VulkanContext ctx, uint binding, int initialCapacity, int framesInFlight)
        : base(binding)
    {
        this.backend = backend;
        this.ctx = ctx;
        buffers = new VkBuffer[framesInFlight];
        memories = new VmaAllocation[framesInFlight];
        mapped = new T*[framesInFlight];
        capacities = new int[framesInFlight];
        for (int i = 0; i < framesInFlight; i++)
            AllocSlot(i, Math.Max(1, initialCapacity));
    }

    internal override void NotifyFrameSlot(int frameSlot) => currentFrameSlot = frameSlot;

    public Span<T> BeginWrite(int count)
    {
        if (count > capacities[currentFrameSlot])
            GrowSlot(currentFrameSlot, count);
        return new Span<T>(mapped[currentFrameSlot], count);
    }

    private void GrowSlot(int slot, int newCount)
    {
        int cap = Math.Max(newCount, capacities[slot] * 2);
        // GameSet3 is a regular (non-update-after-bind) descriptor set rebound across many
        // draws within the current frame, so the old buffer may still be referenced by
        // already-recorded commands in this frame's command buffer even though the fence for
        // this slot's *previous* submission is done - defer the free like every other dynamic
        // Vulkan resource (VulkanBuffer/VulkanTexture/VulkanShader) instead of freeing it
        // immediately (confirmed by the Vulkan validation layer as a destroyed-buffer-still-
        // bound hazard).
        var oldBuffer = buffers[slot];
        var oldMemory = memories[slot];
        ctx.DestroyLater(() => Vma.DestroyBuffer(ctx.Allocator, oldBuffer, oldMemory));
        AllocSlot(slot, cap);
        backend.RewriteGlobalBufferDescriptor(this, slot);
    }

    private void AllocSlot(int slot, int capacity)
    {
        ulong size = (ulong)(capacity * Unsafe.SizeOf<T>());
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
        VulkanContext.Check(Vma.CreateBuffer(ctx.Allocator, &bufferInfo, &allocInfo, &buf, &mem, &info), "vma global buffer");
        buffers[slot] = buf;
        memories[slot] = mem;
        mapped[slot] = (T*)info.pMappedData;
        capacities[slot] = capacity;
    }

    internal override void FillDescriptor(int frameSlot, out DescriptorBufferInfo info)
        => info = new DescriptorBufferInfo(buffers[frameSlot], 0, (ulong)(capacities[frameSlot] * Unsafe.SizeOf<T>()));

    internal override void Dispose()
    {
        for (int i = 0; i < buffers.Length; i++)
            if (buffers[i].Handle != 0)
                Vma.DestroyBuffer(ctx.Allocator, buffers[i], memories[i]);
    }
}
