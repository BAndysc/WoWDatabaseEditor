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

namespace AvaloniaStyles.Controls;

public class VirtualizedGridView : TemplatedControl
{
    private double itemHeight = 24;
    public static readonly DirectProperty<VirtualizedGridView, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, double>(nameof(ItemHeight), o => o.ItemHeight, (o, v) => o.ItemHeight = v);
    
    private IIndexedCollection<object> items = IIndexedCollection<object>.Empty;
    public static readonly DirectProperty<VirtualizedGridView, IIndexedCollection<object>> ItemsProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IIndexedCollection<object>>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    private int? selectedIndex;
    public static readonly DirectProperty<VirtualizedGridView, int?> SelectedIndexProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, int?>(nameof(SelectedIndex), o => o.SelectedIndex, (o, v) => o.SelectedIndex = v, -1, BindingMode.TwoWay);

    private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
    public static readonly DirectProperty<VirtualizedGridView, IEnumerable<GridColumnDefinition>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedGridView, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);
        
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
    
    public int? SelectedIndex
    {
        get => selectedIndex;
        set => SetAndRaise(SelectedIndexProperty, ref selectedIndex, value);
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

        if (e.Key == Key.Up)
        {
            SelectedIndex = IsIndexValid(selectedIndex) ? ClampIndex(selectedIndex!.Value - 1) : ClampIndex(Items.Count - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            SelectedIndex = IsIndexValid(selectedIndex) ? ClampIndex(selectedIndex!.Value + 1) : ClampIndex(0);
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
            SelectedIndex = index;
            return true;
        }

        return false;
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
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                    e.KeyModifiers.HasAllFlags(AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>().CommandModifiers),
                    point.Properties.IsRightButtonPressed);
            }
        }
    }
    
    private ScrollViewer? ScrollViewer => this.FindDescendantOfType<ScrollViewer>();
}