using System;
using Avalonia;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private RecyclableViewList headerViews;
    private RecyclableViewList subheaderViews;

    private double lastStickyHeaderHeight = 0;
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        var viewPort = DataViewport.Translate(new Vector(0, -DrawingStartOffsetY)); // parents already take into account start drawing offset

        headersViewParent.Arrange(new Rect(0, viewPort.Y + DrawingStartOffsetY, finalSize.Width, finalSize.Height));
        subheadersViewParent.Arrange(new Rect(0, viewPort.Y + DrawingStartOffsetY, finalSize.Width, finalSize.Height));

        var visibleRect = new Rect(viewPort.X, 0, viewPort.Width, viewPort.Height);
        
        var template = GroupHeaderTemplate;

        if (template == null || Items == null)
        {
            lastStickyHeaderHeight = 0;
            return finalSize;
        }
        
        headerViews.Reset(template);
        subheaderViews.Reset(AdditionalGroupSubHeader);

        if (!IsGroupingEnabled)
        {
            lastStickyHeaderHeight = 0;
            headerViews.Finish();
            subheaderViews.Finish();
            return finalSize;
        }
        
        var bounds = Bounds;
        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;

        var items = Items;
        
        bool hasSticky = false;
        double y = -viewPort.Y; // DrawingStartOffsetY is already added in the parents' y position
        for (var index = 0; index < items.Count; index++)
        {
            var group = items[index];

            if (!IsFilteredGroupVisible(group, rowFilter, rowFilterParameter))
                continue;
            
            y += group.MarginTop;
            var headerHeight = GetHeaderHeight(index, items, rowFilter, rowFilterParameter);
            var subHeaderHeight = GetSubheaderHeight(index);
            var totalHeaderHeight = headerHeight + subHeaderHeight;
            var groupHeight = totalHeaderHeight;
            var headerRect = new Rect(visibleRect.X, y, visibleRect.Width, headerHeight);
            var subHeaderRect = new Rect(0, y + headerHeight, bounds.Width, subHeaderHeight);
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

            if (visibleRect.Intersects(groupRect) && groupRect.Y < visibleRect.Top)
            {
                var stickyHeaderRect = new Rect(visibleRect.X, 0, visibleRect.Width, headerHeight);
                lastStickyHeaderHeight = headerHeight;
                var headerControl = headerViews.GetNext(group);
                headerControl.Arrange(stickyHeaderRect);
                hasSticky = true;
            }
            else if (visibleRect.Intersects(headerRect) || (hasSticky && visibleRect.Intersects(headerRect)))
            {
                var headerControl = headerViews.GetNext(group);
                headerControl.Arrange(headerRect);
            }

            if (subHeaderHeight > 0 && visibleRect.Intersects(subHeaderRect))
            {
                if (subheaderViews.TryGetNext(group, out var subHeaderControl))
                    subHeaderControl.Arrange(subHeaderRect);
            }
        }

        headerViews.Finish();
        subheaderViews.Finish();
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