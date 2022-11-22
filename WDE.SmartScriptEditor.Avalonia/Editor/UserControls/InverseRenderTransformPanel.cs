using System;
using Avalonia;
using Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class InverseRenderTransformPanel : Panel
{
    static InverseRenderTransformPanel()
    {
        RenderTransformProperty.Changed.AddClassHandler<InverseRenderTransformPanel>((panel, e) =>
        {
            panel.InvalidateMeasure();
        });
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        if (RenderTransform == null)
            return size;
        return new Size(size.Width * RenderTransform.Value.M11, size.Height * RenderTransform.Value.M22);
    }
}