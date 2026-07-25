using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;

namespace GpuInterop;

/// <summary>
/// Base control for composition-backed GPU interop: owns a <see cref="CompositionDrawingSurface"/>
/// and drives a continuous render loop through the compositor (RequestCompositionUpdate). Derived
/// classes create their graphics resources in <see cref="InitializeGraphicsResources"/> and render
/// a frame in <see cref="RenderFrame"/>. Adapted from GpuInterop's DrawingSurfaceDemoBase with the
/// teapot-demo coupling (IGpuDemo / GpuDemo / Yaw,Pitch,Roll,Disco) removed and frame queuing made
/// unconditional so the engine renders every compositor tick.
/// </summary>
public abstract class DrawingSurfaceDemoBase : Control
{
    private CompositionSurfaceVisual? _visual;
    private Compositor? _compositor;
    private readonly Action _update;
    private bool _updateQueued;
    private bool _initialized;

    protected CompositionDrawingSurface? Surface { get; private set; }

    public DrawingSurfaceDemoBase()
    {
        _update = UpdateFrame;
    }

    /// <summary>
    /// When false, detaching from the tree only pauses rendering (the compositor loop stops on its
    /// own once there is no presentation source) and the graphics resources stay alive for a later
    /// reattach; the subclass is then responsible for calling <see cref="TearDownGraphics"/> when the
    /// hosted content requests destruction.
    /// </summary>
    protected virtual bool FreeResourcesOnDetach => true;

    protected bool IsGraphicsInitialized => _initialized;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Initialize();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (FreeResourcesOnDetach)
            TearDownGraphics();
        base.OnDetachedFromLogicalTree(e);
    }

    protected void TearDownGraphics()
    {
        if (_initialized)
        {
            Surface?.Dispose();
            FreeGraphicsResources();
        }

        Surface = null;
        _visual = null;
        _initialized = false;
    }

    async void Initialize()
    {
        try
        {
            var selfVisual = ElementComposition.GetElementVisual(this)!;

            if (_initialized)
            {
                // reattach: resources survived the detach, just plug the visual back in and resume
                ElementComposition.SetElementChildVisual(this, _visual);
                QueueNextFrame();
                return;
            }

            _compositor = selfVisual.Compositor;

            Surface = _compositor.CreateDrawingSurface();
            _visual = _compositor.CreateSurfaceVisual();
            _visual.Size = new(Bounds.Width, Bounds.Height);
            _visual.Surface = Surface;
            ElementComposition.SetElementChildVisual(this, _visual);
            var (res, info) = await DoInitialize(_compositor, Surface);
            OnInfo(info);
            _initialized = res;
            QueueNextFrame();
        }
        catch (Exception e)
        {
            OnInfo(e.ToString());
        }
    }

    void UpdateFrame()
    {
        _updateQueued = false;
        var source = this.GetPresentationSource();
        if (source == null)
            return;

        _visual!.Size = new(Bounds.Width, Bounds.Height);
        var size = PixelSize.FromSize(Bounds.Size, source.RenderScaling);
        RenderFrame(size);
        // continuous rendering: always request the next compositor update
        QueueNextFrame();
    }

    protected void QueueNextFrame()
    {
        if (_initialized && !_updateQueued && _compositor != null)
        {
            _updateQueued = true;
            _compositor?.RequestCompositionUpdate(_update);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BoundsProperty)
            QueueNextFrame();
        base.OnPropertyChanged(change);
    }

    async Task<(bool success, string info)> DoInitialize(Compositor compositor,
        CompositionDrawingSurface compositionDrawingSurface)
    {
        var interop = await compositor.TryGetCompositionGpuInterop();
        if (interop == null)
            return (false, "Compositor doesn't support interop for the current backend");
        return InitializeGraphicsResources(compositor, compositionDrawingSurface, interop);
    }

    protected abstract (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface compositionDrawingSurface, ICompositionGpuInterop gpuInterop);

    protected abstract void FreeGraphicsResources();

    protected abstract void RenderFrame(PixelSize pixelSize);

    /// <summary>Called with the initialization status message (device name or error). Override to surface it.</summary>
    protected virtual void OnInfo(string info) { }
}
