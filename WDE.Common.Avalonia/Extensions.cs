using Avalonia;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaStyles;

namespace WDE.Common.Avalonia;

public static class Extensions
{
    public static T? SelfOrVisualAncestor<T>(this Visual visual) where T : class
    {
        if (visual is T t)
            return t;
        return visual.FindAncestorOfType<T>();
    }

    public static bool GetResource<T>(this object? _, string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current!.Styles.TryGetResource(key, SystemTheme.EffectiveThemeIsDark ? ThemeVariant.Dark : ThemeVariant.Light, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        if (Application.Current.Resources.TryGetResource(key, SystemTheme.EffectiveThemeIsDark ? ThemeVariant.Dark : ThemeVariant.Light, out var res2) && res2 is T t2)
        {
            outT = t2;
            return true;
        }
        return false;
    }
}
