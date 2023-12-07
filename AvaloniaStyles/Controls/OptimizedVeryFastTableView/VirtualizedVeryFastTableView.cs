using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView : Panel, IKeyboardNavigationHandler
{
    protected static double ColumnSpacing = 10;
    protected static double RowHeight = 28;
    protected static double HeaderRowHeight = 36;
    protected static double DrawingStartOffsetY = RowHeight;
    protected static ISolidColorBrush OddRowBackground = new SolidColorBrush(Colors.White);
    protected static ISolidColorBrush SelectedRowBackground = new SolidColorBrush(Color.FromRgb(87, 124, 219));
    protected static ISolidColorBrush EvenRowBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    protected static ISolidColorBrush HeaderBackground = new SolidColorBrush(Colors.White);
    protected static ISolidColorBrush HeaderPressedBackground = new SolidColorBrush(Colors.Gray);
    protected static ISolidColorBrush HeaderHoverBackground = new SolidColorBrush(Colors.LightGray);
    protected static IPen HeaderBorderBackground = new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 1);
    protected static IPen BorderPen = new Pen(new SolidColorBrush(Colors.Black), 1);
    protected static IPen FocusPen = new Pen(new SolidColorBrush(Colors.Black), 2);
    protected static IPen FocusOuterPen = new Pen(new SolidColorBrush(Colors.White), 3);
    protected static ISolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
    protected static ISolidColorBrush FocusTextBrush = OddRowBackground;

    public int ColumnsCount => Columns?.Count ?? 0;

    private PhantomTextBox editor = new();

    private static bool GetResource<T>(string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current!.Styles.TryGetResource(key, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        return false;
    }
    
    static VirtualizedVeryFastTableView()
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
        
        MultiSelectionProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            view.Rebind(e.OldValue as IMultiIndexContainer, e.NewValue as IMultiIndexContainer);
        });
        HiddenColumnsProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            view.RebindHiddenColumns(e.OldValue as IReadOnlyList<int>, e.NewValue as IReadOnlyList<int>);
        });
        ColumnsProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            var list = e.NewValue as IReadOnlyList<ITableColumnHeader>;
            while (list != null && view.columnVisibility.Count < list.Count)
                view.columnVisibility.Add(true);
            view.InvalidateVisual();
            view.InvalidateMeasure();
            view.InvalidateArrange();
        });
        SelectedRowIndexProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            view.EnsureRowVisible((int)e.NewValue!);
        });
        SelectedCellIndexProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            view.EnsureCellVisible((int)e.NewValue!);
        });
        AffectsRender<VirtualizedVeryFastTableView>(SelectedRowIndexProperty);
        AffectsRender<VirtualizedVeryFastTableView>(SelectedCellIndexProperty);
        
        AffectsMeasure<VirtualizedVeryFastTableView>(ItemsCountProperty);
        AffectsRender<VirtualizedVeryFastTableView>(ItemsCountProperty);
        
        AffectsMeasure<VirtualizedVeryFastTableView>(RequestRenderProperty);
        AffectsRender<VirtualizedVeryFastTableView>(RequestRenderProperty);
        
        FocusableProperty.OverrideDefaultValue<VirtualizedVeryFastTableView>(true);
        BackgroundProperty.OverrideDefaultValue<VirtualizedVeryFastTableView>(Brushes.Transparent);

        ItemsCountProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((v, e) => v.lastException = null);
        
        IsReadOnlyProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((table, e) =>
        {
            table.UpdateKeyBindings();
        });
    }
    
    public VirtualizedVeryFastTableView()
    {
        UpdateKeyBindings();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateMeasure();
            InvalidateArrange();
        });
        if (ScrollViewer is { } sc)
        {
            sc.GetObservable(ScrollViewer.OffsetProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
            });
            sc.GetObservable(BoundsProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
            });
        }
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
        
        if (IsSelectedCellValid && !IsReadOnly)
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

        if (Controller.UpdateCursor(currentPoint.Position, currentPoint.Properties.IsLeftButtonPressed))
            InvalidateVisual();

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
                if (!IsRowIndexValid(index))
                {
                    SelectedRowIndex = -1;
                    MultiSelection.Clear();
                }
                else
                {
                    if (multiSelectMode)
                    {
                        if (MultiSelection.Contains(index))
                            MultiSelection.Remove(index);
                        else
                            MultiSelection.Add(index);
                    }
                    else if (rangeSelectMode)
                    {
                        if (!IsRowIndexValid(SelectedRowIndex))
                        {
                            MultiSelection.Clear();
                            MultiSelection.Add(index);
                        }
                        else
                        {
                            var min = SelectedRowIndex < index ? SelectedRowIndex : index;
                            var max = SelectedRowIndex > index ? SelectedRowIndex : index;
                            int cursor = min;
                            while (cursor != max)
                            {
                                if (IsRowIndexValid(cursor))
                                    MultiSelection.Add(cursor);
                                cursor++;
                            }
                            MultiSelection.Add(cursor); // add max
                        }
                    }
                    else
                    {
                        if (leftMouseButton || !MultiSelection.Contains(index))
                        {
                            MultiSelection.Clear();
                            MultiSelection.Add(index);
                        }
                    }
                    SelectedRowIndex = index;
                }
            }
            // cell
            {
                var index = GetColumnIndexByX(e.GetPosition(this).X);
                if (ItemsCount == 0 || !index.HasValue)
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
            if (leftMouseButton)
                RaiseEvent(new ColumnPressedEventArgs(){RoutedEvent = ColumnPressedEvent, Column = pressedColumn, ColumnIndex = pressedColumnIndex, KeyModifiers = e.KeyModifiers});
            else
            {
                // update SelectedCellIndex for context menu
                {
                    var index = GetColumnIndexByX(e.GetPosition(this).X);
                    if (ItemsCount == 0 || !index.HasValue)
                        SelectedCellIndex = -1;
                    else
                        SelectedCellIndex = Math.Clamp(index.Value, 0, ColumnsCount - 1);
                }
            }
            e.Handled = true;
        }

        if (!e.Handled && IsSelectedCellValid)
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (SelectedCellIndex >= ColumnsCount)
                throw new Exception();
            if (Controller.PointerDown(SelectedRowIndex,
                    SelectedCellIndex,
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
        
        var point = e.GetCurrentPoint(this);
        Controller.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed);
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

        if (IsPointHeader(e.GetPosition(this)))
        {
            e.Handled = true;
        }
        else if (IsSelectedCellValid)
        {
            if (Controller.PointerUp(SelectedRowIndex,
                    SelectedCellIndex,
                    SelectedCellRect,
                    e.GetPosition(this),
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased,
                    e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased))
            {
                e.Handled = true;
            }
        }
        
        var point = e.GetCurrentPoint(this);
        Controller.UpdateCursor(point.Position, point.Properties.IsLeftButtonPressed);

        if (e.InitialPressMouseButton == MouseButton.Left)
            lastMouseButtonPressed = false;
        
        InvalidateVisual();
    }

    private void OpenEditor(string? customText = null)
    {
        if (IsReadOnly)
            return;
        if (!Controller.SpawnEditorFor(SelectedRowIndex, SelectedCellIndex, SelectedCellRect, customText, this))
        {
            var initialText = customText ?? Controller.GetCellText(SelectedRowIndex, SelectedCellIndex) ?? "";
            editor.Spawn(this, SelectedCellRect, initialText, customText == null, (text, action) =>
            {
                if (initialText != text)
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
    
    public bool IsSelectedRowValid => IsRowIndexValid(SelectedRowIndex);
    public bool IsSelectedCellValid => SelectedCellIndex >= 0 && SelectedCellIndex < ColumnsCount && IsRowIndexValid(SelectedRowIndex);
    public Rect SelectedCellRect => CellRect(SelectedCellIndex, SelectedRowIndex);

    public Rect CellRect(int column, int row)
    {
        var widthRect = GetColumnRect(column);
        var width = Math.Max(0, widthRect.width);
        return new Rect(widthRect.x, GetRowY(row), width, RowHeight);
    }

    public double GetRowY(int rowIndex)
    {
        return DrawingStartOffsetY + rowIndex * RowHeight;
    }

    protected ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();
    
    private void UpdateKeyBindings()
    {
        KeyBindings.Clear();
        if (IsReadOnly)
            return;
        
        var openAndErase = new DelegateCommand(() =>
        {
            if (IsSelectedCellValid)
                OpenEditor("");
        });
        KeyBindings.Add(new BetterKeyBinding()
        {
            Gesture = new KeyGesture(Key.Enter),
            CustomCommand = new DelegateCommand(() =>
            {
                if (IsSelectedCellValid)
                    OpenEditor();
            })
        });
        KeyBindings.Add(new BetterKeyBinding()
        {
            Gesture = new KeyGesture(Key.Back),
            CustomCommand = openAndErase
        });
        KeyBindings.Add(new BetterKeyBinding()
        {
            Gesture = new KeyGesture(Key.Delete),
            CustomCommand = openAndErase
        });
    }
    
    
    /***
     * This is KeyBinding that forwards the gesture to the focused TextBox first
     */
    private class BetterKeyBinding : KeyBinding, ICommand
    {
        public static readonly StyledProperty<ICommand> CustomCommandProperty = AvaloniaProperty.Register<KeyBinding, ICommand>(nameof (CustomCommand));

        public ICommand CustomCommand
        {
            get => GetValue(CustomCommandProperty);
            set => SetValue(CustomCommandProperty, value);
        }
        
        public BetterKeyBinding()
        {
            Command = this;
        }

        public bool CanExecute(object? parameter)
        {
            if (FocusManager.Instance!.Current is TextBox tb)
                return true;
            return CustomCommand.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            if (FocusManager.Instance!.Current is TextBox tb)
            {
                var ev = Activator.CreateInstance<KeyEventArgs>();
                ev.Key = Gesture.Key;
                ev.KeyModifiers = Gesture.KeyModifiers;
                ev.RoutedEvent = InputElement.KeyDownEvent;
                tb.RaiseEvent(ev);
                if (!ev.Handled && CanExecute(parameter))
                    CustomCommand.Execute(parameter);
            }
            else
                CustomCommand.Execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CustomCommand.CanExecuteChanged += value;
            remove => CustomCommand.CanExecuteChanged -= value;
        }
    }
}