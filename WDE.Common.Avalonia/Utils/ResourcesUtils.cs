using Avalonia;
using Avalonia.Styling;
using AvaloniaStyles;

namespace WDE.Common.Avalonia.Utils;

public static class ResourcesUtils
{
    public static bool Get<T>(string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current!.Styles.TryGetResource(key, SystemTheme.EffectiveThemeVariant, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        return false;
    }
}
