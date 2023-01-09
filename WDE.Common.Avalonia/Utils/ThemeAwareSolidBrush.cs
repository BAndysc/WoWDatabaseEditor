using Avalonia.Media;

namespace WDE.Common.Avalonia.Utils;

public class ThemeAwareSolidBrush : ISolidColorBrush
{
    public Color DarkColor { get; set; }
    public Color LightColor { get; set; }
    public double Opacity { get; set; } = 1;

    public Color Color => AvaloniaStyles.SystemTheme.EffectiveThemeIsDark ? DarkColor : LightColor;
}