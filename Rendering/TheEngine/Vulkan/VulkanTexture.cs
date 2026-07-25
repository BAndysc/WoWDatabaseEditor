using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using TheEngine.Resources;
using VMASharp;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkSampler = Silk.NET.Vulkan.Sampler;

namespace TheEngine.Vulkan;

/// <summary>
/// A 2D image (sampled texture and/or render target attachment). The current image
/// layout is tracked here so barriers can be derived from actual state - the engine's
/// declared "from" usage is not authoritative (see <see cref="TheEngine.Rendering.ResourceUsage"/>).
/// </summary>
internal sealed unsafe class VulkanTexture : INativeTexture
{
    private readonly VulkanContext ctx;
    public Image Image;
    public VmaAllocation Memory;
    public ImageView View;
    public Format Format;
    public ImageAspectFlags Aspect;
    public uint MipLevels;
    public uint ArrayLayers;
    public ImageLayout CurrentLayout = ImageLayout.Undefined;

    public int Width { get; }
    public int Height { get; }
    public int NativeHandle => (int)(Image.Handle & 0x7FFFFFFF) | 1; // non-zero marker for the dispose list
    public int SizeInBytes { get; }

    public FilteringMode Filtering = FilteringMode.Linear;
    public WrapMode Wrapping = WrapMode.Repeat;

    private bool disposed;

    public bool IsDepth => Aspect == ImageAspectFlags.DepthBit;

    public VulkanTexture(VulkanContext ctx, int width, int height, Format format, uint mipLevels, ImageUsageFlags usage, ImageAspectFlags aspect, uint arrayLayers = 1, ReadOnlySpan<uint> concurrentFamilies = default)
    {
        this.ctx = ctx;
        Width = width;
        Height = height;
        Format = format;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        Aspect = aspect;
        SizeInBytes = width * height * FormatSize(format) * (mipLevels > 1 ? 4 : 3) / 3 * (int)arrayLayers;

        var info = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = format,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
        };
        // VMA creates the image, sub-allocates device-local memory and binds it in one call.
        var allocInfo = new VmaAllocationCreateInfo
        {
            usage = VmaMemoryUsage.GpuOnly,
            requiredFlags = MemoryPropertyFlags.DeviceLocalBit,
        };
        Image img;
        VmaAllocation allocation;
        // When the image is written by a separate transfer family and sampled by the graphics family,
        // concurrent sharing lets both access it without queue-family ownership transfers.
        if (concurrentFamilies.Length >= 2)
        {
            info.SharingMode = SharingMode.Concurrent;
            info.QueueFamilyIndexCount = (uint)concurrentFamilies.Length;
            fixed (uint* pFamilies = concurrentFamilies)
            {
                info.PQueueFamilyIndices = pFamilies;
                VulkanContext.Check(Vma.CreateImage(ctx.Allocator, &info, &allocInfo, &img, &allocation, null), "vmaCreateImage");
            }
        }
        else
            VulkanContext.Check(Vma.CreateImage(ctx.Allocator, &info, &allocInfo, &img, &allocation, null), "vmaCreateImage");
        Image = img;
        Memory = allocation;

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Image,
            ViewType = arrayLayers > 1 ? ImageViewType.Type2DArray : ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(aspect, 0, mipLevels, 0, arrayLayers),
        };
        VulkanContext.Check(ctx.vk.CreateImageView(ctx.Device, in viewInfo, null, out View), "image view");
    }

    private static int FormatSize(Format format) => format switch
    {
        Format.R32G32B32A32Sfloat => 16,
        Format.R32Sfloat or Format.R32Uint or Format.D32Sfloat or Format.R8G8B8A8Unorm or Format.B8G8R8A8Unorm => 4,
        _ => 4,
    };

    /// <summary>Records a layout transition with broad (all-commands) scopes. cmd must be outside a rendering pass.</summary>
    public void TransitionTo(CommandBuffer cmd, ImageLayout newLayout)
    {
        if (CurrentLayout == newLayout)
            return;
        var barrier = new ImageMemoryBarrier2
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = AccessFlags2.MemoryWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryWriteBit | AccessFlags2.MemoryReadBit,
            OldLayout = CurrentLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = Image,
            SubresourceRange = new ImageSubresourceRange(Aspect, 0, MipLevels, 0, ArrayLayers),
        };
        var dep = new DependencyInfo
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier,
        };
        ctx.vk.CmdPipelineBarrier2(cmd, in dep);
        CurrentLayout = newLayout;
    }

    public ImageLayout AttachmentLayout => IsDepth ? ImageLayout.DepthAttachmentOptimal : ImageLayout.ColorAttachmentOptimal;

    public void Activate(int slot)
    {
        // GL-only entry point; on Vulkan binding happens through descriptor sets
    }

    public void SetFiltering(FilteringMode mode) => Filtering = mode;

    public void SetWrapping(WrapMode mode) => Wrapping = mode;

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        // reclaim this texture's bindless slot (repoint its descriptor to the fallback) before
        // the image is freed, on the same deferred schedule, so no later frame binds the
        // bindless array with a slot still pointing at this freed image.
        ctx.Backend?.ReleaseBindlessTexture(this);
        var vk = ctx.vk;
        var device = ctx.Device;
        var allocator = ctx.Allocator;
        var view = View;
        var image = Image;
        var allocation = Memory;
        ctx.DestroyLater(() =>
        {
            vk.DestroyImageView(device, view, null);
            Vma.DestroyImage(allocator, image, allocation); // destroys the VkImage and frees its allocation
        });
    }
}

/// <summary>
/// A render target: 1-2 color attachments plus an optional depth attachment. Standalone
/// render textures own their attachments (color0 RGBA8, extra attachments R32ui, D32 depth),
/// wrapping render textures reference textures owned by the texture manager.
/// When a render texture is sampled or blitted, color attachment 0 is the image used,
/// matching the GL backend.
/// </summary>
internal sealed class VulkanRenderTexture : INativeTexture
{
    public readonly VulkanTexture[] Colors;
    public readonly VulkanTexture? Depth;
    private readonly bool ownsColors;
    private readonly bool ownsDepth;
    private bool disposed;

    public int Width { get; }
    public int Height { get; }
    public int NativeHandle => Colors.Length > 0 ? Colors[0].NativeHandle : Depth!.NativeHandle;
    public int SizeInBytes => 0; // attachment sizes are accounted on the attachment textures

    public VulkanRenderTexture(VulkanTexture[] colors, VulkanTexture? depth, bool ownsColors, bool ownsDepth)
    {
        Colors = colors;
        Depth = depth;
        this.ownsColors = ownsColors;
        this.ownsDepth = ownsDepth;
        var sizeSource = colors.Length > 0 ? colors[0] : depth!;
        Width = sizeSource.Width;
        Height = sizeSource.Height;
    }

    public void Activate(int slot)
    {
    }

    public void SetFiltering(FilteringMode mode) => Colors[0].SetFiltering(mode);

    public void SetWrapping(WrapMode mode) => Colors[0].SetWrapping(mode);

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (ownsColors)
            foreach (var color in Colors)
                color.Dispose();
        if (ownsDepth)
            Depth?.Dispose();
    }
}

/// <summary>Caches immutable sampler objects by (filtering, wrapping, mip count).</summary>
internal sealed unsafe class VulkanSamplerCache : IDisposable
{
    private readonly VulkanContext ctx;
    private readonly Dictionary<(FilteringMode, WrapMode, bool mips), VkSampler> samplers = new();

    public VulkanSamplerCache(VulkanContext ctx)
    {
        this.ctx = ctx;
    }

    public VkSampler Get(FilteringMode filtering, WrapMode wrapping, bool mips)
    {
        if (samplers.TryGetValue((filtering, wrapping, mips), out var sampler))
            return sampler;
        var filter = filtering == FilteringMode.Linear ? Filter.Linear : Filter.Nearest;
        var address = wrapping switch
        {
            WrapMode.ClampToEdge => SamplerAddressMode.ClampToEdge,
            WrapMode.ClampToBorder => SamplerAddressMode.ClampToBorder,
            WrapMode.MirroredRepeat => SamplerAddressMode.MirroredRepeat,
            _ => SamplerAddressMode.Repeat,
        };
        var info = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = filter,
            MinFilter = filter,
            MipmapMode = filtering == FilteringMode.Linear ? SamplerMipmapMode.Linear : SamplerMipmapMode.Nearest,
            AddressModeU = address,
            AddressModeV = address,
            AddressModeW = address,
            MinLod = 0,
            MaxLod = mips ? Vk.LodClampNone : 0.25f,
            BorderColor = BorderColor.FloatOpaqueBlack,
        };
        VulkanContext.Check(ctx.vk.CreateSampler(ctx.Device, in info, null, out sampler), "sampler");
        samplers[(filtering, wrapping, mips)] = sampler;
        return sampler;
    }

    public void Dispose()
    {
        foreach (var sampler in samplers.Values)
            ctx.vk.DestroySampler(ctx.Device, sampler, null);
        samplers.Clear();
    }
}
