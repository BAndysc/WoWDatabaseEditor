using System;
using Avalonia.Input;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private int? GetNextColumnInDirection(VerticalCursor currentRow, int start, int direction)
    {
        if (Columns == null)
            return null;

        var span = TableSpan;
        
        int result = start;
        do
        {
            if (span != null)
            {
                span.GetCellSpan(currentRow.GroupIndex, currentRow.RowIndex, result, out _, out var colSpan);
                result += direction * colSpan;
            }
            else
                result += direction;
        } while (result > 0 && result < Columns.Count - 1 && !IsColumnVisible(result));

        if (result != start && result >= 0 && result <= Columns.Count - 1 && IsColumnVisible(result))
            return result;
        return null;
    }
    
    private bool MoveCursorRight()
    {
        var next = GetNextColumnInDirection(SelectedRowIndex, SelectedCellIndex, 1);
        if (!next.HasValue)
            return false;
        
        if (TableSpan is { } span && span.IsMerged(SelectedRowIndex.GroupIndex, SelectedRowIndex.RowIndex, next.Value, out var firstRow, out var firstColumn))
        {
            next = firstColumn;
            SetCurrentValue(SelectedRowIndexProperty, new VerticalCursor(SelectedRowIndex.GroupIndex, firstRow));
        }

        SetCurrentValue(SelectedCellIndexProperty, next.Value);
        return true;
    }

    private bool MoveCursorLeft()
    {
        var prev = GetNextColumnInDirection(SelectedRowIndex, SelectedCellIndex, -1);
        if (!prev.HasValue)
            return false;
        
        if (TableSpan is { } span && span.IsMerged(SelectedRowIndex.GroupIndex, SelectedRowIndex.RowIndex, prev.Value, out var firstRow, out var firstColumn))
        {
            prev = firstColumn;
            SetCurrentValue(SelectedRowIndexProperty, new VerticalCursor(SelectedRowIndex.GroupIndex, firstRow));
        }

        SetCurrentValue(SelectedCellIndexProperty, prev.Value);
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
        var cellIndex = SelectedCellIndex;
        if (Items == null)
            return false;

        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        var items = Items;

        var span = TableSpan;
        
        int MoveCursor(int group, int startRow, int dir)
        {
            if (span == null)
                return startRow + dir;
            
            span.GetCellSpan(group, startRow, cellIndex, out var rowSpan, out _);
            return startRow + dir * rowSpan;
        }

        int FixRowToFirstMerged(int group, int row)
        {
            if (span == null)
                return row;

            if (span.IsMerged(group, row, cellIndex, out var firstRow, out _))
                return firstRow;

            return row;
        }
        
        for (int group = index.GroupIndex, row = MoveCursor(group, index.RowIndex, diff); group < items.Count && group >= 0; group += diff)
        {
            if (IsGroupIndexValid(new VerticalCursor(group, row)) &&
                IsFilteredGroupVisible(items[group], rowFilter, rowFilterParameter))
            {
                for (;row < items[group].Rows.Count && row >= 0;)
                {
                    if (IsRowIndexValid(new VerticalCursor(group, row)))
                    {
                        if (IsFilteredRowVisible(items[group], items[group].Rows[row], rowFilter, rowFilterParameter))
                        {
                            SetCurrentValue(SelectedRowIndexProperty, new VerticalCursor(group, row));
                            return true;
                        }
                    }

                    row = MoveCursor(group, row, diff);
                }
                row = diff > 0 ? 0 : (IsGroupIndexValid(new VerticalCursor(group + diff, 0)) ? items[group + diff].Rows.Count - 1 : 0);
                row = FixRowToFirstMerged(group, row);
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
                SetCurrentValue(SelectedRowIndexProperty, new VerticalCursor(0, 0));
                SetCurrentValue(SelectedCellIndexProperty, 0);
                break;
            case NavigationDirection.Last:
                SetCurrentValue(SelectedRowIndexProperty, new VerticalCursor(Items?.Count ?? 0 - 1, Items?[^1]?.Rows?.Count ?? 0 - 1));
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
    }

    private void EnsureRowVisible(VerticalCursor row)
    {
        if (ScrollViewer == null)
            return;

        if (!row.IsGroupValid)
            return;
        
        var viewRect = DataViewport;

        var headerHeight = GetTotalHeaderHeight(row.GroupIndex);
        var top = GetRowY(row);
        var bottom = top + RowHeight;
        if (TableSpan is { } span && row.IsValid)
        {
            span.GetCellSpan(row.GroupIndex, row.RowIndex, SelectedCellIndex, out var rowSpan, out _);
            bottom = top + rowSpan * RowHeight;
        }

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