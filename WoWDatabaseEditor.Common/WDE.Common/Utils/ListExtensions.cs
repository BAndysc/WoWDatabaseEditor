using System;
using System.Collections.Generic;

namespace WDE.Common.Utils
{
    public static class ListExtensions
    {
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
    }
}