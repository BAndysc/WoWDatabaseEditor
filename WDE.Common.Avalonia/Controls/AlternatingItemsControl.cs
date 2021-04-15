using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;

namespace WDE.Common.Avalonia.Controls
{
    public class AlternatingItemsControl : ItemsControl, IStyleable
    {
        Type IStyleable.StyleKey => typeof(ItemsControl);

        public AlternatingItemsControl()
        {
            LogicalChildren.CollectionChanged += LogicalChildrenOnCollectionChanged;
        }

        
        private void LogicalChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var child in LogicalChildren)
            {
                if (child is ContentPresenter cp)
                {
                    cp.ApplyTemplate();
                }
            }   
            Dispatcher.UIThread.Post(() =>
            {
                int i = 1;
                foreach (var child in Presenter.Panel.Children)
                {
                    if (child is ContentPresenter cp)
                    {
                        cp.Child.Classes.Set("odd", i % 2 == 1);
                        i++;
                    }
                }   
            });
        }
    }
}