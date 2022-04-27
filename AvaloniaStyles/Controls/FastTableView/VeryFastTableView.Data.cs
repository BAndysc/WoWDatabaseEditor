using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DynamicData;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private void Rebind(IReadOnlyList<ITableRow>? old, IReadOnlyList<ITableRow>? @new)
    {
        if (old != null)
        {
            if (old is INotifyCollectionChanged notify)
                notify.CollectionChanged -= CollectionChanged;
            foreach (var row in old)
                row.Changed -= RowChanged;
        }
        if (@new != null)
        {
            if (@new is INotifyCollectionChanged notify)
                notify.CollectionChanged += CollectionChanged;
            foreach (var row in @new)
                row.Changed += RowChanged;
        }
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            foreach (ITableRow old in e.OldItems!)
            {
                old.Changed -= RowChanged;
            }
        }
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
        {
            foreach (ITableRow @new in e.NewItems!)
            {
                @new.Changed += RowChanged;
            }
        }
        if (e.Action == NotifyCollectionChangedAction.Reset)
            throw new Exception("RESET it not supported, because cannot unbind from old items");
    }

    private void RowChanged(ITableRow obj)
    {
        if (Rows == null)
            return;
        
        var index = Rows.IndexOf(obj);
        if (IsRowVisible(index))
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