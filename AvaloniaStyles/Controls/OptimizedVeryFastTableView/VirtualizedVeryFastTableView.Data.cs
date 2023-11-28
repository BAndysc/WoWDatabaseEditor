using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AvaloniaStyles.Controls.FastTableView;
using DynamicData;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView
{
    private void Rebind(IMultiIndexContainer? old, IMultiIndexContainer? @new)
    {
        if (old != null)
            old.Changed -= InvalidateVisual;
        if (@new != null)
            @new.Changed += InvalidateVisual;
        InvalidateVisual();
    }
    
    
    // --------------------------------------------------------------------------------------------
    
    private void RebindHiddenColumns(IReadOnlyList<int>? old, IReadOnlyList<int>? @new)
    {
        if (old != null)
        {
            if (old is INotifyCollectionChanged notify)
                notify.CollectionChanged -= HiddenRowChanged;
        }
        if (@new != null)
        {
            if (@new is INotifyCollectionChanged notify)
                notify.CollectionChanged += HiddenRowChanged;
            columnVisibility.Clear();
            foreach (var hidden in @new)
            {
                while (columnVisibility.Count <= hidden)
                    columnVisibility.Add(true);
                columnVisibility[hidden] = false;
            }
        }
        InvalidateMeasure();
        InvalidateVisual();
    }
    
    private void HiddenRowChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            foreach (int old in e.OldItems!)
            {
                while (old >= columnVisibility.Count)
                    columnVisibility.Add(true);
                columnVisibility[old] = true;
            }
        }
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
        {
            foreach (int @new in e.NewItems!)
            {
                while (@new >= columnVisibility.Count)
                    columnVisibility.Add(true);
                columnVisibility[@new] = false;
            }
        }
        InvalidateMeasure();
        InvalidateVisual();
    }
    
    public static int IndexOf<T>(IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            if (EqualityComparer<T>.Default.Equals(list[i], item))
                return i;
        }

        return -1;
    }
}