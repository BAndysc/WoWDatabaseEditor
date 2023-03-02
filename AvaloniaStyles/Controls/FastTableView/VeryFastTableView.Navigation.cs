using System;
using Avalonia.Input;
using WDE.Common.Utils;

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

    private bool IsGroupIndexValid(VerticalCursor cursor)
    {
        var items = Items;
        return items != null && cursor.GroupIndex >= 0 && cursor.GroupIndex < items.Count;
    }

    private bool IsRowIndexValid(VerticalCursor cursor)
    {
        var items = Items!;
        return IsGroupIndexValid(cursor) && cursor.RowIndex >= 0 && cursor.RowIndex < items[cursor.GroupIndex].Rows.Count;
    }
    
    private bool MoveCursorUpDown(int diff)
    {
        var index = SelectedRowIndex;
        if (Items == null)
            return false;

        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        var items = Items;
        
        for (int group = index.GroupIndex, row = index.RowIndex + diff; group < items.Count && group >= 0; group += diff)
        {
            if (IsGroupIndexValid(new VerticalCursor(group, row)))
            {
                for (;row < items[group].Rows.Count && row >= 0;)
                {
                    if (IsRowIndexValid(new VerticalCursor(group, row)))
                    {
                        if (IsFilteredRowVisible(items[group].Rows[row], rowFilter, rowFilterParameter))
                        {
                            SelectedRowIndex = new VerticalCursor(group, row);
                            return true;
                        }
                    }
                    
                    row += diff;
                }
                row = diff > 0 ? 0 : (IsGroupIndexValid(new VerticalCursor(group + diff, 0)) ? items[group + diff].Rows.Count - 1 : 0);
            }
        }

        return false;
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
                SelectedRowIndex = new VerticalCursor(0, 0);
                SelectedCellIndex = 0;
                break;
            case NavigationDirection.Last:
                SelectedRowIndex = new VerticalCursor(Items?.Count ?? 0 - 1, Items?[^1]?.Rows?.Count ?? 0 - 1);
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

    private void EnsureRowVisible(VerticalCursor row)
    {
        if (ScrollViewer == null)
            return;

        if (!row.IsValid)
            return;
        
        var viewRect = DataViewport;

        var headerHeight = (IsGroupingEnabled ? HeaderRowHeight : 0);
        var top = GetRowY(row);
        var bottom = top + RowHeight;
        if (top < viewRect.Top + headerHeight)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(top - (DrawingStartOffsetY + headerHeight));
        else if (bottom > viewRect.Bottom)
            ScrollViewer.Offset = ScrollViewer.Offset.WithY(bottom - ScrollViewer.Viewport.Height);
    }

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