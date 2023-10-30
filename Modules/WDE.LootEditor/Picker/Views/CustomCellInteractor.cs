using Avalonia;
using AvaloniaStyles.Controls.FastTableView;

namespace WDE.LootEditor.Picker.Views;

public class CustomCellInteractor : ICustomCellInteractor
{
    private bool IsReadOnly(ITableCell cell)
    {
        return true;
    }

    public bool SpawnEditorFor(string? initialText, Visual parent, Rect rect, ITableCell c)
    {
        return IsReadOnly(c);
    }

    public bool PointerDown(ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount)
    {
        return false;
    }
    
    public bool PointerUp(ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton)
    {
        return false;
    }
}