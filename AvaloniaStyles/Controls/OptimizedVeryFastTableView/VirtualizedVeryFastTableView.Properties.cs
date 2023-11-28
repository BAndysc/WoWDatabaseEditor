using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public class ColumnPressedEventArgs : RoutedEventArgs
{
    public int ColumnIndex { get; set; } = -1;
    public ITableColumnHeader? Column { get; set; }
    public KeyModifiers KeyModifiers { get; set; }
}

public partial class VirtualizedVeryFastTableView
{
    public event System.Action<string>? ValueUpdateRequest;
    public static readonly StyledProperty<IReadOnlyList<int>?> HiddenColumnsProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, IReadOnlyList<int>?>(nameof(HiddenColumns));
    public static readonly StyledProperty<IReadOnlyList<ITableColumnHeader>?> ColumnsProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, IReadOnlyList<ITableColumnHeader>?>(nameof(Columns));
    public static readonly StyledProperty<int> SelectedRowIndexProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, int>(nameof(SelectedRowIndex), defaultValue: -1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int> ItemsCountProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, int>(nameof(ItemsCount), defaultValue:0, defaultBindingMode: BindingMode.OneWay);
    public static readonly StyledProperty<int> SelectedCellIndexProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, int>(nameof(SelectedCellIndex), defaultValue:-1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IMultiIndexContainer> MultiSelectionProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, IMultiIndexContainer>(nameof(MultiSelection), defaultValue: new MultiIndexContainer());
    public static readonly StyledProperty<bool> InteractiveHeaderProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, bool>(nameof(InteractiveHeader));
    public static readonly StyledProperty<IVirtualizedTableController> ControllerProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, IVirtualizedTableController>(nameof(Controller), defaultValue: new NullTableController());
    public static readonly RoutedEvent<ColumnPressedEventArgs> ColumnPressedEvent = RoutedEvent.Register<VirtualizedVeryFastTableView, ColumnPressedEventArgs>(nameof(ColumnPressed), RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    private List<bool> columnVisibility = new List<bool>();
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, bool>(nameof(IsReadOnly));
    public static readonly StyledProperty<bool> RequestRenderProperty = AvaloniaProperty.Register<VirtualizedVeryFastTableView, bool>(nameof(RequestRender));

    public IReadOnlyList<int>? HiddenColumns
    {
        get => GetValue(HiddenColumnsProperty);
        set => SetValue(HiddenColumnsProperty, value);
    }
    
    public event EventHandler<ColumnPressedEventArgs> ColumnPressed
    {
        add => AddHandler(ColumnPressedEvent, value, RoutingStrategies.Direct | RoutingStrategies.Bubble, false);
        remove => RemoveHandler(ColumnPressedEvent, value);
    }
    
    public int ItemsCount
    {
        get => GetValue(ItemsCountProperty);
        set => SetValue(ItemsCountProperty, value);
    }
    
    public IReadOnlyList<ITableColumnHeader>? Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public int SelectedRowIndex
    {
        get => GetValue(SelectedRowIndexProperty);
        set => SetValue(SelectedRowIndexProperty, value);
    }

    public int SelectedCellIndex
    {
        get => GetValue(SelectedCellIndexProperty);
        set => SetValue(SelectedCellIndexProperty, value);
    }
    
    public IMultiIndexContainer MultiSelection
    {
        get => GetValue(MultiSelectionProperty);
        set => SetValue(MultiSelectionProperty, value);
    }

    public bool InteractiveHeader
    {
        get => GetValue(InteractiveHeaderProperty);
        set => SetValue(InteractiveHeaderProperty, value);
    }
    
    public IVirtualizedTableController Controller
    {
        get => GetValue(ControllerProperty);
        set => SetValue(ControllerProperty, value);
    }
    
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }
    
    public bool RequestRender
    {
        get => (bool)GetValue(RequestRenderProperty);
        set => SetValue(RequestRenderProperty, value);
    }
}