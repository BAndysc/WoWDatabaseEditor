using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using VMASharp;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace TheEngine.Vulkan;

/// <summary>
/// Owns the Vulkan instance, device, queue and the low-level services every other
/// Vulkan class needs: memory allocation, one-shot command submission (resource
/// uploads at creation time) and deferred destruction (resources disposed by the
/// engine mid-frame are destroyed only when the GPU provably finished using them).
/// </summary>
internal unsafe class VulkanContext : IDisposable
{
    public readonly Vk vk;
    public Instance Instance;
    public PhysicalDevice PhysicalDevice;
    public Device Device;
    public Queue Queue;
    public uint QueueFamily;
    /// <summary>Second queue from the same family used by <see cref="VulkanUploadQueue"/> to submit
    /// content uploads (staging copy → image) in parallel with rendering, off the render thread.
    /// Falls back to <see cref="Queue"/> when the family exposes only one queue (then the upload
    /// queue must serialize its submissions against the render thread with a lock).</summary>
    public Queue TransferQueue;
    /// <summary>Queue family that owns <see cref="TransferQueue"/>. Equals <see cref="QueueFamily"/> when
    /// the transfer queue is a second queue of the graphics family; differs when it's a separate transfer
    /// family (then uploaded images must use concurrent sharing across both families).</summary>
    public uint TransferQueueFamily;
    /// <summary>True when <see cref="TransferQueue"/> is a distinct VkQueue from <see cref="Queue"/>;
    /// false when both alias the same handle (single-queue family) and submissions must be locked.</summary>
    public bool HasDedicatedTransferQueue;
    /// <summary>Serializes queue submissions when the upload queue shares the graphics queue
    /// (<see cref="HasDedicatedTransferQueue"/> == false). With a dedicated transfer queue the two
    /// threads submit to different VkQueues and this lock is unused.</summary>
    public readonly object QueueSubmitLock = new();
    public KhrSurface SurfaceExt = null!;
    public KhrSwapchain SwapchainExt = null!;
    public ExtDebugUtils? DebugUtilsExt;
    /// <summary>Set when VK_KHR_get_surface_capabilities2 + VK_EXT_surface_maintenance1 were enabled
    /// on the instance; needed to query which present modes a swapchain can switch between.</summary>
    public KhrGetSurfaceCapabilities2? GetSurfaceCaps2Ext;
    private bool surfaceMaintenance1;
    /// <summary>True when VK_EXT_swapchain_maintenance1 is enabled: the present mode can then be
    /// switched per-present (vsync toggle without recreating the swapchain - the recreate path is
    /// fragile on MoltenVK's embedded CAMetalLayer, which stops presenting until a resize).</summary>
    public bool SupportsSwapchainMaintenance1;
    public PhysicalDeviceProperties DeviceProperties;
    /// <summary>Native VMA allocator (libvma): sub-allocates buffers and images from pooled device
    /// memory blocks instead of one vkAllocateMemory per resource. Created in PickDeviceAndCreate,
    /// owns all resource memory, destroyed before the device.</summary>
    public VmaAllocator Allocator;
    public string DeviceName = "";
    public int ValidationErrors;
    /// <summary>Back-reference to the owning backend, set after construction. Lets resources
    /// (e.g. <see cref="VulkanTexture"/>) notify the backend on disposal so it can reclaim the
    /// texture's bindless descriptor slot before the image is freed.</summary>
    public VulkanRenderBackend? Backend;

    private DebugUtilsMessengerEXT debugMessenger;
    private CommandPool oneShotPool;
    /// <summary>False when the instance/device were created elsewhere (composition GPU interop) and
    /// merely adopted: Dispose then frees only this context's own VMA allocator + one-shot pool and
    /// leaves the device/instance for their owner to destroy.</summary>
    private readonly bool ownsDevice = true;

    /// <summary>Monotonic id of the last submitted frame; deferred destruction is keyed on it.</summary>
    public ulong CurrentSubmission = 1;
    /// <summary>Highest submission the GPU has provably completed (fence-waited).</summary>
    public ulong CompletedSubmission = 0;

    private readonly List<(ulong submission, Action destroy)> pendingDestroys = new();

    public VulkanContext()
    {
        vk = Vk.GetApi();
    }

    /// <summary>Adopts an externally-created instance/device/queue (e.g. the device the Avalonia
    /// compositor GPU-interop path created and matched to the compositor's GPU). This context does
    /// not create or destroy the device/instance; it only builds the services the engine layers on
    /// top: the VMA allocator, the one-shot command pool and the deferred-destroy bookkeeping. The
    /// surface/swapchain extensions stay unset - an adopted context renders through an external
    /// present target, never a VK_KHR_surface swapchain.</summary>
    public VulkanContext(Vk api, Instance instance, PhysicalDevice physicalDevice, Device device, Queue queue, uint queueFamily)
    {
        vk = api;
        ownsDevice = false;
        Instance = instance;
        PhysicalDevice = physicalDevice;
        Device = device;
        Queue = queue;
        QueueFamily = queueFamily;
        // An adopted device exposes only the single queue the interop layer created; uploads share it.
        TransferQueue = queue;
        TransferQueueFamily = queueFamily;
        HasDedicatedTransferQueue = false;

        vk.GetPhysicalDeviceProperties(physicalDevice, out var props);
        DeviceProperties = props;
        DeviceName = Marshal.PtrToStringAnsi((nint)props.DeviceName) ?? "unknown";

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = QueueFamily,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        Check(vk.CreateCommandPool(Device, in poolInfo, null, out oneShotPool), "create one-shot pool");

        CreateAllocator();
    }

    public void CreateInstance(string[] windowRequiredExtensions)
    {
        var availableLayers = EnumerateLayers();
        var availableExtensions = EnumerateInstanceExtensions();

        // Validation layers are extremely expensive on MoltenVK (can easily 5-8x CPU
        // frame time), so default to ON only in Debug builds (development) and OFF in
        // Release. THEENGINE_VK_VALIDATION=1/0 overrides either way.
#if DEBUG
        bool enableValidation = true;
#else
        bool enableValidation = false;
#endif
        var validationEnv = Environment.GetEnvironmentVariable("THEENGINE_VK_VALIDATION");
        if (validationEnv == "1")
            enableValidation = true;
        else if (validationEnv == "0")
            enableValidation = false;

        var layers = new List<string>();
        if (enableValidation && availableLayers.Contains("VK_LAYER_KHRONOS_validation"))
            layers.Add("VK_LAYER_KHRONOS_validation");

        var extensions = new List<string>(windowRequiredExtensions);
        bool portability = availableExtensions.Contains("VK_KHR_portability_enumeration");
        if (portability)
            extensions.Add("VK_KHR_portability_enumeration");
        bool debugUtils = availableExtensions.Contains(ExtDebugUtils.ExtensionName);
        if (debugUtils)
            extensions.Add(ExtDebugUtils.ExtensionName);
        // prerequisites of the device-level VK_EXT_swapchain_maintenance1 (per-present vsync switch)
        surfaceMaintenance1 = availableExtensions.Contains(KhrGetSurfaceCapabilities2.ExtensionName)
                              && availableExtensions.Contains("VK_EXT_surface_maintenance1");
        if (surfaceMaintenance1)
        {
            extensions.Add(KhrGetSurfaceCapabilities2.ExtensionName);
            extensions.Add("VK_EXT_surface_maintenance1");
        }

        var appName = (byte*)SilkMarshal.StringToPtr("TheEngine");
        var engineName = (byte*)SilkMarshal.StringToPtr("TheEngine");
        var layersPtr = (byte**)SilkMarshal.StringArrayToPtr(layers.ToArray());
        var extensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(extensions.ToArray());

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = appName,
            PEngineName = engineName,
            ApiVersion = Vk.Version13,
        };
        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledLayerCount = (uint)layers.Count,
            PpEnabledLayerNames = layersPtr,
            EnabledExtensionCount = (uint)extensions.Count,
            PpEnabledExtensionNames = extensionsPtr,
            Flags = portability ? InstanceCreateFlags.EnumeratePortabilityBitKhr : InstanceCreateFlags.None,
        };
        Check(vk.CreateInstance(in createInfo, null, out Instance), "vkCreateInstance");

        SilkMarshal.Free((nint)appName);
        SilkMarshal.Free((nint)engineName);
        SilkMarshal.Free((nint)layersPtr);
        SilkMarshal.Free((nint)extensionsPtr);

        if (!vk.TryGetInstanceExtension(Instance, out SurfaceExt))
            throw new Exception("VK_KHR_surface not available");

        if (surfaceMaintenance1 && vk.TryGetInstanceExtension(Instance, out KhrGetSurfaceCapabilities2 caps2Ext))
            GetSurfaceCaps2Ext = caps2Ext;

        if (enableValidation && debugUtils && vk.TryGetInstanceExtension(Instance, out ExtDebugUtils dbg))
        {
            DebugUtilsExt = dbg;
            // the delegate is kept in a field so the GC can't collect it while the
            // driver holds the unmanaged function pointer
            keepAliveDebugCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
            var messengerInfo = new DebugUtilsMessengerCreateInfoEXT
            {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
                MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                PfnUserCallback = keepAliveDebugCallback.Value,
                PUserData = null,
            };
            dbg.CreateDebugUtilsMessenger(Instance, in messengerInfo, null, out debugMessenger);
        }
    }

    private PfnDebugUtilsMessengerCallbackEXT? keepAliveDebugCallback;

    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* data, void* userData)
    {
        var message = Marshal.PtrToStringAnsi((nint)data->PMessage);
        if ((severity & DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt) != 0)
        {
            ValidationErrors++;
            Console.WriteLine($"[VK ERROR] {message}");
        }
        else
            Console.WriteLine($"[vk] {message}");
        return Silk.NET.Vulkan.Vk.False;
    }

    public void PickDeviceAndCreate(SurfaceKHR surface)
    {
        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(Instance, ref deviceCount, null);
        if (deviceCount == 0)
            throw new Exception("No Vulkan devices found");
        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* pDevices = devices)
            vk.EnumeratePhysicalDevices(Instance, ref deviceCount, pDevices);

        // Pick the best suitable device (Vulkan 1.3 + a graphics+present family), preferring a discrete
        // GPU over integrated/virtual/CPU.
        int bestScore = -1;
        foreach (var candidate in devices)
        {
            var props2 = new PhysicalDeviceProperties2 { SType = StructureType.PhysicalDeviceProperties2 };
            vk.GetPhysicalDeviceProperties2(candidate, &props2);
            var props = props2.Properties;
            if (props.ApiVersion < Vk.Version13)
                continue;
            if (!TryFindQueueFamily(candidate, surface, out var family))
                continue;
            int score = props.DeviceType switch
            {
                PhysicalDeviceType.DiscreteGpu => 4,
                PhysicalDeviceType.IntegratedGpu => 3,
                PhysicalDeviceType.VirtualGpu => 2,
                PhysicalDeviceType.Cpu => 1,
                _ => 0,
            };
            if (score <= bestScore)
                continue;
            bestScore = score;
            PhysicalDevice = candidate;
            QueueFamily = family;
            DeviceProperties = props;
            DeviceName = Marshal.PtrToStringAnsi((nint)props.DeviceName) ?? "unknown";
        }
        if (PhysicalDevice.Handle == 0)
            throw new Exception("No Vulkan 1.3 device with graphics+present found");
        Console.WriteLine($"[vk] device: {DeviceName} (type {DeviceProperties.DeviceType}, vendorID 0x{DeviceProperties.VendorID:X4}, deviceID 0x{DeviceProperties.DeviceID:X4})");
        LogQueueFamilies(PhysicalDevice, surface);

        var availableDeviceExtensions = EnumerateDeviceExtensions(PhysicalDevice);
        var deviceExtensions = new List<string> { KhrSwapchain.ExtensionName };
        if (availableDeviceExtensions.Contains("VK_KHR_portability_subset"))
            deviceExtensions.Add("VK_KHR_portability_subset");

        // VK_EXT_swapchain_maintenance1: per-present present-mode switching (vsync toggle without
        // swapchain recreation). Requires its instance-level prerequisites and the feature bit.
        if (surfaceMaintenance1 && availableDeviceExtensions.Contains("VK_EXT_swapchain_maintenance1"))
        {
            var maintQuery = new PhysicalDeviceSwapchainMaintenance1FeaturesEXT
            {
                SType = StructureType.PhysicalDeviceSwapchainMaintenance1FeaturesExt,
            };
            var features2Query = new PhysicalDeviceFeatures2
            {
                SType = StructureType.PhysicalDeviceFeatures2,
                PNext = &maintQuery,
            };
            vk.GetPhysicalDeviceFeatures2(PhysicalDevice, &features2Query);
            if (maintQuery.SwapchainMaintenance1)
            {
                deviceExtensions.Add("VK_EXT_swapchain_maintenance1");
                SupportsSwapchainMaintenance1 = true;
            }
        }

        // Get a dedicated transfer queue so uploads run parallel to rendering. Preferred: a separate
        // transfer-capable family (needed when every family exposes only one queue). Fallback: a second
        // queue in the graphics family. Otherwise share the graphics queue. The cross-family case puts
        // uploaded images in concurrent sharing mode; ordering always uses the upload timeline semaphore.
        var priorities = stackalloc float[2] { 1f, 1f };
        var queueInfos = stackalloc DeviceQueueCreateInfo[2];
        uint queueInfoCount;
        if (TryFindTransferFamily(PhysicalDevice, QueueFamily, out uint transferFamily))
        {
            HasDedicatedTransferQueue = true;
            TransferQueueFamily = transferFamily;
            queueInfos[0] = new DeviceQueueCreateInfo { SType = StructureType.DeviceQueueCreateInfo, QueueFamilyIndex = QueueFamily, QueueCount = 1, PQueuePriorities = priorities };
            queueInfos[1] = new DeviceQueueCreateInfo { SType = StructureType.DeviceQueueCreateInfo, QueueFamilyIndex = transferFamily, QueueCount = 1, PQueuePriorities = priorities };
            queueInfoCount = 2;
        }
        else if (GetQueueFamilyQueueCount(PhysicalDevice, QueueFamily) >= 2)
        {
            HasDedicatedTransferQueue = true;
            TransferQueueFamily = QueueFamily;
            queueInfos[0] = new DeviceQueueCreateInfo { SType = StructureType.DeviceQueueCreateInfo, QueueFamilyIndex = QueueFamily, QueueCount = 2, PQueuePriorities = priorities };
            queueInfoCount = 1;
        }
        else
        {
            HasDedicatedTransferQueue = false;
            TransferQueueFamily = QueueFamily;
            queueInfos[0] = new DeviceQueueCreateInfo { SType = StructureType.DeviceQueueCreateInfo, QueueFamilyIndex = QueueFamily, QueueCount = 1, PQueuePriorities = priorities };
            queueInfoCount = 1;
        }

        vk.GetPhysicalDeviceFeatures(PhysicalDevice, out var supported);
        var enabledFeatures = new PhysicalDeviceFeatures
        {
            RobustBufferAccess = supported.RobustBufferAccess,
            FillModeNonSolid = supported.FillModeNonSolid,
            SamplerAnisotropy = supported.SamplerAnisotropy,
            IndependentBlend = supported.IndependentBlend,
            DrawIndirectFirstInstance = true
        };
        var features13 = new PhysicalDeviceVulkan13Features
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            DynamicRendering = true,
            Synchronization2 = true,
            // required in core 1.3; shaderc/glslang compiles `discard` to OpDemoteToHelperInvocation
            ShaderDemoteToHelperInvocation = true,
        };

        // descriptor indexing for the bindless texture array (set 2): a single
        // CombinedImageSampler binding with a large descriptor count, indexed dynamically
        // per-material and written without waiting for the GPU to be idle. Without
        // UpdateAfterBindPool, MoltenVK caps a single binding at
        // maxPerStageDescriptorSamplers (16) / maxDescriptorSetSamplers (80); with it,
        // the cap rises to maxPerStageDescriptorUpdateAfterBindSamplers (1024).
        var features12 = new PhysicalDeviceVulkan12Features
        {
            SType = StructureType.PhysicalDeviceVulkan12Features,
            PNext = &features13,
            ShaderSampledImageArrayNonUniformIndexing = true,
            DescriptorBindingPartiallyBound = true,
            DescriptorBindingSampledImageUpdateAfterBind = true,
            // VulkanUploadQueue uses a timeline semaphore to order content uploads (transfer queue)
            // before the graphics queue first samples the uploaded image.
            TimelineSemaphore = true,
            // set 3 (GameSet3, app-registered global buffers): rewritten in place when a global
            // buffer grows mid-session, while still bound across the frame's draws - needs its
            // own update-after-bind feature bit, distinct from the sampled-image one above.
            DescriptorBindingStorageBufferUpdateAfterBind = true,
        };

        var maintFeatures = new PhysicalDeviceSwapchainMaintenance1FeaturesEXT
        {
            SType = StructureType.PhysicalDeviceSwapchainMaintenance1FeaturesExt,
            PNext = &features12,
            SwapchainMaintenance1 = true,
        };

        var extensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions.ToArray());
        var deviceInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = SupportsSwapchainMaintenance1 ? &maintFeatures : (void*)&features12,
            QueueCreateInfoCount = queueInfoCount,
            PQueueCreateInfos = queueInfos,
            EnabledExtensionCount = (uint)deviceExtensions.Count,
            PpEnabledExtensionNames = extensionsPtr,
            PEnabledFeatures = &enabledFeatures,
        };
        Check(vk.CreateDevice(PhysicalDevice, in deviceInfo, null, out Device), "vkCreateDevice");
        SilkMarshal.Free((nint)extensionsPtr);

        vk.GetDeviceQueue(Device, QueueFamily, 0, out Queue);
        if (HasDedicatedTransferQueue)
            vk.GetDeviceQueue(Device, TransferQueueFamily, TransferQueueFamily == QueueFamily ? 1u : 0u, out TransferQueue);
        else
            TransferQueue = Queue;
        Console.WriteLine($"[vk] upload path: {(!HasDedicatedTransferQueue ? "shared graphics queue" : TransferQueueFamily != QueueFamily ? $"dedicated transfer queue (family {TransferQueueFamily}, graphics family {QueueFamily})" : "dedicated transfer queue (2nd queue in graphics family)")}");

        if (!vk.TryGetDeviceExtension(Instance, Device, out SwapchainExt))
            throw new Exception("VK_KHR_swapchain not available");

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = QueueFamily,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        Check(vk.CreateCommandPool(Device, in poolInfo, null, out oneShotPool), "create one-shot pool");

        CreateAllocator();
    }

    /// <summary>
    /// Creates the native VMA allocator. The prebuilt libvma is not linked against Vulkan, so it
    /// must be handed the loader entry points: we pass vkGetInstanceProcAddr/vkGetDeviceProcAddr
    /// (fetched through Silk.NET) and let VMA dynamically resolve the rest. VMA copies the function
    /// table during vmaCreateAllocator, so a stack VmaVulkanFunctions is fine.
    /// </summary>
    private void CreateAllocator()
    {
        var gipaName = (byte*)SilkMarshal.StringToPtr("vkGetInstanceProcAddr");
        var gdpaName = (byte*)SilkMarshal.StringToPtr("vkGetDeviceProcAddr");
        var gipa = vk.GetInstanceProcAddr(Instance, gipaName);
        var gdpa = vk.GetInstanceProcAddr(Instance, gdpaName);
        SilkMarshal.Free((nint)gipaName);
        SilkMarshal.Free((nint)gdpaName);

        var fns = new VmaVulkanFunctions
        {
            vkGetInstanceProcAddr = (delegate* unmanaged<Instance, byte*, IntPtr>)gipa.Handle,
            vkGetDeviceProcAddr = (delegate* unmanaged<Device, byte*, IntPtr>)gdpa.Handle,
        };
        var createInfo = new VmaAllocatorCreateInfo
        {
            physicalDevice = PhysicalDevice,
            device = Device,
            instance = Instance,
            // VMA requires >= 1.1 for the dedicated-allocation / bind-memory2 paths; engine is 1.3.
            vulkanApiVersion = Vk.Version13,
            pVulkanFunctions = &fns,
        };
        VmaAllocator allocator;
        Check(Vma.CreateAllocator(&createInfo, &allocator), "vmaCreateAllocator");
        Allocator = allocator;
    }

    private uint GetQueueFamilyQueueCount(PhysicalDevice device, uint family)
    {
        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pFamilies);
        return family < count ? families[family].QueueCount : 1;
    }

    /// <summary>Finds a queue family (other than <paramref name="graphicsFamily"/>) usable for transfer
    /// uploads, preferring a dedicated transfer-only family over a compute/graphics one (better async).
    /// Graphics- and compute-capable families implicitly support transfer per the Vulkan spec.</summary>
    private bool TryFindTransferFamily(PhysicalDevice device, uint graphicsFamily, out uint family)
    {
        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pFamilies);
        // dedicated transfer family (transfer, but not graphics/compute) - least likely to contend
        for (uint i = 0; i < count; i++)
        {
            var flags = families[i].QueueFlags;
            if (i != graphicsFamily && families[i].QueueCount > 0
                && (flags & QueueFlags.TransferBit) != 0
                && (flags & (QueueFlags.GraphicsBit | QueueFlags.ComputeBit)) == 0)
            {
                family = i;
                return true;
            }
        }
        // any other family that can transfer (graphics/compute imply transfer capability)
        for (uint i = 0; i < count; i++)
        {
            var flags = families[i].QueueFlags;
            if (i != graphicsFamily && families[i].QueueCount > 0
                && (flags & (QueueFlags.TransferBit | QueueFlags.GraphicsBit | QueueFlags.ComputeBit)) != 0)
            {
                family = i;
                return true;
            }
        }
        family = 0;
        return false;
    }

    private void LogQueueFamilies(PhysicalDevice device, SurfaceKHR surface)
    {
        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pFamilies);
        for (uint i = 0; i < count; i++)
        {
            SurfaceExt.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var present);
            Console.WriteLine($"[vk]   queue family {i}: count={families[i].QueueCount} flags={families[i].QueueFlags} present={present}");
        }
    }

    private bool TryFindQueueFamily(PhysicalDevice device, SurfaceKHR surface, out uint family)
    {
        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, pFamilies);
        for (uint i = 0; i < count; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) == 0)
                continue;
            SurfaceExt.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupported);
            if (presentSupported)
            {
                family = i;
                return true;
            }
        }
        family = 0;
        return false;
    }

    /// <summary>Records and synchronously submits a one-off command buffer (resource uploads at creation time).</summary>
    public void OneShot(Action<CommandBuffer> record)
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = oneShotPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        Check(vk.AllocateCommandBuffers(Device, in allocInfo, out var cmd), "one-shot alloc");
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        vk.BeginCommandBuffer(cmd, in beginInfo);
        record(cmd);
        vk.EndCommandBuffer(cmd);

        var cmdInfo = new CommandBufferSubmitInfo { SType = StructureType.CommandBufferSubmitInfo, CommandBuffer = cmd };
        var submit = new SubmitInfo2 { SType = StructureType.SubmitInfo2, CommandBufferInfoCount = 1, PCommandBufferInfos = &cmdInfo };
        Check(vk.QueueSubmit2(Queue, 1, in submit, default), "one-shot submit");
        vk.QueueWaitIdle(Queue);
        vk.FreeCommandBuffers(Device, oneShotPool, 1, in cmd);
    }

    /// <summary>Destroys the resource once every submission that may reference it has completed.</summary>
    public void DestroyLater(Action destroy)
    {
        lock (pendingDestroys)
            pendingDestroys.Add((CurrentSubmission, destroy));
    }

    /// <summary>Number of deferred destroy actions executed in the most recent ProcessPendingDestroys (profiling).</summary>
    public int LastDestroyedCount { get; private set; }
    /// <summary>Number of deferred destroy actions still queued after the most recent ProcessPendingDestroys (profiling).</summary>
    public int PendingDestroyQueueLength { get; private set; }

    public void ProcessPendingDestroys()
    {
        lock (pendingDestroys)
        {
            int destroyed = 0;
            for (int i = pendingDestroys.Count - 1; i >= 0; i--)
            {
                if (pendingDestroys[i].submission <= CompletedSubmission)
                {
                    pendingDestroys[i].destroy();
                    pendingDestroys.RemoveAt(i);
                    destroyed++;
                }
            }
            LastDestroyedCount = destroyed;
            PendingDestroyQueueLength = pendingDestroys.Count;
        }
    }

    public void FlushAllPendingDestroys()
    {
        vk.DeviceWaitIdle(Device);
        CompletedSubmission = CurrentSubmission;
        ProcessPendingDestroys();
    }

    private HashSet<string> EnumerateLayers()
    {
        uint count = 0;
        vk.EnumerateInstanceLayerProperties(ref count, null);
        var props = new LayerProperties[count];
        fixed (LayerProperties* pProps = props)
            vk.EnumerateInstanceLayerProperties(ref count, pProps);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].LayerName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    private HashSet<string> EnumerateInstanceExtensions()
    {
        uint count = 0;
        vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, null);
        var props = new ExtensionProperties[count];
        fixed (ExtensionProperties* pProps = props)
            vk.EnumerateInstanceExtensionProperties((byte*)null, ref count, pProps);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].ExtensionName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    private HashSet<string> EnumerateDeviceExtensions(PhysicalDevice device)
    {
        uint count = 0;
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, null);
        var props = new ExtensionProperties[count];
        fixed (ExtensionProperties* pProps = props)
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, pProps);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].ExtensionName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    public static void Check(Result result, string what)
    {
        if (result != Result.Success)
            throw new Exception($"{what} failed: {result}");
    }

    public void Dispose()
    {
        FlushAllPendingDestroys();
        Vma.DestroyAllocator(Allocator);
        vk.DestroyCommandPool(Device, oneShotPool, null);
        // an adopted device/instance is owned by the composition-interop context; only its owner destroys it
        if (!ownsDevice)
            return;
        vk.DestroyDevice(Device, null);
        if (debugMessenger.Handle != 0)
            DebugUtilsExt?.DestroyDebugUtilsMessenger(Instance, debugMessenger, null);
        vk.DestroyInstance(Instance, null);
        if (keepAliveDebugCallback.HasValue)
        {
            keepAliveDebugCallback.Value.Dispose();
        }
    }
}
