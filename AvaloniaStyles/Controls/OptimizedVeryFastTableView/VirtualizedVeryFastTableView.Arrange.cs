using System;
using Avalonia;
using AvaloniaStyles.Controls.FastTableView;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }

    private const double ColumnSplitterPadding = 8;

    protected bool IsOverColumnSplitter(double x, out int columnIndex)
    {
        columnIndex = -1;
        var columns = Columns;
        if (columns == null)
            return false;

        for (int i = 0; i < columns.Count; ++i)
        {
            if (Math.Abs(columns[i].Width - x) <= ColumnSplitterPadding)
            {
                columnIndex = i;
                return true;
            }

            x -= columns[i].Width;
        }

        return false;
    }

    protected bool IsOverColumn(double x, out ITableColumnHeader column, out int columnIndex)
    {
        column = null!;
        columnIndex = -1;
        var columns = Columns;
        if (columns == null)
            return false;

        for (int i = 0; i < columns.Count; ++i)
        {
            if (x <= columns[i].Width)
            {
                columnIndex = i;
                column = columns[i];
                return true;
            }

            x -= columns[i].Width;
        }

        return false;
    }
}