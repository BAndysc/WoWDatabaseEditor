using System;

namespace WDE.Common.Utils
{
    public static class LinqExtensions
    {
        public static void Do<T>(this T? t, Action<T> action)
        {
            if (t != null)
                action(t);
        }
    }
}