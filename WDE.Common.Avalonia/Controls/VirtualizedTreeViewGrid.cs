using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;

namespace WDE.Common.Avalonia.Controls;

public class VirtualizedTreeViewGrid : Grid
{
    private delegate double GetPreferredSizeDelegate(DefinitionBase definition);
    
    static GetPreferredSizeDelegate GetPreferredSize;
    
    static VirtualizedTreeViewGrid()
    {
        GetPreferredSize = (GetPreferredSizeDelegate)Delegate.CreateDelegate(typeof(GetPreferredSizeDelegate),
            typeof(DefinitionBase)
            .GetProperty("PreferredSize", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetGetMethod(true)!);
    }
    
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        double leftBounds = 0.0;
        var that = this as Control;
        while (that != null)
        {
            leftBounds += that.Bounds.X;
            that = that.Parent as Control;
        }
        
        Span<double> lefts = stackalloc double[ColumnDefinitions.Count];
        Span<double> tops = stackalloc double[RowDefinitions.Count];

        double partialSum = 0;
        double tempBounds = leftBounds;
        for (int i = 0; i < ColumnDefinitions.Count; ++i)
        {
            lefts[i] = partialSum;
            var width = GetPreferredSize(ColumnDefinitions[i]);
            width -= tempBounds;
            if (width < 0)
            {
                tempBounds = -width;
                width = 0;
            }
            else
                tempBounds = 0;
            
            partialSum += width;
        }
        
        partialSum = 0;
        for (int i = 0; i < RowDefinitions.Count; ++i)
        {
            tops[i] = partialSum;
            partialSum += RowDefinitions[i].ActualHeight;
        }
        
        tempBounds = leftBounds;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;
            
            var rowIndex = Grid.GetRow(child);
            var columnIndex = Grid.GetColumn(child);

            var actualHeight = rowIndex < RowDefinitions.Count ? GetPreferredSize(RowDefinitions[rowIndex]) : arrangeSize.Height;
            var actualWidth = columnIndex < ColumnDefinitions.Count ? GetPreferredSize(ColumnDefinitions[columnIndex]) : arrangeSize.Width;
            
            actualWidth -= tempBounds;
            if (actualWidth < 0)
            {
                tempBounds = -actualWidth;
                actualWidth = 0;
            }
            else
                tempBounds = 0;
            
            child.Arrange(new Rect(columnIndex < lefts.Length ? lefts[columnIndex] : 0,  rowIndex < tops.Length ? tops[rowIndex] : 0, actualWidth, actualHeight));
        }

        return arrangeSize;
    }
}