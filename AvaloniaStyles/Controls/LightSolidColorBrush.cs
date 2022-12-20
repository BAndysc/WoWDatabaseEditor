using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using AvaloniaStyles.Utils;

namespace AvaloniaStyles.Controls;

public class LightSolidColorBrush : Brush, ISolidColorBrush
{
    public override IBrush ToImmutable() => new ImmutableSolidColorBrush(this);

    public static readonly StyledProperty<ISolidColorBrush> BaseProperty =
        AvaloniaProperty.Register<AccentSolidColorBrush, ISolidColorBrush>(nameof(Base));

    public static readonly StyledProperty<double> LightProperty =
        AvaloniaProperty.Register<AccentSolidColorBrush, double>(nameof(Light));

    [Content]
    public ISolidColorBrush Base
    {
        get => GetValue(BaseProperty);
        set => SetValue(BaseProperty, value);
    }

    public double Light
    {
        get => GetValue(LightProperty);
        set => SetValue(LightProperty, value);
    }
    
    public Color Color
    {
        get
        {
            var baseBrush = Base;
            var hsl = HslColor.FromRgba(baseBrush.Color);
            hsl = hsl.WithLightness(hsl.V + Light);
            return hsl.ToRgba(baseBrush.Opacity);
        }
    }
}