using Avalonia;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellDrawer
{
    bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell);
    void DrawRow(DrawingContext context, IFastTableContext table, ITableRow row, Rect rect);
    bool UpdateCursor(Point point, bool leftPressed) { return false; }
}

public interface IFastTableContext
{
    void InvalidateVisual();
}