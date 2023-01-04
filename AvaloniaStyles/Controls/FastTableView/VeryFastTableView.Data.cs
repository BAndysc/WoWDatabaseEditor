using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Threading;
using DynamicData;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    private void Rebind(ITableMultiSelection? old, ITableMultiSelection? @new)
    {
        if (old != null)
            old.SelectionChanged -= InvalidateVisual;
        if (@new != null)
            @new.SelectionChanged += InvalidateVisual;
        InvalidateVisual();
    }
    
    private void Rebind(IReadOnlyList<ITableRowGroup>? old, IReadOnlyList<ITableRowGroup>? @new)
    {
        if (old != null)
        {
            if (old is INotifyCollectionChanged notify)
                notify.CollectionChanged -= ItemsChanged;
            foreach (var row in old)
            {
                row.RowChanged -= RowChanged;
                row.RowsChanged -= RowsChanged;
            }
        }
        if (@new != null)
        {
            if (@new is INotifyCollectionChanged notify)
                notify.CollectionChanged += ItemsChanged;
            foreach (var row in @new)
            {
                row.RowChanged += RowChanged;
                row.RowsChanged += RowsChanged;
            }
        }

        InvalidateMeasure();
        InvalidateVisual();
        InvalidateArrange();
    }

    private void RowsChanged(ITableRowGroup obj)
    {
        InvalidateMeasure();
        InvalidateVisual();
        InvalidateArrange();
        FixSelectedRowIndexIfInvalid();
        RemoveInvisibleFromSelection();
    }
    
    private void FixSelectedRowIndexIfInvalid()
    {
        if (SelectedRowIndex == VerticalCursor.None)
            return;
        if (Items == null)
        {
            SetCurrentValue(SelectedRowIndexProperty, VerticalCursor.None);
            return;
        }

        if (SelectedRowIndex.GroupIndex >= Items.Count)
        {
            SetCurrentValue(SelectedRowIndexProperty, VerticalCursor.None);
            return;
        }

        if (SelectedRowIndex.RowIndex >= Items[SelectedRowIndex.GroupIndex].Rows.Count)
            SetCurrentValue(SelectedRowIndexProperty, VerticalCursor.None);
    }

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
        InvalidateArrange();
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            foreach (ITableRowGroup old in e.OldItems!)
            {
                old.RowChanged -= RowChanged;
                old.RowsChanged -= RowsChanged;
            }
        }
        if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
        {
            foreach (ITableRowGroup @new in e.NewItems!)
            {
                @new.RowChanged += RowChanged;
                @new.RowsChanged += RowsChanged;
            }
        }
        if (e.Action == NotifyCollectionChangedAction.Reset)
            throw new Exception("RESET it not supported, because cannot unbind from old items");
    }

    private void RowChanged(ITableRowGroup group, ITableRow obj)
    {
        if (Items == null)
            return;

        var groupIndex = ListEx.IndexOf(Items, group);
        if (groupIndex == -1)
            return;
        
        var rowIndex = Items[groupIndex].Rows.IndexOf(obj);
        if (IsRowVisible(new VerticalCursor(groupIndex, rowIndex)))
        {
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
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