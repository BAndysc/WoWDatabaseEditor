using Avalonia;

namespace AvaloniaStyles.Controls.FastTableView;

internal static class Extensions
{
    public static bool GetResource<T>(this object? _, string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        // todo: ava11 theme?
        if ((Application.Current?.Styles.TryGetResource(key, default, out var res) ?? false) && res is T t)
        {
            outT = t;
            return true;
        }
        if ((Application.Current?.Resources.TryGetResource(key, default, out var res2) ?? false) && res2 is T t2)
        {
            outT = t2;
            return true;
        }
        return false;
    }
}