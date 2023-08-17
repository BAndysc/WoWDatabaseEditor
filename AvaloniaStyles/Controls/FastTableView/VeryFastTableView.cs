using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView : Panel, IKeyboardNavigationHandler, IFastTableContext
{
    private static double ColumnSpacing = 10;
    private static double RowHeight = 28;
    private static double HeaderRowHeight = 36;
    private static double DrawingStartOffsetY = RowHeight;
    private static ISolidColorBrush OddRowBackground = new SolidColorBrush(Colors.White);
    private static ISolidColorBrush SelectedRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    private static ISolidColorBrush EvenRowBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    private static ISolidColorBrush HeaderBackground = new SolidColorBrush(Colors.White);
    private static ISolidColorBrush HeaderPressedBackground = new SolidColorBrush(Colors.Gray);
    private static ISolidColorBrush HeaderHoverBackground = new SolidColorBrush(Colors.LightGray);
    private static IPen HeaderBorderBackground = new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 1);
    private static IPen BorderPen = new Pen(new SolidColorBrush(Colors.Black), 1);
    private static IPen FocusPen = new Pen(new SolidColorBrush(Colors.Black), 2);
    private static IPen FocusOuterPen = new Pen(new SolidColorBrush(Colors.White), 3);
    private static ISolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
    private static ISolidColorBrush FocusTextBrush = OddRowBackground;

    public int ColumnsCount => Columns?.Count ?? 0;

    private PhantomTextBox editor = new();

    private static bool GetResource<T>(string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current.Styles.TryGetResource(key, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        return false;
    }
    
    static VeryFastTableView()
    {
        GetResource("FastTableView.OddRowBackground", OddRowBackground, out OddRowBackground);
        GetResource("FastTableView.SelectedRowBackground", SelectedRowBackground, out SelectedRowBackground);
        GetResource("FastTableView.EvenRowBackground", EvenRowBackground, out EvenRowBackground);
        GetResource("FastTableView.HeaderBackground", HeaderBackground, out HeaderBackground);
        GetResource("FastTableView.HeaderHoverBackground", HeaderHoverBackground, out HeaderHoverBackground);
        GetResource("FastTableView.HeaderBorderBackground", HeaderBorderBackground, out HeaderBorderBackground);
        GetResource("FastTableView.HeaderPressedBackground", HeaderPressedBackground, out HeaderPressedBackground);
        GetResource("FastTableView.BorderPen", BorderPen, out BorderPen);
        GetResource("FastTableView.FocusPen", FocusPen, out FocusPen);
        GetResource("FastTableView.FocusOuterPen", FocusOuterPen, out FocusOuterPen);
        GetResource("FastTableView.TextBrush", TextBrush, out TextBrush);
        GetResource("FastTableView.FocusTextBrush", FocusTextBrush, out FocusTextBrush);
        
        ItemsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.Rebind(e.OldValue as IReadOnlyList<ITableRowGroup>, e.NewValue as IReadOnlyList<ITableRowGroup>);
        });
        MultiSelectionProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.Rebind(e.OldValue as ITableMultiSelection, e.NewValue as ITableMultiSelection);
        });
        HiddenColumnsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.RebindHiddenColumns(e.OldValue as IReadOnlyList<int>, e.NewValue as IReadOnlyList<int>);
        });
        ColumnsProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            var list = e.NewValue as IReadOnlyList<ITableColumnHeader>;
            while (list != null && view.columnVisibility.Count < list.Count)
                view.columnVisibility.Add(true);
            view.InvalidateVisual();
            view.InvalidateMeasure();
            view.InvalidateArrange();
        });
        SelectedRowIndexProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.EnsureRowVisible((VerticalCursor)e.NewValue!);
        });
        SelectedCellIndexProperty.Changed.AddClassHandler<VeryFastTableView>((view, e) =>
        {
            view.EnsureCellVisible((int)e.NewValue!);
        });
        AffectsRender<VeryFastTableView>(SelectedRowIndexProperty);
        AffectsRender<VeryFastTableView>(SelectedCellIndexProperty);
        AffectsRender<VeryFastTableView>(RowFilterProperty);
        AffectsRender<VeryFastTableView>(RowFilterParameterProperty);
        AffectsRender(IsGroupingEnabledProperty);
        AffectsMeasure<VeryFastTableView>(RowFilterProperty);
        AffectsMeasure<VeryFastTableView>(RowFilterParameterProperty);
        AffectsMeasure<VeryFastTableView>(IsGroupingEnabledProperty);
        AffectsArrange<VeryFastTableView>(RowFilterProperty);
        AffectsArrange<VeryFastTableView>(RowFilterParameterProperty);
        AffectsArrange<VeryFastTableView>(IsGroupingEnabledProperty);
        FocusableProperty.OverrideDefaultValue<VeryFastTableView>(true);
        BackgroundProperty.OverrideDefaultValue<VeryFastTableView>(Brushes.Transparent);

        RowFilterParameterProperty.Changed.AddClassHandler<VeryFastTableView>((table, e) => table.RemoveInvisibleFromSelection());
    }
    
    public VeryFastTableView()
    {
        var openAndErase = new DelegateCommand(() =>
        {
            if (IsSelectedCellValid)
                OpenEditor("");
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            Command = new DelegateCommand(() =>
            {
                if (IsSelectedCellValid)
                    OpenEditor();
            })
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Back),
            Command = openAndErase
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.Delete),
            Command = openAndErase
        });
        headerViews = new RecyclableViewList(this);
    }

    private void RemoveInvisibleFromSelection()
    {
        var items = Items;

        if (items == null)
            return;
        
        var selection = MultiSelection;
        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        var containsIterator = selection.ContainsIterator;
        
        if (rowFilter == null)
            return; // no filter means the selection is all good

        List<(int groupIndex, int rowIndex)> toRemove = new List<(int groupIndex, int rowIndex)>();
        
        for (var groupIndex = 0; groupIndex < items.Count; groupIndex++)
        {
            var group = items[groupIndex];
            for (var rowIndex = 0; rowIndex < group.Rows.Count; rowIndex++)
            {
                var row = group.Rows[rowIndex];
                var cursor = new VerticalCursor(groupIndex, rowIndex);
                var inSelection = containsIterator.Contains(cursor);
                
                if (inSelection && !IsFilteredRowVisible(group, row, rowFilter, rowFilterParameter))
                    toRemove.Add((groupIndex, rowIndex));
            }
        }

        foreach (var (groupIndex, rowIndex) in toRemove)
        {
            selection.Remove(new VerticalCursor(groupIndex, rowIndex));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateMeasure();
            InvalidateArrange();
        });
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        bool move = false;

        if (e.Key == Key.Down)
        {
            e.Handled = move = MoveCursorDown();
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = move = MoveCursorUp();
        }
        else if (e.Key == Key.Left)
        {
            e.Handled = move = MoveCursorLeft();
        }
        else if (e.Key == Key.Right)
        {
            e.Handled = move = MoveCursorRight();
        }
        else if (e.Key == Key.Tab)
        {
            e.Handled = move = (e.KeyModifiers & KeyModifiers.Shift) != 0 ? MoveCursorPrevious() : MoveCursorNext();
        }
        else if (e.Key == Key.F2)
        {
            OpenEditor();
            e.Handled = true;
        }
        if (move)
        {
            var oldSelectionHasNewFocus = MultiSelection.Contains(SelectedRowIndex);

            if (!oldSelectionHasNewFocus && !e.KeyModifiers.HasFlagFast(KeyModifiers.Shift))
            {
                MultiSelection.Clear();
                oldSelectionHasNewFocus = false;
            }
            
            if (!oldSelectionHasNewFocus)
                MultiSelection.Add(SelectedRowIndex);
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled)
            return;
        
        if (IsSelectedCellValid)
        {
            OpenEditor(e.Text);
            e.Handled = true;
        }
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        Cursor = Cursor.Default;
        lastMouseLocation = new Point(-1, -1);
        InvalidateVisual();
        base.OnPointerLeave(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var currentPoint = e.GetCurrentPoint(this);
        var point = currentPoint.Position;
        lastMouseLocation = point;
        lastMouseButtonPressed = currentPoint.Properties.IsLeftButtonPressed;
        Cursor = (currentlyResizedColumn.HasValue || IsPointHeader(point) && IsOverColumnSplitter(lastMouseLocation.X, out _)) ? new Cursor(StandardCursorType.SizeWestEast) : Cursor.Default;

        if (CustomCellDrawer != null)
        {
            if (CustomCellDrawer.UpdateCursor(currentPoint.Position, currentPoint.Properties.IsLeftButtonPressed))
                InvalidateVisual();
        }

        if (currentlyResizedColumn.HasValue && Columns != null)
        {
            Columns[currentlyResizedColumn.Value].Width = Math.Max(10, resizingColumnStartWidth + e.GetPosition(this).X - resizingColumnStartPointerX);
            //todo: this should observe width, but this might be good enough for now
            InvalidateVisual();
            InvalidateMeasure();
        }
        
        if (IsPointHeader(point))
            InvalidateVisual();
    }

    private Point lastMouseLocation = new Point(-1, -1);
    private bool lastMouseButtonPressed;
    private int? currentlyResizedColumn = null;
    private double resizingColumnStartWidth = 0;
    private double resizingColumnStartPointerX = 0;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        bool rangeSelectMode = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        bool multiSelectMode = (e.KeyModifiers & KeyModifiers.Control) != 0 || (e.KeyModifiers & KeyModifiers.Meta) != 0;
        bool leftMouseButton = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        bool rightMouseButton = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        lastMouseButtonPressed = leftMouseButton;

        if (!IsPointHeader(e.GetPosition(this)))
        {
            // row
            {
                var index = GetRowIndexByY(e.GetPosition(this).Y);
                if (!index.HasValue || !IsRowIndexValid(index.Value))
                {
                    SelectedRowIndex = VerticalCursor.None;
                    MultiSelection.Clear();
                }
                else
                {
                    if (multiSelectMode)
                    {
                        if (MultiSelection.Contains(index.Value))
                            MultiSelection.Remove(index.Value);
                        else
                            MultiSelection.Add(index.Value);
                    }
                    else if (rangeSelectMode)
                    {
                        if (!IsRowIndexValid(SelectedRowIndex))
                        {
                            MultiSelection.Clear();
                            MultiSelection.Add(index.Value);
                        }
                        else
                        {
                            var rowFilter = RowFilter;
                            var rowFilterParameter = RowFilterParameter;
                            var items = Items!;
                            
                            var min = SelectedRowIndex < index.Value ? SelectedRowIndex : index.Value;
                            var max = SelectedRowIndex > index.Value ? SelectedRowIndex : index.Value;
                            VerticalCursor cursor = min;
                            while (cursor != max)
                            {
                                if (IsRowIndexValid(cursor) &&
                                    IsFilteredRowVisible(items[cursor.GroupIndex], items[cursor.GroupIndex].Rows[cursor.RowIndex], rowFilter, rowFilterParameter))
                                    MultiSelection.Add(cursor);
                                cursor = cursor.AddRowIndex(1);
 
                                if (!IsRowIndexValid(cursor))
                                    cursor = cursor.AddGroupIndex(1);
                            }
                            MultiSelection.Add(cursor); // add max
                        }
                    }
                    else
                    {
                        if (leftMouseButton || !MultiSelection.Contains(index.Value))
                        {
                            MultiSelection.Clear();
                            MultiSelection.Add(index.Value);
                        }
                    }
                    SelectedRowIndex = index.Value;
                }
            }
            // cell
            {
                var index = GetColumnIndexByX(e.GetPosition(this).X);
                if (Items?.Count == 0 || !index.HasValue)
                    SelectedCellIndex = -1;
                else
                    SelectedCellIndex = Math.Clamp(index.Value, 0, ColumnsCount - 1);
            }
        }
        else if (IsOverColumnSplitter(e.GetPosition(this).X, out var columnIndex))
        {
            currentlyResizedColumn = columnIndex;
            resizingColumnStartWidth = GetColumnRect(currentlyResizedColumn.Value).width;
            resizingColumnStartPointerX = e.GetPosition(this).X;
            e.Handled = true;
        }
        else if (IsOverColumn(e.GetPosition(this).X, out var pressedColumn, out var pressedColumnIndex))
        {
            RaiseEvent(new ColumnPressedEventArgs(){RoutedEvent = ColumnPressedEvent, Column = pressedColumn, ColumnIndex = pressedColumnIndex, KeyModifiers = e.KeyModifiers});
            e.Handled = true;
        }

        if (!e.Handled && IsSelectedCellValid && Items != null)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            var cell = Items[SelectedRowIndex.GroupIndex].Rows[SelectedRowIndex.RowIndex].CellsList[SelectedCellIndex];
            if (CustomCellInteractor != null && CustomCellInteractor.PointerDown(cell,
                    SelectedCellRect,
                    e.GetPosition(this),
                    properties.IsLeftButtonPressed, 
                    properties.IsRightButtonPressed, 
                    e.ClickCount))
            {
                e.Handled = true;
            }
            
            if (!e.Handled)
            {
                if (e.ClickCount == 2 && properties.IsLeftButtonPressed)
                {
                    OpenEditor();
                    e.Handled = true;
                }
            }
        }
        
        if (CustomCellDrawer != null)
        {
            var point = e.GetCurrentPoint(this);
            CustomCellDrawer.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed);
        }
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (currentlyResizedColumn.HasValue)
        {
            currentlyResizedColumn = null;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (IsSelectedCellValid && Items != null)
        {
            var cell = Items[SelectedRowIndex.GroupIndex].Rows[SelectedRowIndex.RowIndex].CellsList[SelectedCellIndex];
            if (CustomCellInteractor != null && CustomCellInteractor.PointerUp(cell,
                    SelectedCellRect,
                    e.GetPosition(this),
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased,
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased))
            {
                e.Handled = true;
            }
        }
        
        if (CustomCellDrawer != null)
        {
            var point = e.GetCurrentPoint(this);
            CustomCellDrawer.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed);
        }

        if (e.InitialPressMouseButton == MouseButton.Left)
            lastMouseButtonPressed = false;
        
        InvalidateVisual();
    }

    private void OpenEditor(string? customText = null)
    {
        if (Items == null)
            return;
        if (CustomCellInteractor == null ||
            !CustomCellInteractor.SpawnEditorFor(customText, this, SelectedCellRect, Items[SelectedRowIndex.GroupIndex].Rows[SelectedRowIndex.RowIndex].CellsList[SelectedCellIndex]))
        {
            editor.Spawn(this, SelectedCellRect, customText ?? Items[SelectedRowIndex.GroupIndex].Rows[SelectedRowIndex.RowIndex].CellsList[SelectedCellIndex].StringValue ?? "", customText == null, (text, action) =>
            {
                ValueUpdateRequest?.Invoke(text);
                if (action != PhantomTextBox.ActionAfterSave.None)
                {
                    switch (action)
                    {
                        case PhantomTextBox.ActionAfterSave.MoveUp:
                            Move(this, NavigationDirection.Up);
                            break;
                        case PhantomTextBox.ActionAfterSave.MoveDown:
                            Move(this, NavigationDirection.Down);
                            break;
                        case PhantomTextBox.ActionAfterSave.MoveNext:
                            Move(this, NavigationDirection.Next);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(action), action, null);
                    }
                }
            });   
        }
    }
    
    public bool IsSelectedCellValid => SelectedCellIndex >= 0 && SelectedCellIndex < ColumnsCount && IsRowIndexValid(SelectedRowIndex);
    public Rect SelectedCellRect => CellRect(SelectedCellIndex, SelectedRowIndex);

    public Rect CellRect(int column, VerticalCursor row)
    {
        var widthRect = GetColumnRect(column);
        var width = Math.Max(0, widthRect.width);
        return new Rect(widthRect.x, GetRowY(row), width, RowHeight);
    }

    public double GetRowY(VerticalCursor cursor)
    {
        if (Items == null)
            return DrawingStartOffsetY;
        var y = DrawingStartOffsetY;
        var headerHeight = IsGroupingEnabled ? HeaderRowHeight : 0;
        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;
        
        for (int i = 0; i <= cursor.GroupIndex; ++i)
        {
            var rowsInGroup = Items[i].Rows.Count;
            if (i == cursor.GroupIndex)
                rowsInGroup = cursor.RowIndex;

            y += headerHeight;
            
            if (Items[i].IsExpanded)
            {
                if (rowFilter == null)
                    y += rowsInGroup * RowHeight;
                else
                {
                    for (var index = 0; index < rowsInGroup; index++)
                    {
                        var row = Items[i].Rows[index];
                        if (IsFilteredRowVisible(Items[i], row, rowFilter, rowFilterParameter))
                            y += RowHeight;
                    }
                }
            }
        }
        return y;
    }

    private ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();

    private bool IsRowVisible(VerticalCursor row, ScrollViewer? scroll = null)
    {
        if (Items == null)
            return false;
        if (scroll == null)
            scroll = ScrollViewer;
        if (scroll == null)
            return false;
        
        var top = GetRowY(row);
        var bottom = top + RowHeight;
        return top < scroll.Offset.Y + scroll.Viewport.Height && bottom > scroll.Offset.Y;
    }
}