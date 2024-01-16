using Avalonia;
using AvaloniaStyles.Controls.FastTableView;

namespace WDE.LootEditor.Picker.Views;

public class CustomCellInteractor : ICustomCellInteractor
{
    private bool IsReadOnly(ITableCell cell)
    {
        return true;
    }

    public bool SpawnEditorFor(ITableRow row, string? initialText, Visual parent, Rect rect, ITableCell c)
    {
        return IsReadOnly(c);
    }

    public bool PointerDown(ITableRow row, ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount)
    {
        return false;
    }
    
    public bool PointerUp(ITableRow row, ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton)
    {
        return false;
    }
}