using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using TheEngineAvalonia.Interfaces;

namespace TheEngineAvalonia.Apple;

public class EmbeddedAppleVulkanWindow : IRenderingWindow
{
    private ObjectiveC.NSView nsView;
    private ObjectiveC.CAMetalLayer metalLayer;

    public IntPtr Pointer => nsView.Pointer;
    public event Action<long, long, bool, bool>? OnPointerPressed;
    public event Action<long, long, bool, bool>? OnPointerReleased;
    public event Action<long, long, bool, bool>? OnPointerMoved;

    public string[] RequiredInstanceExtensions => new[] { KhrSurface.ExtensionName, ExtMetalSurface.ExtensionName };

    public void Show()
    {
        nsView.Hidden = false;
    }

    public void Hide()
    {
        nsView.Hidden = true;
    }

    public EmbeddedAppleVulkanWindow()
    {
        metalLayer = new ObjectiveC.CAMetalLayer();
        nsView = new ObjectiveC.NSView(new ObjectiveC.NSRect(0, 0, 100, 100))
        {
            WantsLayer = true
        };
        nsView.Layer = metalLayer;
    }

    public unsafe SurfaceKHR CreateSurface(Vk vk, Instance instance)
    {
        if (!vk.TryGetInstanceExtension(instance, out ExtMetalSurface metalSurfaceExt))
            throw new Exception("VK_EXT_metal_surface not available");

        var createInfo = new MetalSurfaceCreateInfoEXT
        {
            SType = StructureType.MetalSurfaceCreateInfoExt,
            PLayer = (IntPtr*)metalLayer.Pointer,
        };
        var result = metalSurfaceExt.CreateMetalSurface(instance, in createInfo, null, out var surface);
        if (result != Result.Success)
            throw new Exception($"vkCreateMetalSurfaceEXT failed: {result}");
        return surface;
    }

    public void Dispose()
    {
        nsView.Dispose();
        metalLayer.Dispose();
    }
}