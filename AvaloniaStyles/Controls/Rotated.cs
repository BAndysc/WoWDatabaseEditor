using Avalonia;
using Avalonia.Controls;

namespace AvaloniaStyles.Controls
{
    public class Rotated : Panel
    {

        protected override Size MeasureOverride(Size availableSize)
        {
            var b = base.MeasureOverride(availableSize);
            return new Size(b.Height, b.Width);
        }
    }
}