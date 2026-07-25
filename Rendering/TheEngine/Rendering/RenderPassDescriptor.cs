using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>What happens to an attachment's previous content when a rendering pass begins.</summary>
public enum LoadOp
{
    /// <summary>Keep the existing content (GL: no clear; Vulkan: VK_ATTACHMENT_LOAD_OP_LOAD).</summary>
    Load,
    /// <summary>Clear to <see cref="RenderPassDescriptor.ClearColor"/> (depth is cleared too).</summary>
    Clear,
}

[Flags]
public enum BlitMask
{
    Color = 1,
    Depth = 2,
}

public enum BlitFilter
{
    Nearest,
    Linear,
}

/// <summary>
/// How a texture is used by the GPU. Transitions between usages are declared
/// with <see cref="ICommandList.Barrier"/> - a no-op on GL, an image memory
/// barrier + layout transition on Vulkan. The "from" usage is a declaration of
/// intent: on first use within a texture's lifetime it may not match (the image
/// is undefined), so a Vulkan executor should track the actual current layout
/// per image and treat a mismatched "from" as benign.
/// </summary>
public enum ResourceUsage
{
    RenderTarget,
    ShaderRead,
    TransferSource,
    TransferDestination,
}

/// <summary>
/// Describes a rendering pass: the target and what happens to its previous content.
/// Maps to vkCmdBeginRendering's attachment info; on GL it binds the framebuffer,
/// sets the viewport and optionally clears.
/// </summary>
public readonly struct RenderPassDescriptor
{
    /// <summary>Render target, or null to render to a default (window) framebuffer.</summary>
    public ITexture? Target { get; init; }

    /// <summary>GL framebuffer id used when <see cref="Target"/> is null.</summary>
    public int DefaultFramebuffer { get; init; }

    /// <summary>Viewport size, used when <see cref="Target"/> is null (render textures know their own size).</summary>
    public int Width { get; init; }
    public int Height { get; init; }

    public LoadOp ColorLoadOp { get; init; }
    public Color4 ClearColor { get; init; }

    /// <summary>Depth attachment load op override; defaults to <see cref="ColorLoadOp"/> when null.</summary>
    public LoadOp? DepthLoadOp { get; init; }

    private readonly float viewportScale;
    /// <summary>Transitional: dynamic resolution scale applied to the render texture's viewport. Defaults to 1.</summary>
    public float ViewportScale
    {
        get => viewportScale <= 0 ? 1 : viewportScale;
        init => viewportScale = value;
    }
}
