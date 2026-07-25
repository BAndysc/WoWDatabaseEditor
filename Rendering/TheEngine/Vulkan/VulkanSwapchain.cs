using Silk.NET.Vulkan;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace TheEngine.Vulkan;

/// <summary>
/// Owns the swapchain, its image views and the per-image renderFinished semaphores.
/// Recreated when the surface size changes (acquire returns out-of-date).
/// </summary>
internal unsafe class VulkanSwapchain : IDisposable
{
    private readonly VulkanContext ctx;
    private readonly SurfaceKHR surface;

    public SwapchainKHR Swapchain;
    /// <summary>Desired present pacing: true = Fifo (vsync), false = prefer Immediate/Mailbox.
    /// With VK_EXT_swapchain_maintenance1 (<see cref="SwitchableModes"/> non-empty) it takes effect
    /// on the next present; otherwise on the next <see cref="Recreate"/>. THEENGINE_VK_PRESENT
    /// pins a mode and disables switching either way.</summary>
    public bool VSync;
    /// <summary>Present modes this swapchain may switch between per-present (declared at creation,
    /// VK_EXT_swapchain_maintenance1). Empty when switching isn't available and a vsync change
    /// needs a full recreate.</summary>
    public PresentModeKHR[] SwitchableModes = Array.Empty<PresentModeKHR>();
    public Image[] Images = Array.Empty<Image>();
    public ImageView[] Views = Array.Empty<ImageView>();
    /// <summary>One per swapchain image: signaled by the frame submit, waited on by present.</summary>
    public VkSemaphore[] RenderFinished = Array.Empty<VkSemaphore>();
    /// <summary>Tracked layout per swapchain image (acquire leaves them in their presented/undefined state).</summary>
    public ImageLayout[] Layouts = Array.Empty<ImageLayout>();
    public Format Format;
    public Extent2D Extent;
    /// <summary>Id of the last present tagged with VK_KHR_present_id (0 = none yet). Ids must be
    /// monotonically increasing per swapchain, so the counter restarts when the swapchain is
    /// recreated - vkWaitForPresentKHR targets must never mix ids across swapchain handles.</summary>
    public ulong LastPresentId;
    /// <summary>The present mode the swapchain was created with (per-present switching may override
    /// it - see <see cref="CurrentSwitchableMode"/>).</summary>
    public PresentModeKHR CreatedPresentMode;

    /// <summary>Whether presents are currently paced by the display (Fifo) - the condition for the
    /// low-latency present-wait throttle. Tracks the actual mode, not the <see cref="VSync"/> wish:
    /// they diverge when THEENGINE_VK_PRESENT pins a mode and when vsync-off falls back to Fifo
    /// because neither Immediate nor Mailbox is supported.</summary>
    public bool PacedByVBlank => SwitchableModes.Length > 0
        ? CurrentSwitchableMode() == PresentModeKHR.FifoKhr
        : CreatedPresentMode == PresentModeKHR.FifoKhr;

    public VulkanSwapchain(VulkanContext ctx, SurfaceKHR surface, uint width, uint height)
    {
        this.ctx = ctx;
        this.surface = surface;
        Create(width, height);
    }

    public void Recreate(uint width, uint height)
    {
        ctx.vk.DeviceWaitIdle(ctx.Device);
        var old = Swapchain;
        DestroyViewsAndSemaphores();
        Create(width, height, old);
        if (old.Handle != 0)
            ctx.SwapchainExt.DestroySwapchain(ctx.Device, old, null);
    }

    private void Create(uint width, uint height, SwapchainKHR oldSwapchain = default)
    {
        LastPresentId = 0;
        ctx.SurfaceExt.GetPhysicalDeviceSurfaceCapabilities(ctx.PhysicalDevice, surface, out var caps);

        uint formatCount = 0;
        ctx.SurfaceExt.GetPhysicalDeviceSurfaceFormats(ctx.PhysicalDevice, surface, ref formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];
        fixed (SurfaceFormatKHR* pFormats = formats)
            ctx.SurfaceExt.GetPhysicalDeviceSurfaceFormats(ctx.PhysicalDevice, surface, ref formatCount, pFormats);

        var surfaceFormat = formats[0];
        foreach (var f in formats)
        {
            if (f.Format == Silk.NET.Vulkan.Format.B8G8R8A8Unorm && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                surfaceFormat = f;
                break;
            }
        }
        Format = surfaceFormat.Format;

        Extent = caps.CurrentExtent.Width != uint.MaxValue
            ? caps.CurrentExtent
            : new Extent2D(
                Math.Clamp(width, caps.MinImageExtent.Width, caps.MaxImageExtent.Width),
                Math.Clamp(height, caps.MinImageExtent.Height, caps.MaxImageExtent.Height));

        uint imageCount = Math.Max(3, caps.MinImageCount);
        if (caps.MaxImageCount > 0 && imageCount > caps.MaxImageCount)
            imageCount = caps.MaxImageCount;

        // VSync=true means Fifo; VSync=false prefers Immediate (uncapped), then Mailbox, falling
        // back to Fifo (the only guaranteed mode) if neither is supported. THEENGINE_VK_PRESENT
        // forces a specific mode: "fifo" (vsync on), "mailbox", or "immediate".
        uint modeCount = 0;
        ctx.SurfaceExt.GetPhysicalDeviceSurfacePresentModes(ctx.PhysicalDevice, surface, ref modeCount, null);
        var supportedModes = new PresentModeKHR[modeCount];
        fixed (PresentModeKHR* pModes = supportedModes)
            ctx.SurfaceExt.GetPhysicalDeviceSurfacePresentModes(ctx.PhysicalDevice, surface, ref modeCount, pModes);
        bool Supports(PresentModeKHR m) => Array.IndexOf(supportedModes, m) >= 0;

        PresentModeKHR presentMode;
        bool pinnedByEnv = true;
        switch (Environment.GetEnvironmentVariable("THEENGINE_VK_PRESENT"))
        {
            case "fifo":
                presentMode = PresentModeKHR.FifoKhr;
                VSync = true; // keep the wish flag consistent with the pinned mode
                break;
            case "mailbox" when Supports(PresentModeKHR.MailboxKhr):
                presentMode = PresentModeKHR.MailboxKhr;
                VSync = false;
                break;
            case "immediate" when Supports(PresentModeKHR.ImmediateKhr):
                presentMode = PresentModeKHR.ImmediateKhr;
                VSync = false;
                break;
            default:
                pinnedByEnv = false;
                presentMode = VSync ? PresentModeKHR.FifoKhr
                    : Supports(PresentModeKHR.ImmediateKhr) ? PresentModeKHR.ImmediateKhr
                    : Supports(PresentModeKHR.MailboxKhr) ? PresentModeKHR.MailboxKhr
                    : PresentModeKHR.FifoKhr;
                break;
        }
        CreatedPresentMode = presentMode;

        // With VK_EXT_swapchain_maintenance1, declare every mode the vsync toggle may want up
        // front so presents can switch between them without a recreate. Only modes the surface
        // reports compatible with the initial mode may be declared.
        SwitchableModes = Array.Empty<PresentModeKHR>();
        if (!pinnedByEnv && ctx.SupportsSwapchainMaintenance1 && ctx.GetSurfaceCaps2Ext != null)
        {
            var compatible = QueryCompatiblePresentModes(presentMode);
            var switchable = new List<PresentModeKHR> { presentMode };
            foreach (var m in stackalloc[] { PresentModeKHR.FifoKhr, PresentModeKHR.ImmediateKhr, PresentModeKHR.MailboxKhr })
                if (m != presentMode && Supports(m) && compatible.Contains(m))
                    switchable.Add(m);
            bool canVsyncOn = switchable.Contains(PresentModeKHR.FifoKhr);
            bool canVsyncOff = switchable.Contains(PresentModeKHR.ImmediateKhr) || switchable.Contains(PresentModeKHR.MailboxKhr);
            if (canVsyncOn && canVsyncOff)
                SwitchableModes = switchable.ToArray();
        }

        fixed (PresentModeKHR* pSwitchable = SwitchableModes)
        {
            var switchableModesInfo = new SwapchainPresentModesCreateInfoEXT
            {
                SType = StructureType.SwapchainPresentModesCreateInfoExt,
                PresentModeCount = (uint)SwitchableModes.Length,
                PPresentModes = pSwitchable,
            };
            var info = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                PNext = SwitchableModes.Length > 0 ? &switchableModesInfo : null,
                Surface = surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = Extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = SharingMode.Exclusive,
                PreTransform = caps.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                OldSwapchain = oldSwapchain,
            };
            VulkanContext.Check(ctx.SwapchainExt.CreateSwapchain(ctx.Device, in info, null, out Swapchain), "vkCreateSwapchainKHR");
        }

        uint count = 0;
        ctx.SwapchainExt.GetSwapchainImages(ctx.Device, Swapchain, ref count, null);
        Images = new Image[count];
        fixed (Image* pImages = Images)
            ctx.SwapchainExt.GetSwapchainImages(ctx.Device, Swapchain, ref count, pImages);

        Views = new ImageView[count];
        RenderFinished = new VkSemaphore[count];
        Layouts = new ImageLayout[count];
        for (int i = 0; i < count; i++)
        {
            var viewInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = Images[i],
                ViewType = ImageViewType.Type2D,
                Format = Format,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            };
            VulkanContext.Check(ctx.vk.CreateImageView(ctx.Device, in viewInfo, null, out Views[i]), "swapchain image view");
            var semInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
            VulkanContext.Check(ctx.vk.CreateSemaphore(ctx.Device, in semInfo, null, out RenderFinished[i]), "renderFinished semaphore");
            Layouts[i] = ImageLayout.Undefined;
        }
    }

    /// <summary>Present modes the surface can switch to per-present when the swapchain was created
    /// with <paramref name="baseMode"/> (VK_EXT_surface_maintenance1 query).</summary>
    private HashSet<PresentModeKHR> QueryCompatiblePresentModes(PresentModeKHR baseMode)
    {
        var modeInfo = new SurfacePresentModeEXT
        {
            SType = StructureType.SurfacePresentModeExt,
            PresentMode = baseMode,
        };
        var surfaceInfo = new PhysicalDeviceSurfaceInfo2KHR
        {
            SType = StructureType.PhysicalDeviceSurfaceInfo2Khr,
            PNext = &modeInfo,
            Surface = surface,
        };
        var compat = new SurfacePresentModeCompatibilityEXT { SType = StructureType.SurfacePresentModeCompatibilityExt };
        var caps2 = new SurfaceCapabilities2KHR { SType = StructureType.SurfaceCapabilities2Khr, PNext = &compat };
        ctx.GetSurfaceCaps2Ext!.GetPhysicalDeviceSurfaceCapabilities2(ctx.PhysicalDevice, &surfaceInfo, &caps2);
        var modes = new PresentModeKHR[compat.PresentModeCount];
        if (compat.PresentModeCount > 0)
        {
            fixed (PresentModeKHR* pModes = modes)
            {
                compat.PPresentModes = pModes;
                ctx.GetSurfaceCaps2Ext.GetPhysicalDeviceSurfaceCapabilities2(ctx.PhysicalDevice, &surfaceInfo, &caps2);
            }
        }
        return modes.ToHashSet();
    }

    /// <summary>The mode presents should request right now, given <see cref="VSync"/> - only valid
    /// when <see cref="SwitchableModes"/> is non-empty (every returned mode is declared in it).</summary>
    public PresentModeKHR CurrentSwitchableMode()
    {
        if (VSync)
            return PresentModeKHR.FifoKhr;
        if (Array.IndexOf(SwitchableModes, PresentModeKHR.ImmediateKhr) >= 0)
            return PresentModeKHR.ImmediateKhr;
        if (Array.IndexOf(SwitchableModes, PresentModeKHR.MailboxKhr) >= 0)
            return PresentModeKHR.MailboxKhr;
        return PresentModeKHR.FifoKhr;
    }

    private void DestroyViewsAndSemaphores()
    {
        foreach (var view in Views)
            ctx.vk.DestroyImageView(ctx.Device, view, null);
        foreach (var sem in RenderFinished)
            ctx.vk.DestroySemaphore(ctx.Device, sem, null);
        Views = Array.Empty<ImageView>();
        RenderFinished = Array.Empty<VkSemaphore>();
    }

    public void Dispose()
    {
        DestroyViewsAndSemaphores();
        if (Swapchain.Handle != 0)
            ctx.SwapchainExt.DestroySwapchain(ctx.Device, Swapchain, null);
    }
}
