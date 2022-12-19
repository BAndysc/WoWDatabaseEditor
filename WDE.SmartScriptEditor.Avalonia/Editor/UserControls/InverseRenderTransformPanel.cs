using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class InverseRenderTransformPanel : Panel
{
    static InverseRenderTransformPanel()
    {
        VerticalAlignmentProperty.OverrideDefaultValue<InverseRenderTransformPanel>(VerticalAlignment.Stretch);
        RenderTransformProperty.Changed.AddClassHandler<InverseRenderTransformPanel>((panel, e) =>
        {
            panel.InvalidateMeasure();
        });
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        if (RenderTransform == null)
            return base.MeasureOverride(availableSize);
        var size = base.MeasureOverride(new Size(availableSize.Width / RenderTransform.Value.M11, availableSize.Height));
        return new Size(size.Width, size.Height * RenderTransform.Value.M22);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (RenderTransform == null)
            return base.ArrangeOverride(finalSize);
        var size = base.ArrangeOverride(new Size(finalSize.Width / RenderTransform.Value.M11, finalSize.Height / RenderTransform.Value.M11));
        return new Size(size.Width, size.Height);
    }
}