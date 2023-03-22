using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using WDE.Common.Collections;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls;

public class VirtualizedGridView : TemplatedControl
{
    private double itemHeight = 24;
    public static readonly DirectProperty<VirtualizedGridView, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, double>(nameof(ItemHeight), o => o.ItemHeight, (o, v) => o.ItemHeight = v);
    
    private IIndexedCollection<object> items = IIndexedCollection<object>.Empty;
    public static readonly DirectProperty<VirtualizedGridView, IIndexedCollection<object>> ItemsProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IIndexedCollection<object>>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    private IMultiIndexContainer selection = new MultiIndexContainer();
    public static readonly DirectProperty<VirtualizedGridView, IMultiIndexContainer> SelectionProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IMultiIndexContainer>(nameof(Selection), o => o.Selection, (o, v) => o.Selection = v, new MultiIndexContainer(), BindingMode.OneWay);

    private int? focusedIndex;
    public static readonly DirectProperty<VirtualizedGridView, int?> FocusedIndexProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, int?>(nameof(FocusedIndex), o => o.FocusedIndex, (o, v) => o.FocusedIndex = v, -1, BindingMode.TwoWay);

    private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
    public static readonly DirectProperty<VirtualizedGridView, IEnumerable<GridColumnDefinition>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedGridView, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);

    private bool multiSelect;
    public static readonly DirectProperty<VirtualizedGridView, bool> MultiSelectProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, bool>(nameof(MultiSelect), o => o.MultiSelect, (o, v) => o.MultiSelect = v);
    
    public double ItemHeight
    {
        get => itemHeight;
        set => SetAndRaise(ItemHeightProperty, ref itemHeight, value);
    }

    public IIndexedCollection<object> Items
    {
        get => items;
        set => SetAndRaise(ItemsProperty, ref items, value);
    }
    
    public IMultiIndexContainer Selection
    {
        get => selection;
        set => SetAndRaise(SelectionProperty, ref selection, value);
    }
    
    public int? FocusedIndex
    {
        get => focusedIndex;
        set => SetAndRaise(FocusedIndexProperty, ref focusedIndex, value);
    }
    
    public bool MultiSelect
    {
        get => multiSelect;
        set => SetAndRaise(MultiSelectProperty, ref multiSelect, value);
    }
        
    public IEnumerable<GridColumnDefinition> Columns
    {
        get => columns;
        set => SetAndRaise(ColumnsProperty, ref columns, value);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        if (e.Key is Key.Up or Key.Down)
        {
            var rangeModifier = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift);
            var toggleModifier = e.KeyModifiers.HasFlagFast(AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>().CommandModifiers);

            int index;
            if (e.Key == Key.Up)
            {
                index = IsIndexValid(focusedIndex) ? ClampIndex(focusedIndex!.Value - 1) : ClampIndex(Items.Count - 1);
            }
            else
            {
                index = IsIndexValid(focusedIndex) ? ClampIndex(focusedIndex!.Value + 1) : ClampIndex(0);
            }
            UpdateSelectionFromIndex(rangeModifier, toggleModifier, index);
            e.Handled = true;
        }
    }

    private bool IsIndexValid(int? index) => index != null && index >= 0 && index < Items.Count;

    private int ClampIndex(int index) => Math.Max(0, Math.Min(index, Items.Count - 1));

    protected ListBoxItem? GetContainerFromEventSource(object? eventSource)
    {
        for (var current = eventSource as IVisual; current != null; current = current.GetVisualParent())
        {
            if (current is ListBoxItem lbi)
            {
                return lbi;
            }
        }

        return null;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        var header = e.NameScope.Find<Grid>("PART_header");
                
        header.ColumnDefinitions.Clear();
        header.Children.Clear();
        VirtualizedGridViewItemPresenter.SetupGridColumns(columns, header, true);

        // additional column in header makes it easier to resize
        header.ColumnDefinitions.Add(new ColumnDefinition(20, GridUnitType.Pixel));
            
        int i = 0;
        foreach (var column in Columns)
        {
            var headerCell = new GridViewColumnHeader()
            {
                ColumnName = column.Name
            };
            headerCell.ColumnName = column.Name;
            Grid.SetColumn(headerCell, i++);
            header.Children.Add(headerCell);
                    
            var splitter = new GridSplitter();
            splitter.Focusable = false;
            Grid.SetColumn(splitter, i++);
            header.Children.Add(splitter);
        }
    }

    protected bool UpdateSelectionFromEventSource(
        object? eventSource,
        bool select = true,
        bool rangeModifier = false,
        bool toggleModifier = false,
        bool rightButton = false,
        bool fromFocus = false)
    {
        var container = GetContainerFromEventSource(eventSource);

        if (container != null && container.Tag is int index)
        {
            UpdateSelectionFromIndex(rangeModifier, toggleModifier, index);
            return true;
        }

        return false;
    }

    private void UpdateSelectionFromIndex(bool rangeModifier, bool toggleModifier, int index)
    {
        rangeModifier &= MultiSelect;
        toggleModifier &= MultiSelect;

        if (!toggleModifier)
            selection.Clear();

        if (rangeModifier)
        {
            var oldFocusedIndex = Math.Max(0, focusedIndex ?? 0);
            if (oldFocusedIndex < index)
            {
                for (int i = oldFocusedIndex; i <= index; ++i)
                    selection.Add(i);
            }
            else
            {
                for (int i = index; i <= oldFocusedIndex; ++i)
                    selection.Add(i);
            }
        }
        else
        {
            if (toggleModifier && selection.Contains(index))
                selection.Remove(index);
            else
                selection.Add(index);
            FocusedIndex = index;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Source is Visual source)
        {
            var point = e.GetCurrentPoint(source);

            if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasFlagFast(KeyModifiers.Shift),
                    e.KeyModifiers.HasFlagFast(AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>().CommandModifiers),
                    point.Properties.IsRightButtonPressed);
            }
        }
    }
    
    private ScrollViewer? ScrollViewer => this.FindDescendantOfType<ScrollViewer>();
}