using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using SilkNetDemo;


namespace GpuInterop.VulkanDemo;

public unsafe class VulkanContext : IDisposable
{
    public required Vk Api { get; init; }
    public required Instance Instance { get; init; }
    public required PhysicalDevice PhysicalDevice { get; init; }
    public required Device Device { get; init; }
    public required Queue Queue { get; init; }
    public required uint QueueFamilyIndex { get; init; }
    public required VulkanCommandBufferPool Pool { get; init; }
    public required DescriptorPool DescriptorPool { get; init; }
    public required ComPtr<ID3D11Device> D3DDevice { get; init; }

    public static (VulkanContext? result, string info) TryCreate(ICompositionGpuInterop gpuInterop)
    {
        TheEngine.Vulkan.MoltenVkIcdFallback.EnsureVulkanDriverDiscoverable();
        using var appName = new ByteString("GpuInterop");
        using var engineName = new ByteString("Test");
        var applicationInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = appName,
            // 1.3: the engine requires dynamic rendering + synchronization2 (core in 1.3).
            ApiVersion = new Version32(1, 3, 0),
            PEngineName = appName,
            EngineVersion = new Version32(1, 0, 0),
            ApplicationVersion = new Version32(1, 0, 0)
        };

        var enabledExtensions = new List<string>()
        {
            "VK_KHR_get_physical_device_properties2"
        };
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            enabledExtensions.Add("VK_KHR_portability_enumeration");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            enabledExtensions.AddRange([
                "VK_KHR_external_memory_capabilities",
                "VK_KHR_external_semaphore_capabilities"
            ]);
        }

        var enabledLayers = new List<string>();
        
        Vk api = GetApi();
        enabledExtensions.Add("VK_EXT_debug_utils");
        if (IsLayerAvailable(api, "VK_LAYER_KHRONOS_validation"))
            enabledLayers.Add("VK_LAYER_KHRONOS_validation");


        Device device = default;
        DescriptorPool descriptorPool = default;
        VulkanCommandBufferPool? pool = null;
        bool success = false;
        try
        {
            // Validation is off by default (very expensive on MoltenVK). Set THEENGINE_VK_VALIDATION=1
            // to keep the KHRONOS validation layer enabled - it pinpoints use-after-free / wrong-layout
            // resource bugs (the LogCallback below prints them) instead of a bare GPU page fault.
            if (Environment.GetEnvironmentVariable("THEENGINE_VK_VALIDATION") != "1")
                enabledLayers.Clear();
            using var pRequiredExtensions = new ByteStringList(enabledExtensions);
            using var pEnabledLayers = new ByteStringList(enabledLayers);

            var instanceCreateInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &applicationInfo,
                PpEnabledExtensionNames = pRequiredExtensions,
                EnabledExtensionCount = pRequiredExtensions.UCount,
                PpEnabledLayerNames = pEnabledLayers,
                EnabledLayerCount = pEnabledLayers.UCount,
                Flags = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? InstanceCreateFlags.EnumeratePortabilityBitKhr : default
            };

            api.CreateInstance(in instanceCreateInfo, null, out var vkInstance).ThrowOnError();


            if (api.TryGetInstanceExtension(vkInstance, out ExtDebugUtils debugUtils))
            {
                var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT
                {
                    SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                    MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                      DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                      DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                    MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                  DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                                  DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                    PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(LogCallback),
                };

                debugUtils.CreateDebugUtilsMessenger(vkInstance, in debugCreateInfo, null, out _);
            }

            var requireDeviceExtensions = new List<string>();
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                requireDeviceExtensions.AddRange([
                    "VK_KHR_external_memory",
                    "VK_KHR_external_semaphore"
                ]);
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!(gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                        .D3D11TextureGlobalSharedHandle)
                    || gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                        .VulkanOpaqueNtHandle))
                   )
                    return (null, "Image sharing is not supported by the current backend");
                requireDeviceExtensions.Add(KhrExternalMemoryWin32.ExtensionName);
                requireDeviceExtensions.Add(KhrExternalSemaphoreWin32.ExtensionName);
                requireDeviceExtensions.Add("VK_KHR_dedicated_allocation");
                requireDeviceExtensions.Add("VK_KHR_get_memory_requirements2");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (!gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                        .IOSurfaceRef)
                   )
                    return (null, "Image sharing is not supported by the current backend");
                requireDeviceExtensions.AddRange(["VK_EXT_metal_objects", "VK_KHR_timeline_semaphore"]);
                // MoltenVK is a portability driver: VK_KHR_portability_subset must be enabled if advertised.
                requireDeviceExtensions.Add("VK_KHR_portability_subset");
            }
            else
            {
                if (!gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes
                        .VulkanOpaquePosixFileDescriptor)
                    || !gpuInterop.SupportedSemaphoreTypes.Contains(KnownPlatformGraphicsExternalSemaphoreHandleTypes
                        .VulkanOpaquePosixFileDescriptor)
                   )
                    return (null, "Image sharing is not supported by the current backend");
                requireDeviceExtensions.Add(KhrExternalMemoryFd.ExtensionName);
                requireDeviceExtensions.Add(KhrExternalSemaphoreFd.ExtensionName);
            }

            uint count = 0;
            api.EnumeratePhysicalDevices(vkInstance, ref count, null).ThrowOnError();
            var physicalDevices = stackalloc PhysicalDevice[(int)count];
            api.EnumeratePhysicalDevices(vkInstance, ref count, physicalDevices)
                .ThrowOnError();

            for (uint c = 0; c < count; c++)
            {
                if (requireDeviceExtensions.Any(ext => !api.IsDeviceExtensionPresent(physicalDevices[c], ext)))
                    continue;

                var physicalDeviceIDProperties = new PhysicalDeviceIDProperties()
                {
                    SType = StructureType.PhysicalDeviceIDProperties
                };
                var physicalDeviceProperties2 = new PhysicalDeviceProperties2()
                {
                    SType = StructureType.PhysicalDeviceProperties2,
                    PNext = &physicalDeviceIDProperties
                };
                api.GetPhysicalDeviceProperties2(physicalDevices[c], &physicalDeviceProperties2);

                if (gpuInterop.DeviceLuid != null && physicalDeviceIDProperties.DeviceLuidvalid)
                {
                    if (!new Span<byte>(physicalDeviceIDProperties.DeviceLuid, 8)
                            .SequenceEqual(gpuInterop.DeviceLuid))
                        continue;
                }
                else if (gpuInterop.DeviceUuid != null)
                {
                    if (!new Span<byte>(physicalDeviceIDProperties.DeviceUuid, 16)
                            .SequenceEqual(gpuInterop.DeviceUuid))
                        continue;
                }

                var physicalDevice = physicalDevices[c];

                var name = Marshal.PtrToStringAnsi(new IntPtr(physicalDeviceProperties2.Properties.DeviceName))!;


                uint queueFamilyCount = 0;
                api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, null);
                var familyProperties = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
                api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref queueFamilyCount, familyProperties);
                for (uint queueFamilyIndex = 0; queueFamilyIndex < queueFamilyCount; queueFamilyIndex++)
                {
                    var family = familyProperties[queueFamilyIndex];
                    if (!family.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                        continue;


                    var queuePriorities = stackalloc float[(int)family.QueueCount];

                    for (var i = 0; i < family.QueueCount; i++)
                        queuePriorities[i] = 1f;

                    // The device must enable everything TheEngine's Vulkan backend requires, on top of
                    // the interop external-memory/semaphore extensions added above. Mirrors
                    // TheEngine.Vulkan.VulkanContext.PickDeviceAndCreate.
                    api.GetPhysicalDeviceFeatures(physicalDevice, out var supportedFeatures);
                    var enabledFeatures = new PhysicalDeviceFeatures
                    {
                        RobustBufferAccess = supportedFeatures.RobustBufferAccess,
                        FillModeNonSolid = supportedFeatures.FillModeNonSolid,
                        SamplerAnisotropy = supportedFeatures.SamplerAnisotropy,
                        IndependentBlend = supportedFeatures.IndependentBlend,
                        DrawIndirectFirstInstance = supportedFeatures.DrawIndirectFirstInstance,
                    };
                    var features13 = new PhysicalDeviceVulkan13Features
                    {
                        SType = StructureType.PhysicalDeviceVulkan13Features,
                        DynamicRendering = true,
                        Synchronization2 = true,
                        ShaderDemoteToHelperInvocation = true,
                    };
                    var features12 = new PhysicalDeviceVulkan12Features
                    {
                        SType = StructureType.PhysicalDeviceVulkan12Features,
                        PNext = &features13,
                        ShaderSampledImageArrayNonUniformIndexing = true,
                        DescriptorBindingPartiallyBound = true,
                        DescriptorBindingSampledImageUpdateAfterBind = true,
                        DescriptorBindingStorageBufferUpdateAfterBind = true,
                        // macOS interop synchronizes with the compositor via a timeline semaphore.
                        TimelineSemaphore = true,
                    };
                    var features2 = new PhysicalDeviceFeatures2
                    {
                        SType = StructureType.PhysicalDeviceFeatures2,
                        PNext = &features12,
                        Features = enabledFeatures,
                    };

                    var queueCreateInfo = new DeviceQueueCreateInfo
                    {
                        SType = StructureType.DeviceQueueCreateInfo,
                        QueueFamilyIndex = queueFamilyIndex,
                        QueueCount = family.QueueCount,
                        PQueuePriorities = queuePriorities
                    };

                    using var pEnabledDeviceExtensions = new ByteStringList(requireDeviceExtensions);
                    var deviceCreateInfo = new DeviceCreateInfo
                    {
                        SType = StructureType.DeviceCreateInfo,
                        // PEnabledFeatures must be null when PhysicalDeviceFeatures2 is chained in PNext.
                        PNext = &features2,
                        QueueCreateInfoCount = 1,
                        PQueueCreateInfos = &queueCreateInfo,
                        PpEnabledExtensionNames = pEnabledDeviceExtensions,
                        EnabledExtensionCount = pEnabledDeviceExtensions.UCount,
                    };

                    api.CreateDevice(physicalDevice, in deviceCreateInfo, null, out device)
                        .ThrowOnError();

                    api.GetDeviceQueue(device, queueFamilyIndex, 0, out var queue);

                    var descriptorPoolSize = new DescriptorPoolSize
                    {
                        Type = DescriptorType.UniformBuffer, DescriptorCount = 16
                    };
                    var descriptorPoolInfo = new DescriptorPoolCreateInfo
                    {
                        SType = StructureType.DescriptorPoolCreateInfo,
                        PoolSizeCount = 1,
                        PPoolSizes = &descriptorPoolSize,
                        MaxSets = 16,
                        Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
                    };
                    
                    api.CreateDescriptorPool(device, &descriptorPoolInfo, null, out descriptorPool)
                        .ThrowOnError();

                    pool = new VulkanCommandBufferPool(api, device, queue, queueFamilyIndex);

                    ComPtr<ID3D11Device> d3dDevice = null;
                    if (physicalDeviceIDProperties.DeviceLuidvalid &&
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                        !gpuInterop.SupportedImageHandleTypes.Contains(KnownPlatformGraphicsExternalImageHandleTypes.VulkanOpaqueNtHandle)
                        )
                        d3dDevice = D3DMemoryHelper.CreateDeviceByLuid(
                            MemoryMarshal.Read<Luid>(new Span<byte>(physicalDeviceIDProperties.DeviceLuid, 8)));
                    success = true;
                    return (new VulkanContext
                    {
                        Api = api,
                        Device = device,
                        Instance = vkInstance,
                        PhysicalDevice = physicalDevice,
                        Queue = queue,
                        QueueFamilyIndex = queueFamilyIndex,
                        Pool = pool,
                        DescriptorPool = descriptorPool,
                        D3DDevice = d3dDevice
                    }, name);
                }
                return (null, "No suitable device queue found");
            }

            return (null, "Suitable device not found");

        }
        catch (Exception e)
        {
            return (null, e.ToString());
        }
        finally
        {
            if (!success)
            {
                pool?.Dispose();
                if (descriptorPool.Handle != default)
                    api.DestroyDescriptorPool(device, descriptorPool, null);
                if (device.Handle != default)
                    api.DestroyDevice(device, null);
            }
        }
    }

    private static unsafe bool IsLayerAvailable(Vk api, string layerName)
    {
        uint layerPropertiesCount;

        api.EnumerateInstanceLayerProperties(&layerPropertiesCount, null).ThrowOnError();

        var layerProperties = new LayerProperties[layerPropertiesCount];

        fixed (LayerProperties* pLayerProperties = layerProperties)
        {
            api.EnumerateInstanceLayerProperties(&layerPropertiesCount, layerProperties).ThrowOnError();

            for (var i = 0; i < layerPropertiesCount; i++)
            {
                var currentLayerName = Marshal.PtrToStringAnsi((IntPtr)pLayerProperties[i].LayerName);

                if (currentLayerName == layerName) return true;
            }
        }

        return false;
    }

    private static unsafe uint LogCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        if (messageSeverity != DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt)
        {
            var message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
            Console.WriteLine(message);
        }

        return Vk.False;
    }


    private const string MacVulkanSdkGlobalPath = "/usr/local/lib/libvulkan.dylib";

    private static Vk GetApi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || !File.Exists(MacVulkanSdkGlobalPath))
            return Vk.GetApi();
        var ctx = new MultiNativeContext(new INativeContext[2]
        {
            Vk.CreateDefaultContext([MacVulkanSdkGlobalPath]),
            null!
        });
        var ret = new Vk(ctx);
        ctx.Contexts[1] = new LamdaNativeContext((Func<string, IntPtr>) ((x) =>
        {
            if (x.EndsWith("ProcAddr"))
                return IntPtr.Zero;
            IntPtr deviceProcAddr = (IntPtr) ret.GetDeviceProcAddr(ret.CurrentDevice.GetValueOrDefault(), x);
            return deviceProcAddr != IntPtr.Zero ? deviceProcAddr : (IntPtr) ret.GetInstanceProcAddr(ret.CurrentInstance.GetValueOrDefault(), x);
        }));
        return ret;
    }
    
    public void Dispose()
    {
        D3DDevice.Dispose();
        Pool.Dispose();
        Api.DestroyDescriptorPool(Device, DescriptorPool, null);
        Api.DestroyDevice(Device, null);
    }
}
