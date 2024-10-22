using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using DynamicData;
using WDE.Common.Collections;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls;

public class VirtualizedGridView : TemplatedControl
{
    private double itemHeight = 24;
    public static readonly DirectProperty<VirtualizedGridView, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, double>(nameof(ItemHeight), o => o.ItemHeight, (o, v) => o.ItemHeight = v);
    
    private IIndexedCollection<object> items = IIndexedCollection<object>.Empty;
    public static readonly DirectProperty<VirtualizedGridView, IIndexedCollection<object>> ItemsProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IIndexedCollection<object>>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    public static readonly StyledProperty<int?> FocusedIndexProperty = AvaloniaProperty.Register<VirtualizedGridView, int?>(nameof(FocusedIndex));
    
    private IMultiIndexContainer selection = new MultiIndexContainer();
    public static readonly DirectProperty<VirtualizedGridView, IMultiIndexContainer> SelectionProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IMultiIndexContainer>(nameof(Selection), o => o.Selection, (o, v) => o.Selection = v, new MultiIndexContainer(), BindingMode.OneWay);

    private IMultiIndexContainer checkedIndices = new MultiIndexContainer();
    public static readonly DirectProperty<VirtualizedGridView, IMultiIndexContainer> CheckedIndicesProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, IMultiIndexContainer>(nameof(CheckedIndices), o => o.CheckedIndices, (o, v) => o.CheckedIndices = v, new MultiIndexContainer(), BindingMode.OneWay);

    private IEnumerable<GridColumnDefinition> columns = new AvaloniaList<GridColumnDefinition>();
    public static readonly DirectProperty<VirtualizedGridView, IEnumerable<GridColumnDefinition>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<VirtualizedGridView, IEnumerable<GridColumnDefinition>>(nameof(Columns), o => o.Columns, (o, v) => o.Columns = v);

    private bool multiSelect;
    public static readonly DirectProperty<VirtualizedGridView, bool> MultiSelectProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, bool>(nameof(MultiSelect), o => o.MultiSelect, (o, v) => o.MultiSelect = v);
    
    private bool useCheckBoxes;
    public static readonly DirectProperty<VirtualizedGridView, bool> UseCheckBoxesProperty = AvaloniaProperty.RegisterDirect<VirtualizedGridView, bool>(nameof(UseCheckBoxes), o => o.UseCheckBoxes, (o, v) => o.UseCheckBoxes = v);

    public static readonly RoutedEvent<RoutedEventArgs> ColumnWidthChangedEvent =
        RoutedEvent.Register<VirtualizedGridView, RoutedEventArgs>(nameof(ColumnWidthChanged), RoutingStrategies.Bubble);

    static VirtualizedGridView()
    {
        FocusableProperty.OverrideDefaultValue<VirtualizedGridView>(true);
    }

    public event EventHandler<RoutedEventArgs> ColumnWidthChanged
    {
        add => AddHandler(ColumnWidthChangedEvent, value);
        remove => RemoveHandler(ColumnWidthChangedEvent, value);
    }

#pragma warning disable AVP1030
    public int? FocusedIndex
    {
        get
        {
            var index = GetValue(FocusedIndexProperty);
            if (!IsIndexValid(index))
                return null;
            return index;
        }
        set => SetValue(FocusedIndexProperty, value);
    }
#pragma warning restore AVP1030

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
    
    public IMultiIndexContainer CheckedIndices
    {
        get => checkedIndices;
        set => SetAndRaise(CheckedIndicesProperty, ref checkedIndices, value);
    }

    public bool MultiSelect
    {
        get => multiSelect;
        set => SetAndRaise(MultiSelectProperty, ref multiSelect, value);
    }
    
    public bool UseCheckBoxes
    {
        get => useCheckBoxes;
        set => SetAndRaise(UseCheckBoxesProperty, ref useCheckBoxes, value);
    }
    
    public IEnumerable<GridColumnDefinition> Columns
    {
        get => columns;
        set => SetAndRaise(ColumnsProperty, ref columns, value);
    }

    private VirtualizedGridViewItemPresenter? children;
    private Grid? header;
    private ScrollViewer? headerScroll;
    private ScrollViewer? contentScroll;

    public VirtualizedGridViewItemPresenter? ItemPresenter => children;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        if (e.Key is Key.Up or Key.Down)
        {
            var rangeModifier = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift);
            var toggleModifier = e.KeyModifiers.HasFlagFast(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? KeyModifiers.Meta : KeyModifiers.Control);

            int index;
            if (e.Key == Key.Up)
            {
                index = IsIndexValid(FocusedIndex) ? ClampIndex(FocusedIndex!.Value - 1) : ClampIndex(Items.Count - 1);
            }
            else
            {
                index = IsIndexValid(FocusedIndex) ? ClampIndex(FocusedIndex!.Value + 1) : ClampIndex(0);
            }
            if (IsIndexValid(index))
                UpdateSelectionFromIndex(rangeModifier, toggleModifier, index);
            e.Handled = true;
        }
    }

    private bool IsIndexValid(int? index) => index != null && index >= 0 && index < Items.Count;

    private int ClampIndex(int index) => Math.Max(0, Math.Min(index, Items.Count - 1));

    protected ListBoxItem? GetContainerFromEventSource(object? eventSource)
    {
        for (var current = eventSource as Visual; current != null; current = current.GetVisualParent())
        {
            if (current is ListBoxItem lbi)
            {
                return lbi;
            }
        }

        return null;
    }

    public IEnumerable<int> GetColumnsWidth()
    {
        if (header == null)
            yield break;
        for (var i = useCheckBoxes ? 2 : 0; // first two columns are checkboxes and splitter, they are not customizable, so skip
             i < header.ColumnDefinitions.Count - 1; // -1, because last column is an empty space
             i += 2) // every second column is a splitter
        {
            var column = header.ColumnDefinitions[i];
            yield return (int)column.Width.Value;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (headerScroll is not null)
            headerScroll.ScrollChanged -= OnHeaderScroll;
        if (contentScroll is not null)
            contentScroll.ScrollChanged -= OnContentScroll;
        
        header = e.NameScope.Get<Grid>("PART_header");
        children = e.NameScope.Get<VirtualizedGridViewItemPresenter>("PART_Children");
        headerScroll = e.NameScope.Get<ScrollViewer>("PART_HeaderScroll");
        contentScroll = e.NameScope.Get<ScrollViewer>("PART_ContentScroll");

        header.ColumnDefinitions.Clear();
        header.Children.Clear();

        headerScroll.ScrollChanged += OnHeaderScroll;
        contentScroll.ScrollChanged += OnContentScroll;
        
        VirtualizedGridViewItemPresenter.SetupGridColumns(columns, header, true, useCheckBoxes);

        int i = 0;
        void AddHeaderCell(string name, bool last = false)
        {
            var headerCell = new GridViewColumnHeader()
            {
                ColumnName = name
            };
            headerCell.ColumnName = name;
            headerCell.GetObservable(BoundsProperty).SubscribeAction(_ =>
            {
                ColumnsSizeChanged();
            });
            Grid.SetColumn(headerCell, i++);
            header.Children.Add(headerCell);

            if (last)
                return;

            var splitter = new GridSplitter();
            splitter.Focusable = false;
            Grid.SetColumn(splitter, i++);
            header.Children.Add(splitter);
        }

        if (useCheckBoxes)
            AddHeaderCell("");
        
        foreach (var column in Columns)
            AddHeaderCell(column.Name);

        AddHeaderCell("", true);
    }

    private void OnContentScroll(object? sender, ScrollChangedEventArgs e)
    {
        if (contentScroll is not null && headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
            headerScroll.Offset = headerScroll.Offset.WithX(contentScroll.Offset.X);
    }

    private void OnHeaderScroll(object? sender, ScrollChangedEventArgs e)
    {
        if (contentScroll is not null && headerScroll is not null && !MathUtilities.IsZero(e.OffsetDelta.X))
            contentScroll.Offset = contentScroll.Offset.WithX(headerScroll.Offset.X);
    }

    private void ColumnsSizeChanged()
    {
        RaiseEvent(new RoutedEventArgs()
        {
            RoutedEvent = ColumnWidthChangedEvent,
            Source = this
        });
        UpdateChildWidth(Bounds.Width);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateChildWidth(finalSize.Width);
        return base.ArrangeOverride(finalSize);
    }

    private void UpdateChildWidth(double controlWidth)
    {
        int width = 20; // 20 extra width for the last column
        foreach (var column in GetColumnsWidth())
            width += column + GridView.SplitterWidth;
        if (children != null)
            children.Width = Math.Max(width, controlWidth - 2);
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

        if (container != null && container.Tag is int index && IsIndexValid(index))
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

        if (!toggleModifier && !rangeModifier)
            selection.Clear();

        if (rangeModifier)
        {
            var oldFocusedIndex = Math.Max(0, FocusedIndex ?? 0);
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
            SetCurrentValue(FocusedIndexProperty, index);
        }
        else
        {
            if (toggleModifier && selection.Contains(index))
                selection.Remove(index);
            else
                selection.Add(index);
            SetCurrentValue(FocusedIndexProperty, index);
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
                    e.KeyModifiers.HasFlagFast(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? KeyModifiers.Meta : KeyModifiers.Control),
                    point.Properties.IsRightButtonPressed);
            }
        }
    }
    
    private ScrollViewer? ScrollViewer => this.FindDescendantOfType<ScrollViewer>();

}
