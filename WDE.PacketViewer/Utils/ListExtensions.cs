using System.Collections.Generic;

namespace WDE.PacketViewer.Utils;

public static class ListExtensions
{
    public static List<T> ToSingletonList<T>(this T item)
    {
        return new List<T> { item };
    }
}