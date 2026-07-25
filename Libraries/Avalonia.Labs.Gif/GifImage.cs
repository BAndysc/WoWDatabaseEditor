using System;
using System.IO;
using System.Numerics;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace Avalonia.Labs.Gif;

/// <summary>
/// A control that presents GIF animations.
/// </summary>
public class GifImage : Control
{
    private CompositionCustomVisual? _customVisual;

    private double _gifWidth, _gifHeight;
    private Stream? _lastSourceStream = null;
    private MemoryStream _lastMemoryFromSourceStream = new();

    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<IGifSource?> SourceProperty =
        AvaloniaProperty.Register<GifImage, IGifSource?>(nameof(Source));

    /// <summary>
    /// Defines the <see cref="IterationCount"/> property.
    /// </summary>
    public static readonly StyledProperty<IterationCount> IterationCountProperty =
        AvaloniaProperty.Register<GifImage, IterationCount>(nameof(IterationCount), IterationCount.Infinite);

    /// <summary>
    /// Defines the <see cref="StretchDirection"/> property.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<GifImage, StretchDirection>(nameof(StretchDirection));

    /// <summary>
    /// Defines the <see cref="Stretch"/> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<GifImage, Stretch>(nameof(Stretch));

    /// <summary>
    /// Gets or sets the <see cref="IGifSource"/>
    /// pointing to the GIF image.
    /// </summary>
    public IGifSource? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling how the image will be stretched.
    /// </summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets a value controlling in what direction the image will be stretched.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount in which the GIF image loops.
    /// </summary>
    public IterationCount IterationCount
    {
        get => GetValue(IterationCountProperty);
        set => SetValue(IterationCountProperty, value);
    }

    static GifImage()
    {
        AffectsRender<GifImage>(SourceProperty,
            StretchProperty,
            StretchDirectionProperty,
            WidthProperty,
            HeightProperty);

        AffectsMeasure<GifImage>(SourceProperty,
            StretchProperty,
            StretchDirectionProperty,
            WidthProperty,
            HeightProperty);
    }

    private Size GetGifSize()
    {
        return new Size(_gifWidth, _gifHeight);
    }

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        return Stretch.CalculateSize(availableSize, GetGifSize(), StretchDirection);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var sourceSize = GetGifSize();
        var result = Stretch.CalculateSize(finalSize, sourceSize);
        return result;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        InitializeGif();
    }

    /// <inheritdoc /> 
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var avProp = change.Property;

        if (avProp == SourceProperty)
        {
            InitializeGif();
        }

        if ((avProp == SourceProperty ||
             avProp == StretchProperty ||
             avProp == StretchDirectionProperty ||
             avProp == IterationCountProperty) && _customVisual is not null)
        {
            _customVisual.SendHandlerMessage(
                new GifDrawPayload(
                    HandlerCommand.Update,
                    null,
                    GetGifSize(),
                    Bounds.Size,
                    Stretch,
                    StretchDirection,
                    IterationCount));
        }
    }

    private void InitializeGif()
    {
        Stop();
        DisposeImpl();

        var elemVisual = ElementComposition.GetElementVisual(this);
        var compositor = elemVisual?.Compositor;

        if (compositor is null)
        {
            return;
        }

        _customVisual = compositor.CreateCustomVisual(new GifCompositionCustomVisualHandler());

        ElementComposition.SetElementChildVisual(this, _customVisual);

        LayoutUpdated += OnLayoutUpdated;

        _customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);

        Stream? stream, s = Source?.GetStream();
        // only perform the copying if the stream has actually changed. 
        // if the stream has not changed, seek our internal memory stream that matches the last stream source.
        // This ensures that the gif data stream is at the right position, even with multiple reads. 
        if (Object.ReferenceEquals(_lastSourceStream, s))
        {
            _lastMemoryFromSourceStream.Seek(0, SeekOrigin.Begin);
            stream = _lastMemoryFromSourceStream;
        }
        else if (s is not null)
        {
            _lastSourceStream = s;
            _lastMemoryFromSourceStream = new();
            s.CopyTo(_lastMemoryFromSourceStream);
            _lastMemoryFromSourceStream.Seek(0, SeekOrigin.Begin);
            stream = _lastMemoryFromSourceStream;
        }
        else
        {
            stream = null;
        }

        _gifWidth = Source?.Size.Width ?? 0.0;
        _gifHeight = Source?.Size.Height ?? 0.0;

        _customVisual?.SendHandlerMessage(
            new GifDrawPayload(
                HandlerCommand.Start,
                stream,
                GetGifSize(),
                Bounds.Size,
                Stretch,
                StretchDirection,
                IterationCount));

        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;

        Stop();
        DisposeImpl();
    }


    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_customVisual == null)
        {
            return;
        }

        _customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);

        _customVisual.SendHandlerMessage(
            new GifDrawPayload(
                HandlerCommand.Update,
                null,
                GetGifSize(),
                Bounds.Size,
                Stretch,
                StretchDirection,
                IterationCount));
    }

    private void Stop()
    {
        _customVisual?.SendHandlerMessage(new GifDrawPayload(HandlerCommand.Stop));
    }

    private void DisposeImpl()
    {
        _customVisual?.SendHandlerMessage(new GifDrawPayload(HandlerCommand.Dispose));
        _customVisual = null;
    }
}
