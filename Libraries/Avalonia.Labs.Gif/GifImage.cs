using System;
using System.IO;
using System.Numerics;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Labs.Gif.Decoding;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Labs.Gif;

/// <summary>
/// A control that presents GIF animations.
/// </summary>
public class GifImage : Control
{
    private CompositionCustomVisual? _customVisual;

    private double _gifWidth, _gifHeight;

    /// <summary>
    /// Defines the <see cref="Source"/> property.
    /// </summary>
    public static readonly StyledProperty<object> SourceProperty =
        AvaloniaProperty.Register<GifImage, object>(nameof(Source));

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
    /// Gets or sets the <see cref="Uri"/> or absolute uri <see cref="string"/> 
    /// pointing to the GIF image resource or
    /// <see cref="Stream"/> containing the GIF image.
    /// </summary>
    public object Source
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

        Stream stream;
        if (Source is Stream s)
        {
            stream = s;
        }
        else if (Source is Uri uri)
        {
            stream = AssetLoader.Open(uri);
        }
        else if (Source is string str)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out var uri2))
            {
                stream = AssetLoader.Open(uri2);
            }
            else
            {
                throw new ArgumentException(
                    "Unsupported Source object: only Stream, Uri and absolute uri string are supported.");
            }
        }
        else
        { 
            throw new ArgumentException(
                "Unsupported Source object: only Stream, Uri and absolute uri string are supported.");
        }

        using var tempGifDecoder = new GifDecoder(stream, CancellationToken.None);
        _gifHeight = tempGifDecoder.Size.Height;
        _gifWidth = tempGifDecoder.Size.Width;

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
