// The native VMA bindings (Generated.cs, compiled from the VMABindings submodule) were
// generated against OpenTK's Vulkan type names, which keep the `Vk` prefix. Silk.NET models
// the identical Vulkan ABI but drops the prefix. These aliases let the pristine generated
// source compile against Silk.NET.Vulkan with zero edits to the submodule.
global using System;

global using VkBuffer = Silk.NET.Vulkan.Buffer;
global using VkImage = Silk.NET.Vulkan.Image;
global using VkDevice = Silk.NET.Vulkan.Device;
global using VkInstance = Silk.NET.Vulkan.Instance;
global using VkPhysicalDevice = Silk.NET.Vulkan.PhysicalDevice;
global using VkDeviceMemory = Silk.NET.Vulkan.DeviceMemory;
global using VkCommandBuffer = Silk.NET.Vulkan.CommandBuffer;
global using VkResult = Silk.NET.Vulkan.Result;
global using VkBool32 = Silk.NET.Core.Bool32;

global using VkAllocationCallbacks = Silk.NET.Vulkan.AllocationCallbacks;
global using VkBufferCreateInfo = Silk.NET.Vulkan.BufferCreateInfo;
global using VkImageCreateInfo = Silk.NET.Vulkan.ImageCreateInfo;
global using VkBufferCopy = Silk.NET.Vulkan.BufferCopy;
global using VkMemoryRequirements = Silk.NET.Vulkan.MemoryRequirements;
global using VkMemoryRequirements2 = Silk.NET.Vulkan.MemoryRequirements2;
global using VkMemoryAllocateInfo = Silk.NET.Vulkan.MemoryAllocateInfo;
global using VkMappedMemoryRange = Silk.NET.Vulkan.MappedMemoryRange;
global using VkBindBufferMemoryInfo = Silk.NET.Vulkan.BindBufferMemoryInfo;
global using VkBindImageMemoryInfo = Silk.NET.Vulkan.BindImageMemoryInfo;
global using VkBufferMemoryRequirementsInfo2 = Silk.NET.Vulkan.BufferMemoryRequirementsInfo2;
global using VkImageMemoryRequirementsInfo2 = Silk.NET.Vulkan.ImageMemoryRequirementsInfo2;
global using VkDeviceBufferMemoryRequirements = Silk.NET.Vulkan.DeviceBufferMemoryRequirements;
global using VkDeviceImageMemoryRequirements = Silk.NET.Vulkan.DeviceImageMemoryRequirements;
global using VkPhysicalDeviceProperties = Silk.NET.Vulkan.PhysicalDeviceProperties;
global using VkPhysicalDeviceMemoryProperties = Silk.NET.Vulkan.PhysicalDeviceMemoryProperties;
global using VkPhysicalDeviceMemoryProperties2 = Silk.NET.Vulkan.PhysicalDeviceMemoryProperties2;
global using VkMemoryGetWin32HandleInfoKHR = Silk.NET.Vulkan.MemoryGetWin32HandleInfoKHR;

// Silk.NET exposes a single [Flags] enum where the generator emitted both the *FlagBits and
// *Flags spellings; both map to the same Silk.NET type.
global using VkMemoryPropertyFlagBits = Silk.NET.Vulkan.MemoryPropertyFlags;
global using VkMemoryPropertyFlags = Silk.NET.Vulkan.MemoryPropertyFlags;
global using VkMemoryMapFlagBits = Silk.NET.Vulkan.MemoryMapFlags;
