using Avalonia;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia;

public static class Extensions
{
    public static T? SelfOrVisualAncestor<T>(this IVisual visual) where T : class
    {
        if (visual is T t)
            return t;
        return visual.FindAncestorOfType<T>();
    }

    public static bool GetResource<T>(this object? _, string key, T defaultVal, out T outT)
    {
        outT = defaultVal;
        if (Application.Current.Styles.TryGetResource(key, out var res) && res is T t)
        {
            outT = t;
            return true;
        }
        return false;
    }
}