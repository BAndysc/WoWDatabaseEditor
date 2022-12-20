using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using AvaloniaStyles.Utils;

namespace AvaloniaStyles.Controls;

public class AccentSolidColorBrush : Brush, ISolidColorBrush
{
    public static readonly StyledProperty<Color> BaseColorProperty =
        AvaloniaProperty.Register<AccentSolidColorBrush, Color>(nameof(BaseColor));

    public static readonly StyledProperty<HslDiff> HueProperty =
        AvaloniaProperty.Register<AccentSolidColorBrush, HslDiff>(nameof(Hue));

    static AccentSolidColorBrush()
    {
        AffectsRender<AccentSolidColorBrush>(BaseColorProperty);
        AffectsRender<AccentSolidColorBrush>(HueProperty);
    }

    public AccentSolidColorBrush(Color color, double opacity = 1.0)
    {
        BaseColor = color;
        Opacity = opacity;
    }

    public AccentSolidColorBrush(uint color) : this(Color.FromUInt32(color))
    {
    }

    public AccentSolidColorBrush()
    {
        Opacity = 1;
    }

    /// <summary>Gets or sets the color of the brush.</summary>
    [Content]
    public Color BaseColor
    {
        get => GetValue(BaseColorProperty);
        set => SetValue(BaseColorProperty, value);
    }

    public HslDiff Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Parses a brush string.</summary>
    /// <param name="s">The brush string.</param>
    /// <returns>The <see cref="P:Avalonia.Media.SolidColorBrush.Color" />.</returns>
    /// <remarks>
    /// Whereas <see cref="M:Avalonia.Media.Brush.Parse(System.String)" /> may return an immutable solid color brush,
    /// this method always returns a mutable <see cref="T:Avalonia.Media.SolidColorBrush" />.
    /// </remarks>
    public static AccentSolidColorBrush Parse(string s)
    {
        ISolidColorBrush solidColorBrush1 = (ISolidColorBrush)Brush.Parse(s);
        return !(solidColorBrush1 is AccentSolidColorBrush solidColorBrush2)
            ? new AccentSolidColorBrush(solidColorBrush1.Color)
            : solidColorBrush2;
    }

    public override string ToString() => Color.ToString();

    /// <inheritdoc />
    public override IBrush ToImmutable() => new ImmutableSolidColorBrush(this);

    public Color Color
    {
        get
        {
            var hsl = HslColor.FromRgba(BaseColor).Scale(Hue);
            return hsl.ToRgba();
        }
        set
        {
            var hsl = HslColor.FromRgba(value);
            //Hue = (float)hsl.H;
            BaseColor = value;
        }
    }
}