using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WDE.Common.WPF.ViewHelpers
{
    // https://stackoverflow.com/a/35557401
    public class StretchedTreeView: TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new StretchedTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is StretchedTreeViewItem;
        }
    }

    public class StretchedTreeViewItem: TreeViewItem
    {
        public static readonly DependencyProperty AllowExpanderProperty =
            DependencyProperty.RegisterAttached("AllowExpander", typeof(bool), typeof(StretchedTreeViewItem),
                new UIPropertyMetadata(true, OnAllowExpanderChanged));
        
        public StretchedTreeViewItem()
        {
            this.Loaded += new RoutedEventHandler(StretchingTreeViewItem_Loaded);
        }

        private void StretchingTreeViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            // The purpose of this code is to stretch the Header Content all the way accross the TreeView.
            if (this.VisualChildrenCount > 0)
            {
                Grid? grid = this.GetVisualChild(0) as Grid;
                if (grid != null && grid.ColumnDefinitions.Count == 3)
                {
                    // Remove the middle column which is set to Auto and let it get replaced with the 
                    // last column that is set to Star.
                    grid.ColumnDefinitions.RemoveAt(1);
                    ChangeExpanderVisibility(grid, GetAllowExpander(this));
                }
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new StretchedTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is StretchedTreeViewItem;
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (GetAllowExpander(this))
                base.OnMouseDoubleClick(e);
        }

        [AttachedPropertyBrowsableForType(typeof(StretchedTreeViewItem))]
        public static bool GetAllowExpander(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowExpanderProperty);
        }

        [AttachedPropertyBrowsableForType(typeof(StretchedTreeViewItem))]
        public static void SetAllowExpander(DependencyObject obj, object value)
        {
            obj.SetValue(AllowExpanderProperty, value);
        }

        private static void ChangeExpanderVisibility(Grid grid, bool visible)
        {
            var expander = grid?.Children[0] as ToggleButton;
            if (expander != null)
                expander.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        
        private static void OnAllowExpanderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as StretchedTreeViewItem;
            if (item == null)
                throw new InvalidOperationException($"can only be attached to {nameof(StretchedTreeViewItem)}");
            
            if (item.VisualChildrenCount > 0)
            {
                Grid? grid = item.GetVisualChild(0) as Grid;
                if (grid != null)
                    ChangeExpanderVisibility(grid, GetAllowExpander(item));
            }
        }
    }
}
