using Avalonia;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellInteractor
{
    bool PointerDown(ITableRow row, ITableCell cell, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount);
    bool PointerUp(ITableRow row, ITableCell cell, Rect cellRect, Point mouse, bool leftButton, bool rightButton);
    bool SpawnEditorFor(ITableRow row, string? initialText, Visual parent, Rect rect, ITableCell cell) => false;
}