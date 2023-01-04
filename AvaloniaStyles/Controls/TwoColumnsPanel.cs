using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls;

public class TwoColumnsPanel : Panel
{
    public static readonly StyledProperty<double> RowSpacingProperty = AvaloniaProperty.Register<TwoColumnsPanel, double>("RowSpacing", 10);
    public static readonly StyledProperty<double> ColumnSpacingProperty = AvaloniaProperty.Register<TwoColumnsPanel, double>("ColumnSpacing", 10);
    public static readonly StyledProperty<int> ColumnSpanProperty = AvaloniaProperty.RegisterAttached<TwoColumnsPanel, Control, int>("ColumnSpan");
    
    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public bool IgnoreOtherPanelsInMeasurement
    {
        get => GetValue(IgnoreOtherPanelsInMeasurementProperty);
        set => SetValue(IgnoreOtherPanelsInMeasurementProperty, value);
    }

    public static int GetColumnSpan(Control control) => control.GetValue(ColumnSpanProperty);

    public static void SetColumnSpan(Control control, int value) => control.SetValue(ColumnSpanProperty, value);

    static TwoColumnsPanel()
    {
        AffectsParentMeasure<TwoColumnsPanel>(IsVisibleProperty);
        AffectsParentArrange<TwoColumnsPanel>(IsVisibleProperty);
        VerticalAlignmentProperty.OverrideDefaultValue<TwoColumnsPanel>(VerticalAlignment.Top);
    }

    private IEnumerable<(Control left, Control? right)> GetChildren()
    {
        for (var index = 0; index < Children.Count; index++)
        {
            Control left;
            Control? right = null;
            left = Children[index];

            if (left.IsSet(ColumnSpanProperty))
            {
                var columnSpan = left.GetValue(ColumnSpanProperty);
                Debug.Assert(columnSpan == 2);
            }
            else
            {
                if (index + 1 < Children.Count)
                    right = Children[index + 1];
                index++;
            }

            if (!left.IsVisible && (right == null || !right.IsVisible))
                continue;
                
            yield return (left, right);
        }
    }

    public double MeasureLeftColumn(double? maxWidth = null)
    {
        double maxLeftWidth = 0;
        foreach (var (left, right) in GetChildren())
        {
            // left has ColumnSpan = 2 -> ignore arrange pass
            if (right == null)
                continue;
            
            left.Measure(new Size(maxWidth ?? 10000, 10000));
            maxLeftWidth = Math.Max(maxLeftWidth, left.DesiredSize.Width);
        }

        return maxLeftWidth;
    }

    private double currentLeftColumnWidth = 0;
    public static readonly StyledProperty<bool> IgnoreOtherPanelsInMeasurementProperty = AvaloniaProperty.Register<TwoColumnsPanel, bool>("IgnoreOtherPanelsInMeasurement");

    private double MeasureParentsLeftColumn()
    {
        double maxParentWidth = 0;
        var parent = this.GetVisualParent();
        while (parent != null)
        {
            if (parent is TwoColumnsPanel tcp)
                maxParentWidth = Math.Max(Math.Max(maxParentWidth, tcp.MeasureLeftColumn()), tcp.MeasureChildrenLeftColumns());
            parent = parent.GetVisualParent();
        }
        return maxParentWidth;
    }

    private double MeasureChildrenLeftColumns()
    {
        double maxChildWidth = 0;
        foreach (var inner in this.GetVisualDescendants().Select(x => x as TwoColumnsPanel)
                     .Where(x => x != null)
                     .Where(x => !x!.IgnoreOtherPanelsInMeasurement))
        {
            maxChildWidth = Math.Max(maxChildWidth, inner!.MeasureLeftColumn());
        }

        return maxChildWidth;
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        double maxLeftWidth = 0;
        double rowSpacing = RowSpacing;
        double height = -rowSpacing; // a trick to not have spacing in the last element
        double maxRightWidth = 0;

        if (IgnoreOtherPanelsInMeasurement)
            maxLeftWidth = MeasureLeftColumn();
        else
           maxLeftWidth = Math.Max(MeasureChildrenLeftColumns(), Math.Max(MeasureParentsLeftColumn(), MeasureLeftColumn()));
        currentLeftColumnWidth = maxLeftWidth;

        var rightAvailableSize = new Size(Math.Max(0, availableSize.Width - maxLeftWidth - ColumnSpacing), availableSize.Height);
        
        foreach (var (left, right) in GetChildren())
        {
            double leftHeight = 0;
            double rightHeight = 0;

            if (right != null)
            {
                left.Measure(new Size(maxLeftWidth, availableSize.Height));
                right.Measure(rightAvailableSize);
                rightHeight = right.DesiredSize.Height;
            }
            else
            {
                left.Measure(new Size(availableSize.Width, availableSize.Height));
            }

            leftHeight = left.DesiredSize.Height;
            height += Math.Max(leftHeight, rightHeight) + rowSpacing;
            maxRightWidth = Math.Max(maxRightWidth, right?.DesiredSize.Width ?? 0);
        }
        
        return new Size(maxRightWidth + maxLeftWidth, Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double rowSpacing = RowSpacing;

        var leftWidth = currentLeftColumnWidth;
        var rightWidth = Math.Max(0, finalSize.Width - currentLeftColumnWidth - ColumnSpacing);

        double y = 0;
        foreach (var (left, right) in GetChildren())
        {
            var leftHeight = left.DesiredSize.Height;
            
            if (right != null)
            {
                var rightHeight = right.DesiredSize.Height;
                var maxHeight = Math.Max(leftHeight, rightHeight);
                left.Arrange(new Rect(0, y, leftWidth, maxHeight));
                right.Arrange(new Rect(leftWidth + ColumnSpacing, y, rightWidth, maxHeight));
                y += maxHeight + rowSpacing;
            }
            else
            {
                left.Arrange(new Rect(0, y, finalSize.Width, leftHeight));
                y += leftHeight + rowSpacing;
            }
        }

        return new Size(finalSize.Width, y);
    }
}