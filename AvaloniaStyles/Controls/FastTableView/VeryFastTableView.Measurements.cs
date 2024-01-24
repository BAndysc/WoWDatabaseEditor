using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    protected double GetTotalHeaderHeight(int index)
    {
        if (!IsGroupingEnabled)
            return 0;

        if (!IsFilteredGroupVisible(Items![index], RowFilter, RowFilterParameter))
            return 0;
        
        if (!IsDynamicHeaderHeight)
            return HeaderRowHeight + SubHeaderHeight;

        return HeaderRowHeight + SubHeaderHeight;
    }
    
    protected double GetHeaderHeight(int index)
    {
        return GetHeaderHeight(index, Items!, RowFilter, RowFilterParameter);   
    }

    protected double GetHeaderHeight(int index, IReadOnlyList<ITableRowGroup> items, IRowFilterPredicate? rowFilter, object? rowFilterParameter)
    {
        if (!IsGroupingEnabled)
            return 0;

        if (!IsFilteredGroupVisible(items![index], rowFilter, rowFilterParameter))
            return 0;
        
        if (!IsDynamicHeaderHeight)
            return HeaderRowHeight;

        return HeaderRowHeight;
    }

    protected double GetSubheaderHeight(int index)
    {
        if (!IsGroupingEnabled)
            return 0;
    
        if (!IsFilteredGroupVisible(Items![index], RowFilter, RowFilterParameter))
            return 0;
        
        if (!IsDynamicHeaderHeight)
            return SubHeaderHeight;
    
        return SubHeaderHeight;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Items == null)
            return new Size(0, 0);

        var height = DrawingStartOffsetY;
        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        int index = 0;
        foreach (var group in Items)
        {
            if (!IsFilteredGroupVisible(group, rowFilter, rowFilterParameter))
                continue;
            
            height += group.MarginTop;
            height += GetTotalHeaderHeight(index++);
            if (group.IsExpanded)
            {
                if (rowFilter == null)
                    height += group.Rows.Count * RowHeight;
                else
                {
                    foreach (var row in group.Rows)
                    {
                        if (IsFilteredRowVisible(group, row, rowFilter, rowFilterParameter))
                            height += RowHeight;
                    }
                }  
            }
        }

        availableSize = new Size(0, height + RowHeight * 2); // always add an extra row height for scrollbar
        if (Columns != null)
        {
            availableSize = availableSize.WithWidth(Columns.Where(c=>c.IsVisible).Select(c => c.Width).Sum());
        }
        return availableSize;
    }
}