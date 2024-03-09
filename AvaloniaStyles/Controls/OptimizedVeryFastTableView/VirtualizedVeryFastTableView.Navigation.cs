using System;
using Avalonia.Input;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView
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
        SetCurrentValue(SelectedCellIndexProperty, next.Value);
        return true;
    }

    private bool MoveCursorLeft()
    {
        var prev = GetNextColumnInDirection(SelectedCellIndex, -1);
        if (!prev.HasValue)
            return false;
        SetCurrentValue(SelectedCellIndexProperty, prev.Value);
        return true;
    }
    private bool IsRowIndexValid(int row)
    {
        return row >= 0 && row < ItemsCount;
    }
    
    private bool MoveCursorUpDown(int diff)
    {
        var index = SelectedRowIndex;
        var newIndex = index + diff;
        if (!IsRowIndexValid(newIndex))
            return false;
        
        SetCurrentValue(SelectedRowIndexProperty, newIndex);
        return true;
    }

    private bool MoveCursorDown()
    {
        return MoveCursorUpDown(1);
    }

    private bool MoveCursorUp()
    {
        return MoveCursorUpDown(-1);
    }

    private bool MoveCursorPrevious()
    {
        if (MoveCursorLeft())
            return true;
        
        SetCurrentValue(SelectedCellIndexProperty, ColumnsCount - 1);
        return MoveCursorUp();
    }

    private bool MoveCursorNext()
    {
        if (MoveCursorRight())
            return true;
        SetCurrentValue(SelectedCellIndexProperty, 0);
        return MoveCursorDown();
    }
    
    public (bool handled, IInputElement? next) GetNext(IInputElement element, NavigationDirection direction)
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
                SetCurrentValue(SelectedRowIndexProperty, 0);
                SetCurrentValue(SelectedCellIndexProperty, 0);
                break;
            case NavigationDirection.Last:
                SetCurrentValue(SelectedRowIndexProperty, ItemsCount - 1);
                SetCurrentValue(SelectedCellIndexProperty, ColumnsCount - 1);
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
        return (false, null);
    }

    private void EnsureRowVisible(int rowIndex)
    {
        if (ScrollViewer == null)
            return;

        if (!IsRowIndexValid(rowIndex))
            return;
        
        var viewRect = DataViewport;

        var top = GetRowY(rowIndex);
        var bottom = top + RowHeight;
        if (top < viewRect.Top)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(top - (DrawingStartOffsetY));
        else if (bottom > viewRect.Bottom - ScrollBarHeight)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(bottom - ScrollViewer.Viewport.Height + ScrollBarHeight);
    }

    private const int ScrollBarHeight = 20;

    private void EnsureCellVisible(int cell)
    {
        if (ScrollViewer == null)
            return;
        var rect = GetColumnRect(cell);
        var left = rect.x;
        var right = left + rect.width;
        if (left < ScrollViewer.Offset.X)
            ScrollViewer.Offset = ScrollViewer.Offset.WithX(left);
        else if (right > ScrollViewer.Offset.X + ScrollViewer.Viewport.Width)
            ScrollViewer.Offset = ScrollViewer.Offset.WithX(right - ScrollViewer.Viewport.Width);
    }
}