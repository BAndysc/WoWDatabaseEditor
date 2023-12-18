using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = DrawingStartOffsetY;
        height += ItemsCount * RowHeight;

        availableSize = new Size(0, height + ScrollBarHeight * 2);
        if (Columns != null)
        {
            availableSize = availableSize.WithWidth(Columns.Where(c=>c.IsVisible).Select(c => c.Width).Sum());
        }
        return availableSize;
    }
}