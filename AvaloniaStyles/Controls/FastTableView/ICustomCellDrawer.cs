using Avalonia;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellDrawer
{
    bool Draw(DrawingContext context, Rect rect, ITableCell cell);
}