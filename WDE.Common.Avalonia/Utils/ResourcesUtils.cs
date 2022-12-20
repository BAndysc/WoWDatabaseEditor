using Avalonia;

namespace WDE.Common.Avalonia.Utils;

public static class ResourcesUtils
{
    public static bool Get<T>(string key, T defaultVal, out T outT)
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