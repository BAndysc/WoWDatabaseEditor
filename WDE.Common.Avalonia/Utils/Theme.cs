using Avalonia;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Utils;

public class Theme
{
    public static readonly StyledProperty<Color> LightColorProperty =
        AvaloniaProperty.RegisterAttached<Theme, SolidColorBrush, Color>("LightColor");

    public static readonly StyledProperty<Color> DarkColorProperty =
        AvaloniaProperty.RegisterAttached<Theme, SolidColorBrush, Color>("DarkColor");

    static Theme()
    {
        LightColorProperty.Changed.AddClassHandler<SolidColorBrush>((brush, e) => UpdateBrushColor(brush));
        DarkColorProperty.Changed.AddClassHandler<SolidColorBrush>((brush, e) => UpdateBrushColor(brush));
    }

    private static void UpdateBrushColor(SolidColorBrush brush)
    {
        var light = brush.IsSet(LightColorProperty) ? GetLightColor(brush) : brush.Color;
        var dark = brush.IsSet(DarkColorProperty) ? GetDarkColor(brush) : brush.Color;
        brush.Color = AvaloniaStyles.SystemTheme.EffectiveThemeIsDark ? dark : light;
    }

    public static Color GetLightColor(AvaloniaObject obj)
    {
        return obj.GetValue(LightColorProperty);
    }

    public static void SetLightColor(AvaloniaObject obj, Color value)
    {
        obj.SetValue(LightColorProperty, value);
    }

    public static Color GetDarkColor(AvaloniaObject obj)
    {
        return obj.GetValue(DarkColorProperty);
    }

    public static void SetDarkColor(AvaloniaObject obj, Color value)
    {
        obj.SetValue(DarkColorProperty, value);
    }
}