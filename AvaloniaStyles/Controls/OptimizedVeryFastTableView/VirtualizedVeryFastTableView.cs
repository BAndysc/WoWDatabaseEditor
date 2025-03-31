using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView : RenderedPanel, ICustomKeyboardNavigation
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
        if (Application.Current!.Styles.TryGetResource(key, SystemTheme.EffectiveThemeVariant, out var res) && res is T t)
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
            // the problem justChangedDataContext solves is:
            // scrolling to the selected row is very useful
            // BUT! in the Sql Editor, there are tabs. When tabs are change, datacontext is changed
            // that caused the SelectedRow to change and the view was scrolled.
            // but the behaviour we want is to remember the scroll position.
            // Despite Offset being bound, the offset was updated before the SelectedRow
            // thus this workaround to skip the scroll when datacontext has just been changed
            if (view.justChangedDataContext)
                return;
            view.EnsureRowVisible((int)e.NewValue!);
        });
        SelectedCellIndexProperty.Changed.AddClassHandler<VirtualizedVeryFastTableView>((view, e) =>
        {
            if (view.justChangedDataContext)
                return;
            view.EnsureCellVisible((int)e.NewValue!);
        });
        AffectsRender<VirtualizedVeryFastTableView>(IsFocusedProperty);
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
    
    private bool justChangedDataContext = false;
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        justChangedDataContext = true;
        DispatcherTimer.RunOnce(() => justChangedDataContext = false, TimeSpan.FromMilliseconds(1));
    }

    public VirtualizedVeryFastTableView()
    {
        UpdateKeyBindings();
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        });
        if (ScrollViewer is { } sc)
        {
            sc.GetObservable(ScrollViewer.OffsetProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
                InvalidateVisual();
            });
            sc.GetObservable(BoundsProperty).SubscribeAction(_ =>
            {
                InvalidateArrange();
                InvalidateVisual();
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
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        SetCurrentValue(CursorProperty, Cursor.Default);
        lastMouseLocation = new Point(-1, -1);
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var currentPoint = e.GetCurrentPoint(this);
        var point = currentPoint.Position;
        lastMouseLocation = point;
        lastMouseButtonPressed = currentPoint.Properties.IsLeftButtonPressed;
        SetCurrentValue(CursorProperty, (currentlyResizedColumn.HasValue || IsPointHeader(point) && IsOverColumnSplitter(lastMouseLocation.X, out _)) ? new Cursor(StandardCursorType.SizeWestEast) : Cursor.Default);

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
                    SetCurrentValue(SelectedRowIndexProperty, -1);
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
                    SetCurrentValue(SelectedRowIndexProperty, index);
                }
            }
            // cell
            {
                var index = GetColumnIndexByX(e.GetPosition(this).X);
                if (ItemsCount == 0 || !index.HasValue)
                    SetCurrentValue(SelectedCellIndexProperty, -1);
                else
                    SetCurrentValue(SelectedCellIndexProperty, Math.Clamp(index.Value, 0, ColumnsCount - 1));
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
                        SetCurrentValue(SelectedCellIndexProperty, -1);
                    else
                        SetCurrentValue(SelectedCellIndexProperty, Math.Clamp(index.Value, 0, ColumnsCount - 1));
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
            var originalText = Controller.GetCellText(SelectedRowIndex, SelectedCellIndex);
            var initialText = customText ?? originalText ?? "";
            editor.Spawn(this, SelectedCellRect, initialText, customText == null, (text, action) =>
            {
                if (originalText != text)
                    ValueUpdateRequest?.Invoke(text);
                if (action != PhantomTextBox.ActionAfterSave.None)
                {
                    switch (action)
                    {
                        case PhantomTextBox.ActionAfterSave.MoveUp:
                            GetNext(this, NavigationDirection.Up);
                            break;
                        case PhantomTextBox.ActionAfterSave.MoveDown:
                            GetNext(this, NavigationDirection.Down);
                            break;
                        case PhantomTextBox.ActionAfterSave.MoveNext:
                            GetNext(this, NavigationDirection.Next);
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
    public class BetterKeyBinding : KeyBinding, ICommand
    {
        public static readonly StyledProperty<ICommand> CustomCommandProperty = AvaloniaProperty.Register<KeyBinding, ICommand>(nameof (CustomCommand));

        public ICommand CustomCommand
        {
            get => GetValue(CustomCommandProperty);
            set => SetValue(CustomCommandProperty, value);
        }

        public BetterKeyBinding()
        {
            SetCurrentValue(CommandProperty, this);
        }

        public static TopLevel? GetTopLevel()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
            {
                var visualRoot = viewApp.MainView?.GetVisualRoot();
                return visualRoot as TopLevel;
            }
            return null;
        }

        public bool CanExecute(object? parameter)
        {
            var focusManager = GetTopLevel()?.FocusManager;
            var currentAsTextBox = focusManager?.GetFocusedElement() as TextBox;
            var currentAsTextEditor = focusManager?.GetFocusedElement() as TextEditor;
            var currentAsTextArea = focusManager?.GetFocusedElement() as TextArea;
            if (currentAsTextBox != null || currentAsTextEditor != null || currentAsTextArea != null)
                 return true;
            return CustomCommand.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            var focusManager = GetTopLevel()?.FocusManager;
            var currentAsTextBox = focusManager?.GetFocusedElement() as TextBox;
            var currentAsTextEditor = focusManager?.GetFocusedElement() as TextEditor;
            var currentAsTextArea = focusManager?.GetFocusedElement() as TextArea;
            if (currentAsTextBox != null || currentAsTextEditor != null || currentAsTextArea != null)
            {
                var ev = new KeyEventArgs()
                {
                    Key = Gesture.Key,
                    KeyModifiers = Gesture.KeyModifiers,
                    RoutedEvent = InputElement.KeyDownEvent
                };
                ((Control?)currentAsTextBox ?? (Control?)currentAsTextEditor ?? currentAsTextArea)!.RaiseEvent(ev);
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