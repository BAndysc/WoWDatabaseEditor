using System;
using Avalonia.Media;
using AvaloniaEdit.Document;

namespace WDE.Common.Avalonia.Controls.TextMarkers;

public sealed class TextMarker : TextSegment, ITextMarker
{
    readonly TextMarkerService service;

    public TextMarker(TextMarkerService service, TextMarkerTypes type, int startOffset, int length)
    {
        this.service = service;
        this.StartOffset = startOffset;
        this.Length = length;
        this.markerTypes = type;
    }

    public event EventHandler? Deleted;

    public bool IsDeleted
    {
        get { return !this.IsConnectedToCollection; }
    }

    public void Delete()
    {
        service.Remove(this);
    }

    internal void OnDeleted()
    {
        if (Deleted != null)
            Deleted(this, EventArgs.Empty);
    }

    void Redraw()
    {
        service.Redraw(this);
    }

    IBrush? backgroundBrush;

    public IBrush? BackgroundBrush
    {
        get => backgroundBrush;
        set
        {
            if (!Equals(backgroundBrush, value))
            {
                backgroundBrush = value;
                Redraw();
            }
        }
    }

    IBrush? foregroundBrush;

    public IBrush? ForegroundBrush
    {
        get => foregroundBrush;
        set
        {
            if (!Equals(foregroundBrush, value))
            {
                foregroundBrush = value;
                Redraw();
            }
        }
    }

    FontWeight? fontWeight;

    public FontWeight? FontWeight
    {
        get => fontWeight;
        set
        {
            if (fontWeight != value)
            {
                fontWeight = value;
                Redraw();
            }
        }
    }

    FontStyle? fontStyle;

    public FontStyle? FontStyle
    {
        get => fontStyle;
        set
        {
            if (fontStyle != value)
            {
                fontStyle = value;
                Redraw();
            }
        }
    }

    public object? Tag { get; set; }

    TextMarkerTypes markerTypes;

    public TextMarkerTypes MarkerTypes => markerTypes;

    Color markerColor;

    public Color MarkerColor
    {
        get => markerColor;
        set
        {
            if (markerColor != value)
            {
                markerColor = value;
                Redraw();
            }
        }
    }

    public object? ToolTip { get; set; }
}