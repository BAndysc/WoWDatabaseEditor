using Silk.NET.Vulkan;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace TheEngine.Vulkan;

/// <summary>
/// Abstracts where the engine's final "swapchain pass" renders and how that image is handed off.
/// Two implementations exist: <see cref="KhrSwapchainPresentTarget"/> presents to a real
/// VK_KHR_surface swapchain (standalone window), while <see cref="ExternalImagePresentTarget"/>
/// renders into an image owned by the Avalonia compositor GPU-interop path (no acquire, no present -
/// the interop wrapper that brackets the frame does the layout work and hands the image to the
/// compositor). The backend's BeginFrame/EndFrame/TransitionSwapchainImage and the command list's
/// swapchain pass go through this interface instead of touching a swapchain directly.
/// </summary>
internal interface IVulkanPresentTarget : IDisposable
{
    Extent2D Extent { get; }
    Format Format { get; }

    /// <summary>Layout the rendered image must be left in at end of frame: PresentSrcKhr for KHR;
    /// ColorAttachmentOptimal for external (the interop wrapper transitions it to TransferSrc).</summary>
    ImageLayout FinalLayout { get; }

    /// <summary>Whether the final swapchain pass must vertically flip its output. The engine renders
    /// top-down (negative-height viewport), which a KHR swapchain presents correctly; the Avalonia
    /// compositor samples the imported image with the opposite Y origin, so the external target flips.</summary>
    bool FlipY { get; }

    /// <summary>Semaphore the frame submit must wait on (default/handle==0 means none).
    /// Valid after <see cref="Acquire"/> for the current frame.</summary>
    VkSemaphore WaitSemaphore { get; }

    /// <summary>Semaphore the frame submit must signal (default/handle==0 means none).
    /// Valid after <see cref="Acquire"/> for the current frame.</summary>
    VkSemaphore SignalSemaphore { get; }

    /// <summary>Whether present pacing can be chosen at all. True only for the KHR swapchain
    /// (Fifo vs Immediate); the external target is paced by the Avalonia compositor, which
    /// effectively vsyncs every frame and offers no way to opt out.</summary>
    bool SupportsVSyncControl { get; }

    /// <summary>Desired vsync state. KHR applies it on the next <see cref="EnsureSize"/> (swapchain
    /// recreate); the external target ignores the setter and always reports true.</summary>
    bool VSync { get; set; }

    /// <summary>Whether presents are currently display-paced (Fifo) AND <see cref="ThrottlePresentQueue"/>
    /// can actually throttle - the condition for running the low-latency wait and its pacing telemetry.
    /// False for the external target (the compositor paces; nothing to wait on).</summary>
    bool CanThrottlePresentQueue { get; }

    /// <summary>Match the target to the desired size (KHR recreate-on-resize; external no-op).</summary>
    void EnsureSize(uint width, uint height);

    /// <summary>Selects the image to render into this frame. Returns false when none is available
    /// (minimized window / no external image set). KHR signals <paramref name="imageAvailable"/>.</summary>
    bool Acquire(VkSemaphore imageAvailable, out uint imageIndex);

    Image GetImage(uint imageIndex);
    ImageView GetView(uint imageIndex);
    ImageLayout GetLayout(uint imageIndex);
    void SetLayout(uint imageIndex, ImageLayout layout);

    /// <summary>Hand the rendered image to the consumer (KHR: vkQueuePresentKHR; external: no-op).</summary>
    void Present(uint imageIndex);

    /// <summary>Blocks until at most <paramref name="maxPendingPresents"/> queued presents remain
    /// undisplayed (VK_KHR_present_wait). 0 = wait until the most recent present reached the display
    /// (lowest latency, serializes the frame to the refresh cadence); 1 = allow one queued present
    /// (keeps cross-frame CPU/GPU pipelining). No-op when present_wait is unavailable, vsync is off,
    /// or presentation is externally paced.</summary>
    void ThrottlePresentQueue(int maxPendingPresents);
}

/// <summary>Present target backed by a real VK_KHR_surface swapchain (standalone window mode).</summary>
internal sealed unsafe class KhrSwapchainPresentTarget : IVulkanPresentTarget
{
    private readonly VulkanContext ctx;
    private readonly VulkanSwapchain swapchain;
    private uint desiredWidth, desiredHeight;

    public KhrSwapchainPresentTarget(VulkanContext ctx, SurfaceKHR surface, uint width, uint height)
    {
        this.ctx = ctx;
        desiredWidth = width;
        desiredHeight = height;
        swapchain = new VulkanSwapchain(ctx, surface, width, height);
    }

    public Extent2D Extent => swapchain.Extent;
    public Format Format => swapchain.Format;
    public ImageLayout FinalLayout => ImageLayout.PresentSrcKhr;
    public bool FlipY => false;
    public VkSemaphore WaitSemaphore { get; private set; }
    public VkSemaphore SignalSemaphore { get; private set; }

    public bool SupportsVSyncControl => true;

    /// <summary>With VK_EXT_swapchain_maintenance1 the new mode is simply requested at the next
    /// present. Without it the setter only flags the change and the swapchain is recreated at the
    /// next EnsureSize (start of frame, before acquire - never mid-frame). The recreate fallback is
    /// known-fragile on MoltenVK when the surface is an embedded CAMetalLayer (Avalonia native
    /// panel): the layer can stop presenting until a resize forces another recreate.</summary>
    public bool VSync
    {
        get => swapchain.VSync;
        set
        {
            if (swapchain.VSync == value)
                return;
            swapchain.VSync = value;
            if (swapchain.SwitchableModes.Length == 0)
                vsyncChanged = true;
        }
    }
    private bool vsyncChanged;

    public void EnsureSize(uint width, uint height)
    {
        desiredWidth = width;
        desiredHeight = height;
        if (vsyncChanged || swapchain.Extent.Width != width || swapchain.Extent.Height != height)
        {
            vsyncChanged = false;
            swapchain.Recreate(width, height);
        }
    }

    public bool Acquire(VkSemaphore imageAvailable, out uint imageIndex)
    {
        imageIndex = 0;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            uint idx = 0;
            var result = ctx.SwapchainExt.AcquireNextImage(ctx.Device, swapchain.Swapchain, ulong.MaxValue, imageAvailable, default, ref idx);
            if (result == Result.ErrorOutOfDateKhr)
            {
                swapchain.Recreate(desiredWidth, desiredHeight);
                continue;
            }
            if (result is Result.Success or Result.SuboptimalKhr)
            {
                imageIndex = idx;
                WaitSemaphore = imageAvailable;
                SignalSemaphore = swapchain.RenderFinished[idx];
                return true;
            }
            return false;
        }
        return false;
    }

    public Image GetImage(uint imageIndex) => swapchain.Images[imageIndex];
    public ImageView GetView(uint imageIndex) => swapchain.Views[imageIndex];
    public ImageLayout GetLayout(uint imageIndex) => swapchain.Layouts[imageIndex];
    public void SetLayout(uint imageIndex, ImageLayout layout) => swapchain.Layouts[imageIndex] = layout;

    // ---- scheduled presents (VK_GOOGLE_display_timing) ----
    // With plain FIFO the presentation engine only learns about a frame when it is submitted, so on
    // adaptive-refresh displays (macOS ProMotion) the panel keeps re-guessing the cadence: drawables
    // are held back unpredictably, QueueSubmit blocks on nextDrawable and the frame rate collapses
    // to ~45 fps even though the frame fits in a refresh. Giving every present an explicit desired
    // display time (MoltenVK: presentDrawable:atTime:) hands CoreAnimation a plannable schedule.
    // The schedule advances by one refresh period per present; when the engine falls behind, it
    // reanchors to "now + margin" (the present then lands on the next reachable vblank).
    private long nextDesiredPresentNs;
    private ulong refreshPeriodNs;
    private int refreshPeriodRequery;
    private uint presentTimingId;

    private ulong NextScheduledPresentTimeNs()
    {
        if (!ctx.SupportsDisplayTiming || !swapchain.PacedByVBlank)
        {
            nextDesiredPresentNs = 0;
            return 0;
        }
        // the refresh period can change without a swapchain recreate (window moved to another
        // monitor), so re-query it once in a while
        if (refreshPeriodNs == 0 || --refreshPeriodRequery <= 0)
        {
            RefreshCycleDurationGOOGLE cycle;
            if (ctx.GetRefreshCycleDurationGoogle(ctx.Device, swapchain.Swapchain, &cycle) == Result.Success)
            {
                if (cycle.RefreshDuration != refreshPeriodNs)
                    Console.WriteLine($"[vk] display refresh period: {cycle.RefreshDuration / 1e6:0.00}ms ({1e9 / cycle.RefreshDuration:0.#}Hz)");
                refreshPeriodNs = cycle.RefreshDuration;
            }
            refreshPeriodRequery = 240;
        }
        if (refreshPeriodNs == 0)
            return 0;
        long period = (long)refreshPeriodNs;
        long now = (long)(System.Diagnostics.Stopwatch.GetTimestamp() * (1_000_000_000.0 / System.Diagnostics.Stopwatch.Frequency));
        long t = nextDesiredPresentNs + period;
        // margin: CoreAnimation needs the request a bit ahead of the target to hit it
        long min = now + period / 3;
        if (nextDesiredPresentNs == 0 || t > now + 3 * period)
            t = min; // first frame, or schedule far ahead (clock/estimate drift): re-anchor
        else if (t < min)
            // running slower than the refresh rate: skip whole slots but KEEP the schedule's phase,
            // so a too-slow engine settles on a clean divisor (120Hz panel -> steady 60) instead of
            // re-anchoring at an arbitrary phase every frame (which reads as judder)
            t += (min - t + period - 1) / period * period;
        nextDesiredPresentNs = t;
        return (ulong)t;
    }

    public void Present(uint imageIndex)
    {
        var swapchainHandle = swapchain.Swapchain;
        var idx = imageIndex;
        var renderFinished = swapchain.RenderFinished[imageIndex];
        // per-present vsync (VK_EXT_swapchain_maintenance1): request the mode matching the current
        // VSync wish with every present - switching costs nothing when the mode didn't change
        var presentMode = swapchain.SwitchableModes.Length > 0 ? swapchain.CurrentSwitchableMode() : default;
        var presentModeInfo = new SwapchainPresentModeInfoEXT
        {
            SType = StructureType.SwapchainPresentModeInfoExt,
            SwapchainCount = 1,
            PPresentModes = &presentMode,
        };
        void* pNext = swapchain.SwitchableModes.Length > 0 ? &presentModeInfo : null;
        ulong desiredPresentTime = NextScheduledPresentTimeNs();
        var presentTime = new PresentTimeGOOGLE
        {
            PresentID = ++presentTimingId,
            DesiredPresentTime = desiredPresentTime,
        };
        var presentTimesInfo = new PresentTimesInfoGOOGLE
        {
            SType = StructureType.PresentTimesInfoGoogle,
            PNext = pNext,
            SwapchainCount = 1,
            PTimes = &presentTime,
        };
        if (desiredPresentTime != 0)
            pNext = &presentTimesInfo;
        ulong presentId = swapchain.LastPresentId + 1;
        var presentIdInfo = new PresentIdKHR
        {
            SType = StructureType.PresentIDKhr,
            PNext = pNext,
            SwapchainCount = 1,
            PPresentIds = &presentId,
        };
        if (ctx.SupportsPresentWait)
            pNext = &presentIdInfo;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            PNext = pNext,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &renderFinished,
            SwapchainCount = 1,
            PSwapchains = &swapchainHandle,
            PImageIndices = &idx,
        };
        var result = ctx.SwapchainExt.QueuePresent(ctx.Queue, in presentInfo);
        if (ctx.SupportsPresentWait && result is Result.Success or Result.SuboptimalKhr)
            swapchain.LastPresentId = presentId;
        if (result is not (Result.Success or Result.SuboptimalKhr or Result.ErrorOutOfDateKhr))
            VulkanContext.Check(result, "present");
    }

    public bool CanThrottlePresentQueue => ctx.SupportsPresentWait && swapchain.PacedByVBlank;

    public void ThrottlePresentQueue(int maxPendingPresents)
    {
        if (!CanThrottlePresentQueue)
            return;
        ulong last = swapchain.LastPresentId;
        if (last <= (ulong)maxPendingPresents)
            return;
        // 250ms timeout: never hang on a stalled presentation engine (occluded window, display
        // sleep) - timing out just means the throttle degrades to today's queue-depth pacing
        var result = ctx.PresentWaitExt!.WaitForPresent(ctx.Device, swapchain.Swapchain, last - (ulong)maxPendingPresents, 250_000_000);
        if (result is not (Result.Success or Result.Timeout or Result.SuboptimalKhr or Result.ErrorOutOfDateKhr))
            VulkanContext.Check(result, "vkWaitForPresentKHR");
    }

    public void Dispose() => swapchain.Dispose();
}

/// <summary>Present target that renders into an image supplied each frame by the Avalonia
/// composition GPU-interop driver. The interop wrapper (BeginDraw/Present) transitions the image and
/// hands it to the compositor; here there is no acquire semaphore, no present, and the final layout
/// is left at ColorAttachmentOptimal. The tracked layout is reset to Undefined when the image is set,
/// matching the interop BeginDraw transition (contents discarded).</summary>
internal sealed class ExternalImagePresentTarget : IVulkanPresentTarget
{
    private Image image;
    private ImageView view;
    private Extent2D extent;
    private Format format;
    private ImageLayout layout;
    private bool hasImage;

    /// <summary>Called by the interop driver before the engine frame, after BeginDraw produced the image.</summary>
    public void SetImage(Image image, ImageView view, Extent2D extent, Format format)
    {
        this.image = image;
        this.view = view;
        this.extent = extent;
        this.format = format;
        layout = ImageLayout.Undefined;
        hasImage = true;
    }

    public Extent2D Extent => extent;
    public Format Format => format;
    public ImageLayout FinalLayout => ImageLayout.ColorAttachmentOptimal;
    public bool FlipY => true;
    public VkSemaphore WaitSemaphore => default;
    public VkSemaphore SignalSemaphore => default;

    // the compositor paces presentation - effectively always vsynced, nothing to control here
    public bool SupportsVSyncControl => false;
    public bool VSync { get => true; set { } }
    public bool CanThrottlePresentQueue => false;

    public void EnsureSize(uint width, uint height) { }

    public bool Acquire(VkSemaphore imageAvailable, out uint imageIndex)
    {
        imageIndex = 0;
        return hasImage;
    }

    public Image GetImage(uint imageIndex) => image;
    public ImageView GetView(uint imageIndex) => view;
    public ImageLayout GetLayout(uint imageIndex) => layout;
    public void SetLayout(uint imageIndex, ImageLayout layout) => this.layout = layout;
    public void Present(uint imageIndex) { }
    public void ThrottlePresentQueue(int maxPendingPresents) { }
    public void Dispose() { }
}
