using Silk.NET.Vulkan;

namespace TheEngineAvalonia.Interfaces;

public interface IRenderingWindow : System.IDisposable
{
    IntPtr Pointer { get; }
    event Action<long, long, bool, bool>? OnPointerPressed;
    event Action<long, long, bool, bool>? OnPointerReleased;
    event Action<long, long, bool, bool>? OnPointerMoved;
    void Show();
    void Hide();

    /// <summary>Instance extensions this window's <see cref="CreateSurface"/> implementation needs enabled
    /// (VK_KHR_surface plus the platform-specific *_surface extension).</summary>
    string[] RequiredInstanceExtensions { get; }

    /// <summary>Creates the real VkSurfaceKHR for this native window (as opposed to treating the
    /// window handle itself as already being a surface).</summary>
    SurfaceKHR CreateSurface(Vk vk, Instance instance);
}