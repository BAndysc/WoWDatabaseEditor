using Avalonia;
using Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls;

public class NoArrangePanel : Panel
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(0, 0);
    }
}