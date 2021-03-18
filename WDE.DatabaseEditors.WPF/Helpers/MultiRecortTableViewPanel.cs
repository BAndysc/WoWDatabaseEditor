using System;
using System.Windows;
using System.Windows.Controls;

namespace WDE.DatabaseEditors.WPF.Helpers
{
    public class MultiRecortTableViewPanel : VirtualizingStackPanel
    {
        protected override Size MeasureOverride(Size constraint)
        {
            Size maxSize = new();
            
            foreach (UIElement item in InternalChildren)
            {
                item.Measure(constraint);
                maxSize.Height = Math.Max(item.DesiredSize.Height, maxSize.Height);
                maxSize.Width = Math.Max(item.DesiredSize.Width, maxSize.Width);
            }

            return maxSize;
        }

        // protected override Size ArrangeOverride(Size arrangeSize)
        // {
        //     foreach (UIElement item in InternalChildren)
        //     {
        //         item.Arrange(new Rect(arrangeSize));
        //     }
        //
        //     return arrangeSize;
        // }
    }
}