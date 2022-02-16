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
}