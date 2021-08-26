using System.Collections.Generic;

namespace AvaloniaStyles.Utils
{
    internal static class ListExtensions
    {
        public static void OverrideWith<T>(this IList<T> that, IList<T> with)
        {
            int i = 0;
            foreach (var r in with)
            {
                if (i < that.Count)
                    that[i] = r;
                else
                    that.Add(r);

                i++;
            }
            while (that.Count > i)
            {
                that.RemoveAt(that.Count - 1);
            }
        }
    }
}