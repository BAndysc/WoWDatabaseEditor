using System;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private int? GetNextColumnInDirection(int start, int direction)
    {
        if (Columns == null)
            return null;

        int result = start;
        do
        {
            result += direction;
        } while (result > 0 && result < Columns.Count - 1 && !IsColumnVisible(result));

        if (result != start && result >= 0 && result <= Columns.Count - 1 && IsColumnVisible(result))
            return result;
        return null;
    }
    
    private bool MoveCursorRight()
    {
        var next = GetNextColumnInDirection(SelectedCellIndex, 1);
        if (!next.HasValue)
            return false;
        SelectedCellIndex = next.Value;
        return true;
    }

    private bool MoveCursorLeft()
    {
        var prev = GetNextColumnInDirection(SelectedCellIndex, -1);
        if (!prev.HasValue)
            return false;
        SelectedCellIndex = prev.Value;
        return true;
    }
    
    private bool MoveCursorDown()
    {
        if (Rows == null || SelectedRowIndex == Rows?.Count - 1)
            return false;
        SelectedRowIndex++;
        return true;
    }

    private bool MoveCursorUp()
    {
        if (SelectedRowIndex == 0)
            return false;
        SelectedRowIndex--;
        return true;
    }

    private bool MoveCursorPrevious()
    {
        if (MoveCursorLeft())
            return true;
        
        SelectedCellIndex = ColumnsCount - 1;
        return MoveCursorUp();
    }

    private bool MoveCursorNext()
    {
        if (MoveCursorRight())
            return true;
        SelectedCellIndex = 0;
        return MoveCursorDown();
    }


    public void SetOwner(IInputRoot owner)
    {
        
    }

    public void Move(IInputElement element, NavigationDirection direction, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        switch (direction)
        {
            case NavigationDirection.Next:
                MoveCursorNext();
                break;
            case NavigationDirection.Previous:
                MoveCursorPrevious();
                break;
            case NavigationDirection.First:
                SelectedRowIndex = 0;
                SelectedCellIndex = 0;
                break;
            case NavigationDirection.Last:
                SelectedRowIndex = Rows?.Count - 1 ?? - 1;
                SelectedCellIndex = ColumnsCount - 1;
                break;
            case NavigationDirection.Left:
                MoveCursorLeft();
                break;
            case NavigationDirection.Right:
                MoveCursorNext();
                break;
            case NavigationDirection.Up:
                MoveCursorUp();
                break;
            case NavigationDirection.Down:
                MoveCursorDown();
                break;
            case NavigationDirection.PageUp:
                MoveCursorUp();
                break;
            case NavigationDirection.PageDown:
                MoveCursorDown();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
        MultiSelection.Clear();
        MultiSelection.Add(SelectedRowIndex);
    }

    private void EnsureRowVisible(int row)
    {
        var top = row * RowHeight + DrawingStartOffsetY;
        var bottom = top + RowHeight;
        if (top < ScrollViewer.Offset.Y + DrawingStartOffsetY * 2)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(top - DrawingStartOffsetY);
        else if (bottom > ScrollViewer.Offset.Y + ScrollViewer.Viewport.Height)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(bottom - ScrollViewer.Viewport.Height);
    }

    private void EnsureCellVisible(int cell)
    {
        var rect = GetColumnRect(cell);
        var left = rect.x;
        var right = left + rect.width;
        if (left < ScrollViewer.Offset.X)
            ScrollViewer.Offset = ScrollViewer.Offset.WithX(left);
        else if (right > ScrollViewer.Offset.X + ScrollViewer.Viewport.Width)
            ScrollViewer.Offset = ScrollViewer.Offset.WithX(right - ScrollViewer.Viewport.Width);
    }
}