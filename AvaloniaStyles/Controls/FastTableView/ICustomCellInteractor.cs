using Avalonia;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellInteractor
{
    bool PointerDown(ITableCell cell, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount);
    bool PointerUp(ITableCell cell, Rect cellRect, Point mouse, bool leftButton, bool rightButton);
    bool SpawnEditorFor(string? initialText, Visual parent, Rect rect, ITableCell cell) => false;
}