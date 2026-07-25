using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using Image = Silk.NET.Vulkan.Image;
using VkHandle = OpenTK.Windowing.GraphicsLibraryFramework.VkHandle;

namespace VulkanSandbox;

// M1 spike: prove the full standalone presentation path TheEngine's Vulkan backend will use:
// OpenTK (GLFW) window with no GL context -> VkSurfaceKHR -> Vulkan 1.3 device with
// dynamic rendering + synchronization2 (MoltenVK) -> swapchain -> animated clear.
// Exit code 0 = ran clean with zero validation errors.
internal static unsafe class Program
{
    private const int MaxFramesInFlight = 2;

    private static Vk vk = null!;
    private static int validationErrors;

    /// <summary>Without a Vulkan SDK install macOS has no ICD manifests, so the loader
    /// (Silk.NET.Vulkan.Loader.Native) finds no driver; point it at the bundled MoltenVK
    /// (Silk.NET.MoltenVK.Native) unless a system manifest or explicit override exists.
    /// Same logic as TheEngine.Vulkan.MoltenVkIcdFallback (this spike doesn't reference TheEngine).</summary>
    private static void SetupMoltenVkIcdFallback()
    {
        if (!OperatingSystem.IsMacOS() ||
            Environment.GetEnvironmentVariable("VK_ICD_FILENAMES") != null ||
            Environment.GetEnvironmentVariable("VK_DRIVER_FILES") != null)
            return;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] systemDirs =
        {
            Path.Combine(home, ".config/vulkan/icd.d"),
            "/etc/xdg/vulkan/icd.d",
            "/usr/local/etc/vulkan/icd.d",
            "/etc/vulkan/icd.d",
            Path.Combine(home, ".local/share/vulkan/icd.d"),
            "/usr/local/share/vulkan/icd.d",
            "/usr/share/vulkan/icd.d",
        };
        if (systemDirs.Any(dir => Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.json").Any()))
            return;

        var baseDir = AppContext.BaseDirectory;
        var manifest = new[]
        {
            Path.Combine(baseDir, "runtimes", "osx", "native", "MoltenVK_icd.json"),
            Path.Combine(baseDir, "MoltenVK_icd.json"),
        }.FirstOrDefault(File.Exists);
        if (manifest != null)
            Environment.SetEnvironmentVariable("VK_DRIVER_FILES", manifest);
    }

    public static int Main(string[] args)
    {
        long maxFrames = long.MaxValue;
        int gpuOverride = -1;
        string? screenshotPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--frames" && i + 1 < args.Length)
                maxFrames = long.Parse(args[++i]);
            else if (args[i] == "--gpu" && i + 1 < args.Length)
                gpuOverride = int.Parse(args[++i]);
            else if (args[i] == "--screenshot" && i + 1 < args.Length)
                screenshotPath = args[++i];
        }

        var windowSettings = new NativeWindowSettings
        {
            API = ContextAPI.NoAPI,
            ClientSize = new Vector2i(1280, 720),
            Title = "TheEngine - Vulkan sandbox",
        };
        using var window = new NativeWindow(windowSettings);

        SetupMoltenVkIcdFallback();
        vk = Vk.GetApi();
        // point GLFW at the loader Silk.NET loaded (shipped by Silk.NET.Vulkan.Loader.Native);
        // a second loader instance in the process rejects our instance handles
        if (vk.Context.TryGetProcAddress("vkGetInstanceProcAddr", out var vkGetInstanceProcAddr))
            GLFW.InitVulkanLoader(vkGetInstanceProcAddr);

        if (!GLFW.VulkanSupported())
        {
            Console.WriteLine("GLFW says Vulkan is not supported (loader not found).");
            return 1;
        }

        // ---- instance ----
        var availableLayers = GetInstanceLayers();
        var availableInstanceExtensions = GetInstanceExtensions();
        bool validation = availableLayers.Contains("VK_LAYER_KHRONOS_validation");

        var instanceExtensions = new List<string>(GLFW.GetRequiredInstanceExtensions());
        bool portability = availableInstanceExtensions.Contains("VK_KHR_portability_enumeration");
        if (portability)
            instanceExtensions.Add("VK_KHR_portability_enumeration");
        if (validation && availableInstanceExtensions.Contains(ExtDebugUtils.ExtensionName))
            instanceExtensions.Add(ExtDebugUtils.ExtensionName);
        Console.WriteLine($"Instance extensions: {string.Join(", ", instanceExtensions)}");
        Console.WriteLine($"Validation layer: {(validation ? "enabled" : "NOT AVAILABLE")}");

        var appName = (byte*)SilkMarshal.StringToPtr("VulkanSandbox");
        var engineName = (byte*)SilkMarshal.StringToPtr("TheEngine");
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = appName,
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = engineName,
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version13,
        };

        var instanceExtensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(instanceExtensions.ToArray());
        var layers = validation ? new[] { "VK_LAYER_KHRONOS_validation" } : Array.Empty<string>();
        var layersPtr = (byte**)SilkMarshal.StringArrayToPtr(layers);

        var instanceCreateInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            Flags = portability ? InstanceCreateFlags.EnumeratePortabilityBitKhr : InstanceCreateFlags.None,
            EnabledExtensionCount = (uint)instanceExtensions.Count,
            PpEnabledExtensionNames = instanceExtensionsPtr,
            EnabledLayerCount = (uint)layers.Length,
            PpEnabledLayerNames = layersPtr,
        };
        Check(vk.CreateInstance(&instanceCreateInfo, null, out var instance), "vkCreateInstance");
        SilkMarshal.Free((nint)instanceExtensionsPtr);
        SilkMarshal.Free((nint)layersPtr);
        SilkMarshal.Free((nint)appName);
        SilkMarshal.Free((nint)engineName);

        ExtDebugUtils? debugUtils = null;
        DebugUtilsMessengerEXT messenger = default;
        if (validation && vk.TryGetInstanceExtension(instance, out ExtDebugUtils du))
        {
            debugUtils = du;
            var messengerCreateInfo = new DebugUtilsMessengerCreateInfoEXT
            {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt |
                                  DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
                MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                              DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                              DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback),
            };
            Check(du.CreateDebugUtilsMessenger(instance, &messengerCreateInfo, null, out messenger), "vkCreateDebugUtilsMessengerEXT");
        }

        // ---- surface (created by GLFW, same path SponzaDemo will use) ----
        var surfaceResult = GLFW.CreateWindowSurface(new VkHandle(instance.Handle), window.WindowPtr, null, out var surfaceHandle);
        if ((int)surfaceResult != 0)
            throw new Exception($"glfwCreateWindowSurface failed: {surfaceResult}");
        var surface = new SurfaceKHR((ulong)surfaceHandle.Handle);

        if (!vk.TryGetInstanceExtension(instance, out KhrSurface khrSurface))
            throw new Exception("VK_KHR_surface not available");

        // ---- physical device ----
        uint deviceCount = 0;
        Check(vk.EnumeratePhysicalDevices(instance, &deviceCount, null), "vkEnumeratePhysicalDevices");
        var physicalDevices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* p = physicalDevices)
            Check(vk.EnumeratePhysicalDevices(instance, &deviceCount, p), "vkEnumeratePhysicalDevices");

        PhysicalDevice physicalDevice = default;
        uint queueFamily = 0;
        string pickedName = "";
        for (int i = 0; i < deviceCount; i++)
        {
            vk.GetPhysicalDeviceProperties(physicalDevices[i], out var props);
            var name = Marshal.PtrToStringAnsi((nint)props.DeviceName) ?? "?";
            var v = props.ApiVersion;
            Console.WriteLine($"GPU{i}: {name} (api {(v >> 22) & 0x7F}.{(v >> 12) & 0x3FF}.{v & 0xFFF})");

            if (gpuOverride >= 0 && i != gpuOverride)
                continue;
            if (physicalDevice.Handle != 0)
                continue;
            if (((v >> 22) & 0x7F) * 100 + ((v >> 12) & 0x3FF) < 103)
                continue; // need 1.3 for core dynamic rendering + sync2
            if (!TryFindQueueFamily(physicalDevices[i], khrSurface, surface, out var family))
                continue;
            physicalDevice = physicalDevices[i];
            queueFamily = family;
            pickedName = name;
        }
        if (physicalDevice.Handle == 0)
            throw new Exception("no suitable Vulkan 1.3 device with graphics+present queue found");
        Console.WriteLine($"Picked: {pickedName}, queue family {queueFamily}");

        // ---- logical device ----
        var deviceExtensions = new List<string> { KhrSwapchain.ExtensionName };
        if (GetDeviceExtensions(physicalDevice).Contains("VK_KHR_portability_subset"))
            deviceExtensions.Add("VK_KHR_portability_subset"); // mandatory to enable on MoltenVK

        var features13 = new PhysicalDeviceVulkan13Features
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            DynamicRendering = true,
            Synchronization2 = true,
        };
        float queuePriority = 1f;
        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = queueFamily,
            QueueCount = 1,
            PQueuePriorities = &queuePriority,
        };
        var deviceExtensionsPtr = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions.ToArray());
        var deviceCreateInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = &features13,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            EnabledExtensionCount = (uint)deviceExtensions.Count,
            PpEnabledExtensionNames = deviceExtensionsPtr,
        };
        Check(vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, out var device), "vkCreateDevice");
        SilkMarshal.Free((nint)deviceExtensionsPtr);
        vk.GetDeviceQueue(device, queueFamily, 0, out var queue);

        if (!vk.TryGetDeviceExtension(instance, device, out KhrSwapchain khrSwapchain))
            throw new Exception("VK_KHR_swapchain not available");

        // ---- swapchain ----
        Check(khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out var caps), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        uint formatCount = 0;
        khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];
        fixed (SurfaceFormatKHR* f = formats)
            khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, f);
        var surfaceFormat = formats.FirstOrDefault(
            f => f.Format == Format.B8G8R8A8Unorm && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr,
            formats[0]);

        GLFW.GetFramebufferSize(window.WindowPtr, out int fbWidth, out int fbHeight);
        Extent2D extent = caps.CurrentExtent.Width != uint.MaxValue
            ? caps.CurrentExtent
            : new Extent2D(
                (uint)Math.Clamp(fbWidth, (int)caps.MinImageExtent.Width, (int)caps.MaxImageExtent.Width),
                (uint)Math.Clamp(fbHeight, (int)caps.MinImageExtent.Height, (int)caps.MaxImageExtent.Height));

        uint minImageCount = caps.MinImageCount + 1;
        if (caps.MaxImageCount > 0 && minImageCount > caps.MaxImageCount)
            minImageCount = caps.MaxImageCount;

        var swapchainCreateInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = minImageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit |
                         (caps.SupportedUsageFlags & ImageUsageFlags.TransferSrcBit),
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = caps.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = PresentModeKHR.FifoKhr,
            Clipped = true,
        };
        Check(khrSwapchain.CreateSwapchain(device, &swapchainCreateInfo, null, out var swapchain), "vkCreateSwapchainKHR");
        Console.WriteLine($"Swapchain: {surfaceFormat.Format}/{surfaceFormat.ColorSpace}, {extent.Width}x{extent.Height}, FIFO");

        uint imageCount = 0;
        khrSwapchain.GetSwapchainImages(device, swapchain, &imageCount, null);
        var images = new Image[imageCount];
        fixed (Image* im = images)
            khrSwapchain.GetSwapchainImages(device, swapchain, &imageCount, im);

        var views = new ImageView[imageCount];
        for (int i = 0; i < imageCount; i++)
        {
            var viewCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = images[i],
                ViewType = ImageViewType.Type2D,
                Format = surfaceFormat.Format,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            };
            Check(vk.CreateImageView(device, &viewCreateInfo, null, out views[i]), "vkCreateImageView");
        }

        // ---- triangle pipeline (SPIR-V from shaderc, dynamic rendering, push constants) ----
        var vertSpirv = CompileGlsl(Path.Combine(AppContext.BaseDirectory, "shaders/tri.vert"));
        var fragSpirv = CompileGlsl(Path.Combine(AppContext.BaseDirectory, "shaders/tri.frag"));
        var vertModule = CreateShaderModule(device, vertSpirv);
        var fragModule = CreateShaderModule(device, fragSpirv);

        var pushRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = 16,
        };
        var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushRange,
        };
        Check(vk.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out var pipelineLayout), "vkCreatePipelineLayout");

        Pipeline trianglePipeline;
        var shaderEntry = (byte*)SilkMarshal.StringToPtr("main");
        {
            var stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.VertexBit,
                    Module = vertModule,
                    PName = shaderEntry,
                },
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.FragmentBit,
                    Module = fragModule,
                    PName = shaderEntry,
                },
            };

            var vertexBinding = new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = 6 * sizeof(float), // vec3 position + vec3 color
                InputRate = VertexInputRate.Vertex,
            };
            var vertexAttributes = stackalloc VertexInputAttributeDescription[2]
            {
                new() { Location = 0, Binding = 0, Format = Format.R32G32B32Sfloat, Offset = 0 },
                new() { Location = 1, Binding = 0, Format = Format.R32G32B32Sfloat, Offset = 12 },
            };
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &vertexBinding,
                VertexAttributeDescriptionCount = 2,
                PVertexAttributeDescriptions = vertexAttributes,
            };
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
            };
            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1,
            };
            var rasterization = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModeFlags.None,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1f,
            };
            var multisample = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };
            var blendAttachment = new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                                 ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            };
            var colorBlend = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = 1,
                PAttachments = &blendAttachment,
            };
            var dynamicStates = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
            var dynamicState = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates,
            };
            // dynamic rendering: no VkRenderPass, the pipeline is keyed by attachment formats
            var colorFormat = surfaceFormat.Format;
            var renderingCreateInfo = new PipelineRenderingCreateInfo
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = 1,
                PColorAttachmentFormats = &colorFormat,
            };
            var pipelineCreateInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &renderingCreateInfo,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterization,
                PMultisampleState = &multisample,
                PColorBlendState = &colorBlend,
                PDynamicState = &dynamicState,
                Layout = pipelineLayout,
            };
            Check(vk.CreateGraphicsPipelines(device, default, 1, &pipelineCreateInfo, null, out trianglePipeline), "vkCreateGraphicsPipelines");
        }
        SilkMarshal.Free((nint)shaderEntry);

        // ---- vertex buffer (host visible; staging comes in M2) ----
        // GL-style +y up: red on top
        var vertices = new float[]
        {
            0.0f, 0.6f, 0.0f,   1f, 0f, 0f,
            -0.6f, -0.5f, 0.0f, 0f, 1f, 0f,
            0.6f, -0.5f, 0.0f,  0f, 0f, 1f,
        };
        var bufferCreateInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(vertices.Length * sizeof(float)),
            Usage = BufferUsageFlags.VertexBufferBit,
            SharingMode = SharingMode.Exclusive,
        };
        Check(vk.CreateBuffer(device, &bufferCreateInfo, null, out var vertexBuffer), "vkCreateBuffer");
        vk.GetBufferMemoryRequirements(device, vertexBuffer, out var memoryRequirements);
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, out var memoryProperties);

        uint FindMemoryType(uint typeBits, MemoryPropertyFlags wanted)
        {
            for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
                if ((typeBits & (1u << i)) != 0 &&
                    (memoryProperties.MemoryTypes[i].PropertyFlags & wanted) == wanted)
                    return (uint)i;
            throw new Exception($"no memory type with {wanted}");
        }

        var memoryAllocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memoryRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit),
        };
        Check(vk.AllocateMemory(device, &memoryAllocateInfo, null, out var vertexMemory), "vkAllocateMemory");
        Check(vk.BindBufferMemory(device, vertexBuffer, vertexMemory, 0), "vkBindBufferMemory");
        void* mapped;
        Check(vk.MapMemory(device, vertexMemory, 0, bufferCreateInfo.Size, 0, &mapped), "vkMapMemory");
        vertices.AsSpan().CopyTo(new Span<float>(mapped, vertices.Length));
        vk.UnmapMemory(device, vertexMemory);

        // ---- screenshot readback buffer (swapchain -> host; same machinery the
        // Vulkan --replay-captures comparison will use) ----
        Silk.NET.Vulkan.Buffer screenshotBuffer = default;
        DeviceMemory screenshotMemory = default;
        void* screenshotMapped = null;
        ulong screenshotSize = (ulong)extent.Width * extent.Height * 4;
        if (screenshotPath != null)
        {
            var screenshotBufferCreateInfo = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Size = screenshotSize,
                Usage = BufferUsageFlags.TransferDstBit,
                SharingMode = SharingMode.Exclusive,
            };
            Check(vk.CreateBuffer(device, &screenshotBufferCreateInfo, null, out screenshotBuffer), "vkCreateBuffer");
            vk.GetBufferMemoryRequirements(device, screenshotBuffer, out var screenshotRequirements);
            var screenshotAllocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = screenshotRequirements.Size,
                MemoryTypeIndex = FindMemoryType(screenshotRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit),
            };
            Check(vk.AllocateMemory(device, &screenshotAllocateInfo, null, out screenshotMemory), "vkAllocateMemory");
            Check(vk.BindBufferMemory(device, screenshotBuffer, screenshotMemory, 0), "vkBindBufferMemory");
            void* screenshotMappedLocal;
            Check(vk.MapMemory(device, screenshotMemory, 0, screenshotSize, 0, &screenshotMappedLocal), "vkMapMemory");
            screenshotMapped = screenshotMappedLocal;
        }

        // ---- commands + sync ----
        var poolCreateInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = queueFamily,
        };
        Check(vk.CreateCommandPool(device, &poolCreateInfo, null, out var commandPool), "vkCreateCommandPool");

        var commandBuffers = new CommandBuffer[MaxFramesInFlight];
        var allocateInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = MaxFramesInFlight,
        };
        fixed (CommandBuffer* cb = commandBuffers)
            Check(vk.AllocateCommandBuffers(device, &allocateInfo, cb), "vkAllocateCommandBuffers");

        var imageAvailable = new VkSemaphore[MaxFramesInFlight];
        var inFlight = new Fence[MaxFramesInFlight];
        var renderFinished = new VkSemaphore[imageCount]; // per swapchain image to avoid reuse-before-present
        var semaphoreCreateInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceCreateInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };
        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            Check(vk.CreateSemaphore(device, &semaphoreCreateInfo, null, out imageAvailable[i]), "vkCreateSemaphore");
            Check(vk.CreateFence(device, &fenceCreateInfo, null, out inFlight[i]), "vkCreateFence");
        }
        for (int i = 0; i < imageCount; i++)
            Check(vk.CreateSemaphore(device, &semaphoreCreateInfo, null, out renderFinished[i]), "vkCreateSemaphore");

        // ---- frame loop ----
        long rendered = 0;
        int frame = 0;
        long captureFrame = maxFrames == long.MaxValue ? 120 : Math.Max(0, maxFrames / 2);
        float* pushData = stackalloc float[4];
        while (!GLFW.WindowShouldClose(window.WindowPtr) && rendered < maxFrames)
        {
            window.ProcessEvents(0.0);

            var fence = inFlight[frame];
            Check(vk.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences");

            uint imageIndex = 0;
            var acquire = khrSwapchain.AcquireNextImage(device, swapchain, ulong.MaxValue, imageAvailable[frame], default, &imageIndex);
            if (acquire == Result.ErrorOutOfDateKhr)
                break; // spike: no swapchain recreation, just stop cleanly
            if (acquire != Result.Success && acquire != Result.SuboptimalKhr)
                throw new Exception($"vkAcquireNextImageKHR: {acquire}");

            Check(vk.ResetFences(device, 1, &fence), "vkResetFences");

            var cmd = commandBuffers[frame];
            Check(vk.ResetCommandBuffer(cmd, 0), "vkResetCommandBuffer");
            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };
            Check(vk.BeginCommandBuffer(cmd, &beginInfo), "vkBeginCommandBuffer");

            TransitionImage(cmd, images[imageIndex],
                ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal,
                PipelineStageFlags2.None, AccessFlags2.None,
                PipelineStageFlags2.ColorAttachmentOutputBit, AccessFlags2.ColorAttachmentWriteBit);

            float t = rendered * 0.03f;
            var colorAttachment = new RenderingAttachmentInfo
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageView = views[imageIndex],
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ClearValue = new ClearValue(new ClearColorValue(
                    0.5f + 0.5f * MathF.Sin(t),
                    0.5f + 0.5f * MathF.Sin(t + 2.094f),
                    0.5f + 0.5f * MathF.Sin(t + 4.188f),
                    1f)),
            };
            var renderingInfo = new RenderingInfo
            {
                SType = StructureType.RenderingInfo,
                RenderArea = new Rect2D(new Offset2D(0, 0), extent),
                LayerCount = 1,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachment,
            };
            vk.CmdBeginRendering(cmd, &renderingInfo);

            vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, trianglePipeline);
            // negative-height viewport (core since 1.1): keeps GL's +y-up clip space,
            // this is how the ported engine will avoid touching every projection matrix
            var viewport = new Viewport(0, extent.Height, extent.Width, -(float)extent.Height, 0f, 1f);
            vk.CmdSetViewport(cmd, 0, 1, &viewport);
            var scissor = new Rect2D(new Offset2D(0, 0), extent);
            vk.CmdSetScissor(cmd, 0, 1, &scissor);
            ulong vertexOffset = 0;
            var boundVertexBuffer = vertexBuffer;
            vk.CmdBindVertexBuffers(cmd, 0, 1, &boundVertexBuffer, &vertexOffset);
            pushData[0] = t;
            pushData[1] = extent.Width / (float)extent.Height;
            vk.CmdPushConstants(cmd, pipelineLayout, ShaderStageFlags.VertexBit, 0, 16, pushData);
            vk.CmdDraw(cmd, 3, 1, 0, 0);

            vk.CmdEndRendering(cmd);

            bool captureThisFrame = screenshotPath != null && rendered == captureFrame;
            if (captureThisFrame)
            {
                TransitionImage(cmd, images[imageIndex],
                    ImageLayout.ColorAttachmentOptimal, ImageLayout.TransferSrcOptimal,
                    PipelineStageFlags2.ColorAttachmentOutputBit, AccessFlags2.ColorAttachmentWriteBit,
                    PipelineStageFlags2.CopyBit, AccessFlags2.TransferReadBit);
                var copyRegion = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
                    ImageExtent = new Extent3D(extent.Width, extent.Height, 1),
                };
                vk.CmdCopyImageToBuffer(cmd, images[imageIndex], ImageLayout.TransferSrcOptimal, screenshotBuffer, 1, &copyRegion);
                TransitionImage(cmd, images[imageIndex],
                    ImageLayout.TransferSrcOptimal, ImageLayout.PresentSrcKhr,
                    PipelineStageFlags2.CopyBit, AccessFlags2.TransferReadBit,
                    PipelineStageFlags2.None, AccessFlags2.None);
            }
            else
            {
                TransitionImage(cmd, images[imageIndex],
                    ImageLayout.ColorAttachmentOptimal, ImageLayout.PresentSrcKhr,
                    PipelineStageFlags2.ColorAttachmentOutputBit, AccessFlags2.ColorAttachmentWriteBit,
                    PipelineStageFlags2.None, AccessFlags2.None);
            }

            Check(vk.EndCommandBuffer(cmd), "vkEndCommandBuffer");

            var waitInfo = new SemaphoreSubmitInfo
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = imageAvailable[frame],
                StageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            };
            var signalInfo = new SemaphoreSubmitInfo
            {
                SType = StructureType.SemaphoreSubmitInfo,
                Semaphore = renderFinished[imageIndex],
                StageMask = PipelineStageFlags2.ColorAttachmentOutputBit,
            };
            var cmdInfo = new CommandBufferSubmitInfo
            {
                SType = StructureType.CommandBufferSubmitInfo,
                CommandBuffer = cmd,
            };
            var submitInfo = new SubmitInfo2
            {
                SType = StructureType.SubmitInfo2,
                WaitSemaphoreInfoCount = 1,
                PWaitSemaphoreInfos = &waitInfo,
                CommandBufferInfoCount = 1,
                PCommandBufferInfos = &cmdInfo,
                SignalSemaphoreInfoCount = 1,
                PSignalSemaphoreInfos = &signalInfo,
            };
            Check(vk.QueueSubmit2(queue, 1, &submitInfo, fence), "vkQueueSubmit2");

            var presentSwapchain = swapchain;
            var presentWait = renderFinished[imageIndex];
            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &presentWait,
                SwapchainCount = 1,
                PSwapchains = &presentSwapchain,
                PImageIndices = &imageIndex,
            };
            var present = khrSwapchain.QueuePresent(queue, &presentInfo);
            if (present == Result.ErrorOutOfDateKhr)
                break;
            if (present != Result.Success && present != Result.SuboptimalKhr)
                throw new Exception($"vkQueuePresentKHR: {present}");

            if (captureThisFrame)
            {
                // the submit fence also covers the copy, so waiting on it makes the
                // mapped (coherent) buffer safe to read
                Check(vk.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences");
                var pixels = new ReadOnlySpan<byte>(screenshotMapped, (int)screenshotSize);
                using var screenshotImage = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(pixels, (int)extent.Width, (int)extent.Height);
                screenshotImage.SaveAsPng(screenshotPath!);
                Console.WriteLine($"Screenshot saved to {Path.GetFullPath(screenshotPath!)}");
            }

            rendered++;
            frame = (frame + 1) % MaxFramesInFlight;
        }

        // ---- cleanup (validation reports leaks, so destroy everything) ----
        vk.DeviceWaitIdle(device);
        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            vk.DestroySemaphore(device, imageAvailable[i], null);
            vk.DestroyFence(device, inFlight[i], null);
        }
        for (int i = 0; i < imageCount; i++)
            vk.DestroySemaphore(device, renderFinished[i], null);
        vk.DestroyCommandPool(device, commandPool, null);
        vk.DestroyPipeline(device, trianglePipeline, null);
        vk.DestroyPipelineLayout(device, pipelineLayout, null);
        vk.DestroyShaderModule(device, vertModule, null);
        vk.DestroyShaderModule(device, fragModule, null);
        vk.DestroyBuffer(device, vertexBuffer, null);
        vk.FreeMemory(device, vertexMemory, null);
        if (screenshotBuffer.Handle != 0)
        {
            vk.UnmapMemory(device, screenshotMemory);
            vk.DestroyBuffer(device, screenshotBuffer, null);
            vk.FreeMemory(device, screenshotMemory, null);
        }
        for (int i = 0; i < imageCount; i++)
            vk.DestroyImageView(device, views[i], null);
        khrSwapchain.DestroySwapchain(device, swapchain, null);
        vk.DestroyDevice(device, null);
        khrSurface.DestroySurface(instance, surface, null);
        if (debugUtils != null)
            debugUtils.DestroyDebugUtilsMessenger(instance, messenger, null);
        vk.DestroyInstance(instance, null);

        if (validationErrors == 0)
        {
            Console.WriteLine($"VULKAN M1 SPIKE PASSED: {rendered} frames on {pickedName}, 0 validation errors");
            return 0;
        }
        Console.WriteLine($"VULKAN M1 SPIKE FAILED: {validationErrors} validation errors");
        return 1;
    }

    private static byte[] CompileGlsl(string path)
    {
        var source = File.ReadAllText(path);
        var kind = Path.GetExtension(path) switch
        {
            ".vert" => Silk.NET.Shaderc.ShaderKind.VertexShader,
            ".frag" => Silk.NET.Shaderc.ShaderKind.FragmentShader,
            ".comp" => Silk.NET.Shaderc.ShaderKind.ComputeShader,
            var ext => throw new Exception($"unsupported shader extension {ext}"),
        };

        var api = Silk.NET.Shaderc.Shaderc.GetApi();
        var compiler = api.CompilerInitialize();
        var options = api.CompileOptionsInitialize();
        api.CompileOptionsSetSourceLanguage(options, Silk.NET.Shaderc.SourceLanguage.Glsl);
        api.CompileOptionsSetTargetEnv(options, Silk.NET.Shaderc.TargetEnv.Vulkan, (uint)Silk.NET.Shaderc.EnvVersion.Vulkan13);
        var result = api.CompileIntoSpv(compiler, source, (nuint)System.Text.Encoding.UTF8.GetByteCount(source),
            kind, path, "main", options);
        try
        {
            if (api.ResultGetCompilationStatus(result) != Silk.NET.Shaderc.CompilationStatus.Success)
                throw new Exception($"shaderc failed for {path}:\n{api.ResultGetErrorMessageS(result)}");
            var spirv = new byte[(int)api.ResultGetLength(result)];
            new ReadOnlySpan<byte>(api.ResultGetBytes(result), spirv.Length).CopyTo(spirv);
            return spirv;
        }
        finally
        {
            api.ResultRelease(result);
            api.CompileOptionsRelease(options);
            api.CompilerRelease(compiler);
        }
    }

    private static ShaderModule CreateShaderModule(Device device, byte[] spirv)
    {
        fixed (byte* code = spirv)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)code,
            };
            Check(vk.CreateShaderModule(device, &createInfo, null, out var module), "vkCreateShaderModule");
            return module;
        }
    }

    private static void TransitionImage(CommandBuffer cmd, Image image,
        ImageLayout oldLayout, ImageLayout newLayout,
        PipelineStageFlags2 srcStage, AccessFlags2 srcAccess,
        PipelineStageFlags2 dstStage, AccessFlags2 dstAccess)
    {
        var barrier = new ImageMemoryBarrier2
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = srcStage,
            SrcAccessMask = srcAccess,
            DstStageMask = dstStage,
            DstAccessMask = dstAccess,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
        };
        var dependencyInfo = new DependencyInfo
        {
            SType = StructureType.DependencyInfo,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier,
        };
        vk.CmdPipelineBarrier2(cmd, &dependencyInfo);
    }

    private static bool TryFindQueueFamily(PhysicalDevice physicalDevice, KhrSurface khrSurface, SurfaceKHR surface, out uint family)
    {
        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* f = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, f);
        for (uint i = 0; i < count; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) == 0)
                continue;
            khrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, i, surface, out var presentSupported);
            if (presentSupported)
            {
                family = i;
                return true;
            }
        }
        family = 0;
        return false;
    }

    private static HashSet<string> GetInstanceLayers()
    {
        uint count = 0;
        vk.EnumerateInstanceLayerProperties(&count, null);
        var props = new LayerProperties[count];
        fixed (LayerProperties* p = props)
            vk.EnumerateInstanceLayerProperties(&count, p);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].LayerName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    private static HashSet<string> GetInstanceExtensions()
    {
        uint count = 0;
        vk.EnumerateInstanceExtensionProperties((byte*)null, &count, null);
        var props = new ExtensionProperties[count];
        fixed (ExtensionProperties* p = props)
            vk.EnumerateInstanceExtensionProperties((byte*)null, &count, p);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].ExtensionName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    private static HashSet<string> GetDeviceExtensions(PhysicalDevice physicalDevice)
    {
        uint count = 0;
        vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &count, null);
        var props = new ExtensionProperties[count];
        fixed (ExtensionProperties* p = props)
            vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &count, p);
        var result = new HashSet<string>();
        for (int i = 0; i < count; i++)
            fixed (byte* name = props[i].ExtensionName)
                result.Add(Marshal.PtrToStringAnsi((nint)name)!);
        return result;
    }

    private static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* data, void* userData)
    {
        var message = Marshal.PtrToStringAnsi((nint)data->PMessage);
        if ((severity & DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt) != 0)
        {
            validationErrors++;
            Console.WriteLine($"[VK ERROR] {message}");
        }
        else
        {
            Console.WriteLine($"[vk] {message}");
        }
        return Vk.False;
    }

    private static void Check(Result result, string what)
    {
        if (result != Result.Success)
            throw new Exception($"{what} failed: {result}");
    }
}
