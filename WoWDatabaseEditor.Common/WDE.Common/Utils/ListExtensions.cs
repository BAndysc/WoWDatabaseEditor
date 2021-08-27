using System;
using System.Collections.Generic;

namespace WDE.Common.Utils
{
    public static class ListExtensions
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
        
        public static T? RemoveFirstIf<T>(this IList<T> list, Func<T, bool> pred)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (pred(list[i]))
                {
                    var elem = list[i];
                    list.RemoveAt(i);
                    return elem;
                }
            }

            return default;
        }
        
        public static T? RemoveIf<T>(this IList<T> list, Func<T, bool> pred)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (pred(list[i]))
                {
                    list.RemoveAt(i);
                }
            }

            return default;
        }
    }
}