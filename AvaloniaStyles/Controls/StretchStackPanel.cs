using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaloniaStyles.Controls;

public class StretchStackPanel : Panel
{
    public static readonly StyledProperty<double> SpacingProperty = StackLayout.SpacingProperty.AddOwner<StretchStackPanel>();
    
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public static readonly StyledProperty<bool> StretchProperty = AvaloniaProperty.Register<StretchStackPanel, bool>(nameof(Stretch), true);

    public bool Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    static StretchStackPanel()
    {
        AffectsArrange<StretchStackPanel>(StretchProperty, SpacingProperty);
        AffectsMeasure<StretchStackPanel>(StretchProperty, SpacingProperty);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Avalonia.Controls.Controls children = Children;
        int visibleChildrenCount = 0;
        int index = 0;
        for (int count = children.Count; index < count; ++index)
        {
            Control control = children[index];
            if (control == null)
                continue;
            
            bool isVisible = control.IsVisible;
            if (isVisible)
                visibleChildrenCount++;
        }
        
        double singleWidth = (finalSize.Width - Spacing * Math.Max(0, visibleChildrenCount - 1)) / visibleChildrenCount;
        double x = 0;
        double widthLeft = finalSize.Width;
        bool stretch = Stretch;
        
        index = 0;
        for (int count = children.Count; index < count; ++index)
        {
            Control control = children[index];
            if (control == null)
                continue;
            
            bool isVisible = control.IsVisible;

            if (!isVisible)
                continue;

            var width = stretch ? singleWidth : Math.Clamp(control.DesiredSize.Width, 0, widthLeft);
            control.Arrange(new Rect(x, finalSize.Height / 2 - control.DesiredSize.Height/2, width, control.DesiredSize.Height));
            x += width + Spacing;
            widthLeft -= width + Spacing;
        }

        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size finalSize = new Size();
        Avalonia.Controls.Controls children = Children;
        Size size2 = availableSize;
        bool isHorizontal = true; //this.Orientation == Orientation.Horizontal;
        double spacing = Spacing;
        bool any = false;
        Size availableSize1 = !isHorizontal ? size2.WithHeight(double.PositiveInfinity) : size2.WithWidth(double.PositiveInfinity);
        int index = 0;
        for (int count = children.Count; index < count; ++index)
        {
            Control control = children[index];
            if (control != null)
            {
                bool isVisible = control.IsVisible;
                if (isVisible && !any)
                    any = true;
                control.Measure(availableSize1);
                Size desiredSize = control.DesiredSize;
                if (isHorizontal)
                {
                    finalSize = finalSize.WithWidth(finalSize.Width + (isVisible ? spacing : 0.0) + desiredSize.Width);
                    finalSize = finalSize.WithHeight(Math.Max(finalSize.Height, desiredSize.Height));
                }
                else
                {
                    finalSize = finalSize.WithWidth(Math.Max(finalSize.Width, desiredSize.Width));
                    finalSize = finalSize.WithHeight(finalSize.Height + (isVisible ? spacing : 0.0) + desiredSize.Height);
                }
            }
        }
        finalSize = !isHorizontal ? finalSize.WithHeight(finalSize.Height - (any ? spacing : 0.0)) : finalSize.WithWidth(finalSize.Width - (any ? spacing : 0.0));
        return finalSize;
    }
}