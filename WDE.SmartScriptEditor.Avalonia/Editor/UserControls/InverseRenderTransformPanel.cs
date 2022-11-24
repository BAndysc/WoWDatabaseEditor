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
        if (RenderTransform == null)
            return base.MeasureOverride(availableSize);
        var size = base.MeasureOverride(new Size(availableSize.Width / RenderTransform.Value.M11, availableSize.Height));
        return new Size(size.Width , size.Height * RenderTransform.Value.M22); // * RenderTransform.Value.M11
    }
}