using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Controls;

public class SimpleGraph : Control
{
    public static readonly StyledProperty<float[]> SourceDataArrayProperty = AvaloniaProperty.Register<SimpleGraph, float[]>("SourceDataArray");
    public static readonly StyledProperty<int> SourceStartOffsetProperty = AvaloniaProperty.Register<SimpleGraph, int>("SourceStartOffset");
    public static readonly StyledProperty<float> MinYProperty = AvaloniaProperty.Register<SimpleGraph, float>("MinY");
    public static readonly StyledProperty<float> MaxYProperty = AvaloniaProperty.Register<SimpleGraph, float>("MaxY");
    public static readonly StyledProperty<IBrush> ForegroundProperty = AvaloniaProperty.Register<SimpleGraph, IBrush>("Foreground");
    public static readonly StyledProperty<int> LengthProperty = AvaloniaProperty.Register<SimpleGraph, int>("Length");

    static SimpleGraph()
    {
        AffectsRender<SimpleGraph>(SourceStartOffsetProperty, MinYProperty, MaxYProperty, LengthProperty);
    }

    FormattedText? maxText;
    FormattedText? minText;
    PolylineGeometry? geom;
    
    public override void Render(DrawingContext context)
    {
        var len = Length;
        var max = MaxY;
        var min = MinY;
        if (len == 0 || Bounds.Width <= 0 || (max - min) <= 0)
            return;


        maxText ??= new FormattedText(NiceNumber(MaxY), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 7, Foreground);
        minText ??= new FormattedText(NiceNumber(MinY), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 7, Foreground);
        var graphWidth = Bounds.Width - 25;

        var startY = maxText.Height;
        var graphHeight = Bounds.Height - startY;
        context.DrawText(maxText, new Point(0, 0));
        context.DrawText(minText, new Point(0, Bounds.Height - minText.Height));

        if (graphWidth < 0 || graphHeight < 0)
            return;
        
        var startOffset = SourceStartOffset;
        var range = (max - min);
        var widthPerPoint = graphWidth / len;

        geom = new PolylineGeometry(){IsFilled = true};
        geom.Points.Clear();
        geom.Points.Add(new Point(25, Bounds.Height));

        double x = 25;
        for (int i = startOffset; i < len; ++i)
        {
            float val = SourceDataArray[i];
            float y = (val - min) / range;
            geom.Points.Add(new Point(x, (1 - y) * graphHeight + startY));
            x += widthPerPoint;
        }
        for (int i = 0; i < startOffset; ++i)
        {
            float val = SourceDataArray[i];
            float y = (val - min) / range;
            geom.Points.Add(new Point(x, (1 - y) * graphHeight + startY));
            x += widthPerPoint;
        }

        geom.Points.Add(new Point(x - widthPerPoint, Bounds.Height));
        context.DrawGeometry(Foreground, null, geom);
    }

    private string NiceNumber(float val)
    {
        if (val > 1000000000)
            return (val / 1000000000).ToString("0.00") + "G";
        if (val > 1000000)
            return (val / 1000000).ToString("0.00") + "M";
        if (val > 1000)
            return (val / 1000).ToString("0.00") + "K";
        return val.ToString("0.00");
    }

    public float[] SourceDataArray
    {
        get => GetValue(SourceDataArrayProperty);
        set => SetValue(SourceDataArrayProperty, value);
    }

    public int SourceStartOffset
    {
        get => (int)GetValue(SourceStartOffsetProperty);
        set => SetValue(SourceStartOffsetProperty, value);
    }

    public float MinY
    {
        get => (float)GetValue(MinYProperty);
        set => SetValue(MinYProperty, value);
    }

    public float MaxY
    {
        get => (float)GetValue(MaxYProperty);
        set => SetValue(MaxYProperty, value);
    }

    public IBrush Foreground
    {
        get => (IBrush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public int Length
    {
        get => (int)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }
}