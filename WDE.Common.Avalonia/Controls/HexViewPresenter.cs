using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Prism.Commands;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Controls;

public class CopyOnWriteMemory<T>
{
    private T[]? array;
    private Memory<T>? writeableMemory;
    private ReadOnlyMemory<T> readOnlyMemory;
    private bool isModified;
    
    public T this[int index]
    {
        get => readOnlyMemory.Span[index];
        set
        {
            isModified = true;
            if (writeableMemory != null)
            {
                writeableMemory.Value.Span[index] = value;
                return;
            }

            array = readOnlyMemory.ToArray();
            writeableMemory = array.AsMemory();
            readOnlyMemory = writeableMemory.Value;
            writeableMemory.Value.Span[index] = value;
        }
    }
    
    public bool IsCopy => array != null;
    
    public bool IsModified => isModified;
    
    public T[]? ModifiedArray => array;

    public int Length => array?.Length ?? readOnlyMemory.Length;

    public CopyOnWriteMemory(T[] arr)
    {
        array = arr;
        this.writeableMemory = arr;
        this.readOnlyMemory = arr;
    }
    
    public CopyOnWriteMemory(Memory<T> writeableMemory)
    {
        array = null;
        this.writeableMemory = writeableMemory;
        this.readOnlyMemory = writeableMemory;
    }
    
    public CopyOnWriteMemory(ReadOnlyMemory<T> readOnlyMemory)
    {
        array = null;
        this.writeableMemory = null;
        this.readOnlyMemory = readOnlyMemory;
    }

    public ReadOnlySpan<T> Slice(int start, int length)
    {
        return readOnlyMemory.Span.Slice(start, length);
    }
}

public class HexViewControl : TemplatedControl
{
    public static readonly StyledProperty<CopyOnWriteMemory<byte>> BytesProperty = 
        AvaloniaProperty.Register<HexViewControl, CopyOnWriteMemory<byte>>(nameof(Bytes), new CopyOnWriteMemory<byte>(Array.Empty<byte>().AsMemory()));

    public static readonly StyledProperty<int> SelectionStartProperty = 
        AvaloniaProperty.Register<HexViewControl, int>(nameof(SelectionStart), 0, coerce: CoerceSelectionStartIndex);

    public static readonly StyledProperty<int> SelectionEndProperty = 
        AvaloniaProperty.Register<HexViewControl, int>(nameof(SelectionEnd), 0, coerce: CoerceSelectionEndIndex);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty = 
        AvaloniaProperty.Register<HexViewControl, IBrush>(nameof(SelectionBrush), Brushes.Gainsboro);

    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        AvaloniaProperty.Register<HexViewControl, TimeSpan>(nameof(CaretBlinkInterval), TimeSpan.FromMilliseconds(600));
    
    public static readonly StyledProperty<int?> CaretIndexProperty =
        AvaloniaProperty.Register<HexViewControl, int?>(nameof(CaretIndex), 0, coerce: CoerceCaretIndex, defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<bool> CaretInAsciiProperty =
        AvaloniaProperty.Register<HexViewControl, bool>(nameof(CaretInAscii), defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HexViewControl, bool>(nameof(IsReadOnly));

    public CopyOnWriteMemory<byte> Bytes
    {
        get => GetValue(BytesProperty);
        set => SetValue(BytesProperty, value);
    }
    
    public int SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }
    
    public int SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }
    
    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }
    
    public TimeSpan CaretBlinkInterval
    {
        get => GetValue(CaretBlinkIntervalProperty);
        set => SetValue(CaretBlinkIntervalProperty, value);
    }

    public int? CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, value);
    }
    
    public bool CaretInAscii
    {
        get => GetValue(CaretInAsciiProperty);
        set => SetValue(CaretInAsciiProperty, value);
    }
    
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    static HexViewControl()
    {
        FocusableProperty.OverrideDefaultValue<HexViewControl>(true);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectionStartProperty
            || change.Property == SelectionEndProperty 
            || change.Property == CaretIndexProperty)
        {
            presenter?.InvalidateVisual();
        }
    }

    private HexViewPresenter? presenter;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        presenter = e.NameScope.Find<HexViewPresenter>("PART_ContentHost");
    }

    private static int? CoerceCaretIndex(AvaloniaObject obj, int? value)
    {
        if (!value.HasValue)
            return null;
        
        var control = obj as HexViewControl;
        var presenter = obj as HexViewPresenter;
        var length = control?.Bytes.Length ?? presenter?.Bytes.Length ?? 0;
        return Math.Max(0, Math.Min(value.Value, length - 1));
    }
    
    private static int CoerceSelectionStartIndex(AvaloniaObject obj, int value)
    {
        var control = obj as HexViewControl;
        var presenter = obj as HexViewPresenter;
        var length = control?.Bytes.Length ?? presenter?.Bytes.Length ?? 0;
        return Math.Max(0, Math.Min(value, length - 1));
    }
    
    private static int CoerceSelectionEndIndex(AvaloniaObject obj, int value)
    {
        var control = obj as HexViewControl;
        var presenter = obj as HexViewPresenter;
        var length = control?.Bytes.Length ?? presenter?.Bytes.Length ?? 0;
        return Math.Max(0, Math.Min(value, length));
    }
}

public class HexViewPresenter : Control, ILogicalScrollable
{
    public static readonly StyledProperty<CopyOnWriteMemory<byte>> BytesProperty = 
        HexViewControl.BytesProperty.AddOwner<HexViewPresenter>();

    public static readonly StyledProperty<int> SelectionStartProperty = 
        HexViewControl.SelectionStartProperty.AddOwner<HexViewPresenter>();

    public static readonly StyledProperty<int> SelectionEndProperty = 
        HexViewControl.SelectionEndProperty.AddOwner<HexViewPresenter>();

    public static readonly StyledProperty<IBrush> SelectionBrushProperty = 
        HexViewControl.SelectionBrushProperty.AddOwner<HexViewPresenter>();

    public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
        HexViewControl.CaretBlinkIntervalProperty.AddOwner<HexViewPresenter>();
    
    public static readonly StyledProperty<int?> CaretIndexProperty =
        HexViewControl.CaretIndexProperty.AddOwner<HexViewPresenter>();
    
    public static readonly StyledProperty<bool> CaretInAsciiProperty =
        HexViewControl.CaretInAsciiProperty.AddOwner<HexViewPresenter>();
    
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        HexViewControl.IsReadOnlyProperty.AddOwner<HexViewPresenter>();

    private volatile bool updating;
    private DispatcherTimer? caretTimer;
    private bool caretBlink;
    private Size extent;
    private Size viewport;
    private Vector offset;
    private bool canHorizontallyScroll;
    private bool canVerticallyScroll;
    private EventHandler? scrollInvalidated;
    private Typeface typeface;
    private double lineHeight;
    private double charWidth;
    private FontFamily? fontFamily;
    private double fontSize;
    private IBrush? foreground;
    private Size scrollSize = new(1, 1);
    private Size pageScrollSize = new(10, 10);
    private bool updatedLowNibble;

    private const float asciiSpacing = 20; // spacing between hex and ascii
    private const float groupSpacing = 10; // spacing between bytesPerGroup bytes groups
    private const int bytesPerGroup = 4;
    
    public CopyOnWriteMemory<byte> Bytes
    {
        get => GetValue(BytesProperty);
        set => SetValue(BytesProperty, value);
    }
    
    public int SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }
    
    public int SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }
    
    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }
    
    public TimeSpan CaretBlinkInterval
    {
        get => GetValue(CaretBlinkIntervalProperty);
        set => SetValue(CaretBlinkIntervalProperty, value);
    }

    public int? CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, value);
    }
    
    public bool CaretInAscii
    {
        get => GetValue(CaretInAsciiProperty);
        set => SetValue(CaretInAsciiProperty, value);
    }
    
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    static HexViewPresenter()
    {
        FocusableProperty.OverrideDefaultValue<HexViewPresenter>(true);
    }

    // high and low are ASCII characters for given hex (0 - 255) value
    internal static (byte high, byte low)[] bytesLookupTable = BuildBytesLookupTable();
    
    static (byte high, byte low)[] BuildBytesLookupTable()
    {
        // https://stackoverflow.com/a/14333437/1616645
        (byte high, byte low)[] c = new (byte, byte)[256];
        int b;
        for (int i = 0; i < 256; i++) {
            b = i >> 4;
            var highNibble = (byte)(55 + b + (((b-10)>>31)&-7));
            b = i & 0xF;
            var lowNibble = (byte)(55 + b + (((b-10)>>31)&-7));
            c[i] = (highNibble, lowNibble);
        }
        return c;
    }

    Size IScrollable.Extent => extent;

    Vector IScrollable.Offset
    {
        get => offset;
        set
        {
            if (updating)
            {
                return;
            }
            updating = true;
            offset = CoerceOffset(value);
            InvalidateScrollable();
            updating = false;
        }
    }

    Size IScrollable.Viewport => viewport;

    bool ILogicalScrollable.CanHorizontallyScroll
    {
        get => canHorizontallyScroll;
        set
        {
            canHorizontallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.CanVerticallyScroll
    {
        get => canVerticallyScroll;
        set
        {
            canVerticallyScroll = value;
            InvalidateMeasure();
        }
    }

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => scrollInvalidated += value;
        remove => scrollInvalidated -= value;
    }

    Size ILogicalScrollable.ScrollSize => scrollSize;

    Size ILogicalScrollable.PageScrollSize => pageScrollSize;

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e)
    {
        scrollInvalidated?.Invoke(this, e);
    }
   
    private Vector CoerceOffset(Vector value)
    {
        var scrollable = (ILogicalScrollable)this;
        var maxX = Math.Max(scrollable.Extent.Width - scrollable.Viewport.Width, 0);
        var maxY = Math.Max(scrollable.Extent.Height - scrollable.Viewport.Height, 0);
        return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
        static double Clamp(double val, double min, double max) => val < min ? min : val > max ? max : val;
    }

    private FormattedText[] formattedTextCache = Array.Empty<FormattedText>();
        
    private FormattedText GetFormattedText(byte chr)
    {
        if (formattedTextCache.Length == 0)
        {
            formattedTextCache = new FormattedText[256];
            for (int i = 0; i < 256; ++i)
            {
                formattedTextCache[i] = new FormattedText(((char)i).ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    foreground);
            }
        }
        return formattedTextCache[chr];
    }

    public HexViewPresenter()
    {
        var copyCommand = new DelegateCommand(async () =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null)
                return;
            
            if (SelectionStart >= SelectionEnd)
                return;
            
            var length = SelectionEnd - SelectionStart;
            if (CaretInAscii)
            {
                if (Bytes.Slice(SelectionStart, length).Contains((byte)0))
                {
                    await ViewBind.ResolveViewModel<IMessageBoxService>()
                        .SimpleDialog("Warning", "Can't copy null byte as string", "You are trying to copy bytes as string but they contain null byte. Copied string will be truncated to the first null byte");
                }
                var str = Encoding.UTF8.GetString(Bytes.Slice(SelectionStart, length));
                await clipboard.SetTextAsync(str);
            }
            else
            {
                var chars = new char[length * 2];
                for (int i = SelectionStart; i < SelectionEnd; ++i)
                {
                    var b = Bytes[i];
                    var (highNibble, lowNibble) = bytesLookupTable[b];
                    chars[(i - SelectionStart) * 2] = (char)highNibble;
                    chars[(i - SelectionStart) * 2 + 1] = (char)lowNibble;
                }
                await clipboard.SetTextAsync(new string(chars));
            }
        });

        var pasteCommand = new AsyncAutoCommand(async () =>
        {
            if (IsReadOnly)
                return;
            
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard == null)
                return;
            
            var text = await clipboard.GetTextAsync();
            if (text == null)
                return;

            byte[] bytes;
            if (CaretInAscii)
                bytes = Encoding.UTF8.GetBytes(text);
            else
            {
                bytes = new byte[text.Length / 2];
                for (int i = 0; i < text.Length / 2; ++i)
                {
                    var highChar = text[i * 2];
                    var lowChar = text[i * 2 + 1];
                    var highNibble = highChar is >= '0' and <= '9' ? highChar - '0' : highChar - 'A' + 10;
                    var lowNibble = lowChar is >= '0' and <= '9' ? lowChar - '0' : lowChar - 'A' + 10;
                    bytes[i] = (byte)((highNibble << 4) | lowNibble);
                }
            }
            
            foreach (var b in bytes)
            {
                var caretIndex = CaretIndex ?? SelectionStart;
                SetCurrentValue(CaretIndexProperty, caretIndex);
                if (SelectionStart == SelectionEnd)
                {
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }

                Bytes[caretIndex] = b;
                CaretIndex++;
                if (SelectionStart == SelectionEnd || caretIndex >= SelectionEnd)
                {
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }
            }
        }, () => !IsReadOnly);
        
        var selectAllCommand = new DelegateCommand(() =>
        {
            SetCurrentValue(SelectionStartProperty, 0);
            SetCurrentValue(SelectionEndProperty, Bytes.Length);
            SetCurrentValue(CaretIndexProperty, null);
        });
        
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.C, KeyModifiers.Control),
            Command = copyCommand
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.C, KeyModifiers.Meta),
            Command = copyCommand
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.V, KeyModifiers.Control),
            Command = pasteCommand
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.V, KeyModifiers.Meta),
            Command = pasteCommand
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.A, KeyModifiers.Control),
            Command = selectAllCommand
        });
        KeyBindings.Add(new KeyBinding()
        {
            Gesture = new KeyGesture(Key.A, KeyModifiers.Meta),
            Command = selectAllCommand
        });
    }
    
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        StartBlinkTimer();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        StopBlinkTimer();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Invalidate();
        InvalidateScrollable();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        StopBlinkTimer();
    }

    private void StartBlinkTimer()
    {
        StopBlinkTimer();
        caretTimer = new DispatcherTimer(){Interval = CaretBlinkInterval};
        caretTimer.Tick += OnCaretTimerTick;
        caretTimer.Start();
    }

    private void OnCaretTimerTick(object? sender, EventArgs e)
    {
        caretBlink = !caretBlink;
        InvalidateVisual();
    }

    private void StopBlinkTimer()
    {
        caretBlink = false;
        if (caretTimer != null)
        {
            caretTimer.Tick -= OnCaretTimerTick;
            caretTimer.Stop();
            caretTimer = null;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            InvalidateScrollable();
        }
        
        if (change.Property == TextBlock.FontFamilyProperty
            || change.Property == TextBlock.FontSizeProperty
            || change.Property == TextBlock.ForegroundProperty
            || change.Property == BytesProperty)
        {
            Invalidate();
            InvalidateScrollable();
            InvalidateVisual();
        }

        if (change.Property == BytesProperty)
        {
            SetCurrentValue(CaretIndexProperty, 0);
            SetCurrentValue(SelectionStartProperty, 0);
            SetCurrentValue(SelectionEndProperty, 0);
        }
        
        if (change.Property == CaretBlinkIntervalProperty && IsFocused)
            StartBlinkTimer();
        
        if (change.Property == SelectionStartProperty
            || change.Property == SelectionEndProperty 
            || change.Property == CaretIndexProperty)
        {
            InvalidateVisual();
            updatedLowNibble = false;
        }
    }

    private void Invalidate()
    {
        fontFamily = TextElement.GetFontFamily(this);
        fontSize = TextElement.GetFontSize(this);
        foreground = TextElement.GetForeground(this);
        typeface = new Typeface(fontFamily);
        formattedTextCache = Array.Empty<FormattedText>();
        var ft = GetFormattedText((byte)'0');
        lineHeight = ft.Height;
        charWidth = ft.Width;
    }

    // a group is bytesPerGroup bytes
    private int GroupsPerLine()
    {
        // each byte is 3 chars - an ascii preview and 2 hex digits
        // * 2, because each spacing occurs twice - between each group in hex and between each group in ascii
        int bytesPerLine = (int)(Bounds.Width / (3 * charWidth + groupSpacing / bytesPerGroup * 2));
        return Math.Max(1, (int)(bytesPerLine * 1.0 / bytesPerGroup)); // but at least 1
    }
    
    public void InvalidateScrollable()
    {
        if (this is not ILogicalScrollable scrollable)
        {
            return;
        }

        var width = Bounds.Width;
        var height = Bounds.Height;

        var groupsPerLine = GroupsPerLine();
        var bytesPerLine = groupsPerLine * bytesPerGroup;
        var lines = (int)Math.Ceiling(Bytes.Length / (double)bytesPerLine);
        
        scrollSize = new Size(1, lineHeight);
        pageScrollSize = new Size(viewport.Width, viewport.Height);
        extent = new Size(Math.Max(width, bytesPerGroup * 3 * charWidth), lines * lineHeight);
        viewport = new Size(width, height);

        scrollable.RaiseScrollInvalidated(EventArgs.Empty);
        
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var groupsPerLine = GroupsPerLine();
        var bytesPerLine = groupsPerLine * bytesPerGroup;
        var logicalLines = (int)Math.Ceiling(Bytes.Length / (double)bytesPerLine);
        
        var startLine = (int)Math.Floor(offset.Y / lineHeight);
        var lines = viewport.Height / lineHeight;
        var endLine = (int)Math.Min(Math.Ceiling(startLine + lines), logicalLines - 1);

        double y = startLine * lineHeight - offset.Y;
        for (var i = startLine; i <= endLine; i++)
        {
            double x = 0;
            for (int j = 0; j < bytesPerLine; ++j)
            {
                var index = i * bytesPerLine + j;
                if (index >= Bytes.Length)
                    break;
                var b = Bytes[index];
                var (highNibble, lowNibble) = bytesLookupTable[b];
                var ftHigh = GetFormattedText(highNibble);
                var ftLow = GetFormattedText(lowNibble);
                
                var isSelected = index >= SelectionStart && index < SelectionEnd;
                
                if (isSelected && IsEffectivelyEnabled)
                {
                    var rect = new Rect(x, y, charWidth * 2, lineHeight);
                    context.FillRectangle(SelectionBrush, rect);
                }

                if (CaretIndex == index && !CaretInAscii && caretBlink && IsEffectivelyEnabled)
                {
                    if (foreground != null)
                        context.FillRectangle(foreground, new Rect(x, y, 1, lineHeight));
                }

                context.DrawText(ftHigh, new Point(x, y));
                x += charWidth;
                context.DrawText(ftLow, new Point(x, y));
                x += charWidth;
                
                if (j % bytesPerGroup == bytesPerGroup - 1 && j != bytesPerLine - 1)
                {
                    var nextIsSelected = index + 1 >= SelectionStart && index + 1 < SelectionEnd;
                    if (isSelected && nextIsSelected && IsEffectivelyEnabled)
                    {
                        var rect = new Rect(x, y, groupSpacing, lineHeight);
                        context.FillRectangle(SelectionBrush, rect);
                    }
                    
                    x += groupSpacing;
                }
            }

            x = bytesPerLine * 2 * charWidth + groupSpacing * (bytesPerLine / bytesPerGroup) + asciiSpacing;
            
            for (int j = 0; j < bytesPerLine; ++j)
            {
                var index = i * bytesPerLine + j;
                if (index >= Bytes.Length)
                    break;
                var b = Bytes[index];
                var ft = GetFormattedText(char.IsPunctuation((char)b) || char.IsSymbol((char)b) || char.IsAsciiLetterOrDigit((char)b) || b == ' ' ? b : (byte)'.');
                
                var isSelected = index >= SelectionStart && index < SelectionEnd;
                
                if (isSelected && IsEffectivelyEnabled)
                {
                    var rect = new Rect(x, y, charWidth, lineHeight);
                    context.FillRectangle(SelectionBrush, rect);
                }

                if (CaretIndex == index && CaretInAscii && caretBlink && IsEffectivelyEnabled)
                {
                    if (foreground != null)
                        context.FillRectangle(foreground, new Rect(x, y, 1, lineHeight));
                }

                context.DrawText(ft, new Point(x, y));
                x += charWidth;
                
                if (j % bytesPerGroup == bytesPerGroup - 1 && j != bytesPerLine - 1)
                {
                    var nextIsSelected = index + 1 >= SelectionStart && index + 1 < SelectionEnd;
                    if (isSelected && nextIsSelected && IsEffectivelyEnabled)
                    {
                        var rect = new Rect(x, y, groupSpacing, lineHeight);
                        context.FillRectangle(SelectionBrush, rect);
                    }
                    
                    x += groupSpacing;
                }
            }
            
            y += lineHeight;
        }
    }

    private int? GetIndexFromCoords(Point point, out bool inAscii)
    {
        var groupsPerLine = GroupsPerLine();
        var bytesPerLine = groupsPerLine * bytesPerGroup;
        var logicalLines = (int)Math.Ceiling(Bytes.Length / (double)bytesPerLine);
        
        var hexWidth = bytesPerLine * 2 * charWidth + groupSpacing * (bytesPerLine / bytesPerGroup);
        var asciiStart = hexWidth + asciiSpacing;

        if (point.Y >= logicalLines * lineHeight)
        {
            inAscii = false;
            return Bytes.Length - 1;
        }
        
        var line = (int)Math.Floor(point.Y / lineHeight);
        double y = line * lineHeight;

        if (point.X < hexWidth)
        {
            double x = 0;
            for (int indexInLine = 0; indexInLine < bytesPerLine; ++indexInLine)
            {
                var index = line * bytesPerLine + indexInLine;
                if (index >= Bytes.Length)
                    break;
                
                var makeSpacing = indexInLine % bytesPerGroup == bytesPerGroup - 1 && indexInLine != bytesPerLine - 1;
                
                var rect = new Rect(x, y, charWidth * 2 + (makeSpacing ? groupSpacing : 0), lineHeight);
                if (rect.Contains(point))
                {
                    inAscii = false;
                    return index;
                }
                
                x += charWidth * 2;
                if (makeSpacing)
                    x += groupSpacing;
            }
        }
        else
        {
            double x = asciiStart;
            for (int indexInLine = 0; indexInLine < bytesPerLine; ++indexInLine)
            {
                var index = line * bytesPerLine + indexInLine;
                if (index >= Bytes.Length)
                    break;

                var makeSpacing = indexInLine % bytesPerGroup == bytesPerGroup - 1 && indexInLine != bytesPerLine - 1;
                
                var rect = new Rect(x, y, charWidth + (makeSpacing ? groupSpacing : 0), lineHeight);
                if (rect.Contains(point))
                {
                    inAscii = true;
                    return index;
                }
                
                x += charWidth;
                if (makeSpacing)
                    x += groupSpacing;
            }
        }

        inAscii = false;
        return null;
    }

    private bool isSelecting;
    private int? startingIndex;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
            
        var point = e.GetPosition(this) + offset;
        var index = GetIndexFromCoords(point, out var pressInAscii);
        if (index.HasValue)
        {
            SetCurrentValue(CaretIndexProperty, index.Value);
            SetCurrentValue(SelectionEndProperty, index.Value);
            SetCurrentValue(SelectionStartProperty, index.Value);
            SetCurrentValue(CaretInAsciiProperty, pressInAscii);
            isSelecting = true;
            startingIndex = index;
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (isSelecting && startingIndex.HasValue)
        {
            var point = e.GetPosition(this) + offset;
            var index = GetIndexFromCoords(point, out var pressInAscii);
            if (index.HasValue)
            {
                SetCurrentValue(SelectionStartProperty, Math.Min(startingIndex.Value, index.Value));
                SetCurrentValue(SelectionEndProperty, Math.Max(startingIndex.Value, index.Value) + 1);
                if (SelectionStart != SelectionEnd)
                    SetCurrentValue(CaretIndexProperty, null);
                SetCurrentValue(CaretInAsciiProperty, pressInAscii);
            }
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (isSelecting)
        {
            isSelecting = false;
            e.Handled = true;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (IsReadOnly)
            return;

        if (CaretInAscii && e.Text != null)
        {
            var bytes = Encoding.UTF8.GetBytes(e.Text);

            foreach (var b in bytes)
            {
                var caretIndex = CaretIndex ?? SelectionStart;
                SetCurrentValue(CaretIndexProperty, caretIndex);
                if (SelectionStart == SelectionEnd)
                {
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }
                
                Bytes[caretIndex] = b;
                CaretIndex++;
                if (SelectionStart == SelectionEnd || caretIndex >= SelectionEnd)
                {
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }

                e.Handled = true;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var selectMode = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        var offset = 0;
        if (e.Key is Key.Left or Key.Right or Key.Down or Key.Up)
        {
            caretBlink = true;
            if (e.Key == Key.Left)
                offset = -1;
            else if (e.Key == Key.Right)
                offset = 1;
            else if (e.Key == Key.Up)
                offset = -GroupsPerLine() * bytesPerGroup;
            else if (e.Key == Key.Down)
                offset = GroupsPerLine() * bytesPerGroup;
        }
        
        if (offset != 0)
        {
            if (selectMode)
            {
                int oldCaretIndex;

                if (!CaretIndex.HasValue)
                    oldCaretIndex = offset < 0 ? SelectionStart : SelectionEnd;
                else
                    oldCaretIndex = CaretIndex.Value;
                
                var newCaretIndex = oldCaretIndex + offset;
                SetCurrentValue(CaretIndexProperty, newCaretIndex);
                
                if (Math.Abs(oldCaretIndex - SelectionStart) < Math.Abs(oldCaretIndex - SelectionEnd))
                {
                    // The start of the selection is closer to the caret
                    if (CaretIndex < SelectionEnd)
                        SetCurrentValue(SelectionStartProperty, newCaretIndex);
                    else
                    {
                        // Switch the roles of start and end
                        SetCurrentValue(SelectionStartProperty, SelectionEnd);
                        SetCurrentValue(SelectionEndProperty, newCaretIndex);
                    }
                }
                else
                {
                    // The end of the selection is closer to the caret
                    if (CaretIndex > SelectionStart)
                        SetCurrentValue(SelectionEndProperty, newCaretIndex);
                    else
                    {
                        // Switch the roles of start and end
                        SetCurrentValue(SelectionEndProperty, SelectionStart);
                        SetCurrentValue(SelectionStartProperty, newCaretIndex);
                    }
                }
            }
            else
            {
                if (!CaretIndex.HasValue)
                    SetCurrentValue(CaretIndexProperty, offset < 0 ? SelectionStart : SelectionEnd);
                else
                    SetCurrentValue(CaretIndexProperty, CaretIndex.Value + offset);
                SetCurrentValue(SelectionStartProperty, CaretIndex ?? 0);
                SetCurrentValue(SelectionEndProperty, CaretIndex ?? 0);
            }
            e.Handled = true;   
        }
        else
        {
            if (!CaretInAscii)
            {
                byte? b = null;
                if (e.Key >= Key.D0 && e.Key <= Key.D9)
                    b = (byte)(e.Key - Key.D0);
                else if (e.Key >= Key.A && e.Key <= Key.F)
                    b = (byte)(e.Key - Key.A + 10);
                else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                    b = (byte)(e.Key - Key.NumPad0);

                var caretIndex = CaretIndex.HasValue ? CaretIndex.Value : SelectionStart;
                SetCurrentValue(CaretIndexProperty, caretIndex);
                
                if (b.HasValue)
                {
                    if (IsReadOnly)
                        return;

                    var currentByte = Bytes[caretIndex];
                    if (!updatedLowNibble)
                    {
                        Bytes[caretIndex] = b.Value;
                        updatedLowNibble = true;
                        InvalidateVisual();
                    }
                    else
                    {
                        currentByte = (byte)(currentByte << 4 | b.Value);
                        Bytes[caretIndex] = currentByte;
                        InvalidateVisual();
                        CaretIndex++;
                        if (SelectionStart == SelectionEnd || caretIndex >= SelectionEnd)
                        {
                            SetCurrentValue(SelectionStartProperty, caretIndex);
                            SetCurrentValue(SelectionEndProperty, caretIndex);
                        }
                    }

                    e.Handled = true;
                }
            }
        }
    }
}