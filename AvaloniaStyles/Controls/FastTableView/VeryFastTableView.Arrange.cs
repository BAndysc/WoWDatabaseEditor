using System;
using Avalonia;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private RecyclableViewList headerViews;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var template = GroupHeaderTemplate;
        
        if (template == null || Items == null)
            return base.ArrangeOverride(finalSize);
        
        headerViews.Reset(template);

        if (!IsGroupingEnabled)
        {
            headerViews.Finish();
            return finalSize;
        }
        
        var viewPort = DataViewport;
        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        var stickyHeaderRect = new Rect(viewPort.X, viewPort.Top, viewPort.Width, HeaderRowHeight);

        bool hasSticky = false;
        double y = DrawingStartOffsetY;
        foreach (var group in Items)
        {
            var headerRect = new Rect(viewPort.X, y, viewPort.Width, HeaderRowHeight);
            var groupHeight = HeaderRowHeight;
            if (group.IsExpanded)
            {
                if (rowFilter == null)
                    groupHeight += group.Rows.Count * RowHeight;
                else
                {
                    foreach (var row in group.Rows)
                        if (IsFilteredRowVisible(group, row, rowFilter, rowFilterParameter))
                            groupHeight += RowHeight;
                }   
            }
            var groupRect = new Rect(0, y, finalSize.Width, groupHeight);

            y += groupRect.Height;

            if (viewPort.Contains(headerRect) || (hasSticky && viewPort.Intersects(headerRect)))
            {
                var control = headerViews.GetNext(group);
                control.Arrange(headerRect);
            }
            else if (viewPort.Intersects(groupRect))
            {
                var control = headerViews.GetNext(group);
                control.Arrange(stickyHeaderRect);
                hasSticky = true;
            }
        }
        
        headerViews.Finish();
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