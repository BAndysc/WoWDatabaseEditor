using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AvaloniaStyles.Controls;

public class ControlRenderer : Control
{
    public Action<DrawingContext>? RenderOverride { get; set; }
    
    public override void Render(DrawingContext context)
    {
        RenderOverride?.Invoke(context);
    }
}

public abstract class RenderedPanel : Panel
{
    private ControlRenderer renderer;

    public Control Renderer => renderer;

    public RenderedPanel()
    {
        renderer = new ControlRenderer();
        Children.Add(renderer);
        renderer.RenderOverride = Render;

        Children.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                Children.Add(renderer);
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove &&
                     ReferenceEquals(args.OldItems![0], renderer))
            {
                Children.Add(renderer);
            }
        };
    }

    protected void EnsureRenderer()
    {
        if (!Children.Contains(renderer))
            Children.Add(renderer);
    }

    public IEnumerable<Control> ActualChildren => Children.Where(x => !IsRenderer(x));

    public IEnumerable<Visual> ActualVisualChildren => VisualChildren.Where(x => !IsRenderer(x));

    // might be not the best solution?
    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
        renderer.Arrange(new Rect(0, 0, finalRect.Width, finalRect.Height));
    }
    
    protected bool IsRenderer(Control control)
    {
        return ReferenceEquals(control, renderer);
    }
    
    protected bool IsRenderer(Visual visual)
    {
        return ReferenceEquals(visual, renderer);
    }
    
    protected static void AffectsRender<T>(AvaloniaProperty property) where T : RenderedPanel
    {
        property.Changed.AddClassHandler<T>((t, e) =>
        {
            t.InvalidateVisual();
        });
    }

    public new void InvalidateVisual()
    {
        renderer.InvalidateVisual();
    }
    
    public new abstract void Render(DrawingContext context);
}