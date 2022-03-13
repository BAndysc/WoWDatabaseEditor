using Avalonia;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellDrawer
{
    bool Draw(DrawingContext context, Rect rect, ITableCell cell);
    void DrawRow(DrawingContext context, ITableRow row, Rect rect) {}
    bool UpdateCursor(Point point, bool leftPressed) { return false; }
}