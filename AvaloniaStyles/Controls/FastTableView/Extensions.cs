using Avalonia;
using Avalonia.Styling;

namespace AvaloniaStyles.Controls.FastTableView;

internal static partial class Extensions
{
    public static bool GetResource<T>(this object? _, string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if ((Application.Current?.Styles.TryGetResource(key, SystemTheme.EffectiveThemeVariant, out var res) ?? false) && res is T t)
        {
            outT = t;
            return true;
        }
        if ((Application.Current?.Resources.TryGetResource(key, SystemTheme.EffectiveThemeVariant, out var res2) ?? false) && res2 is T t2)
        {
            outT = t2;
            return true;
        }
        return false;
    }
}