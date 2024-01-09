using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        
        public static int IndexIfObject(this IList list, Func<object?, bool> pred)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (pred(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexIf<T>(this IReadOnlyList<T> list, Func<T, bool> pred)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (pred(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }
        
        public static int IndexIf<T>(this IList<T> list, Func<T, bool> pred)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (pred(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexIf<T>(this List<T> list, Func<T, bool> pred)
        {
            return ((IList<T>)list).IndexIf(pred);
        }

        public static int IndexIf<T>(this T[] list, Func<T, bool> pred)
        {
            return ((IList<T>)list).IndexIf(pred);
        }
        
        public static int IndexIf<T>(this ObservableCollection<T> list, Func<T, bool> pred)
        {
            return ((IList<T>)list).IndexIf(pred);
        }

        public static int RemoveIf<T>(this IList<T> list, Func<T, bool> pred)
        {
            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (pred(list[i]))
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], item))
                    return i;
            }

            return -1;
        }

        public static int IndexOfIgnoreCase(this IReadOnlyList<string> list, string item)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Equals(item, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public static void AddIfNotNull<T>(this IList<T> list, T? element)
        {
            if (element != null)
                list.Add(element);
        }

        public static void AddIfNotNull<T>(this IList<T> list, IEnumerable<T?>? elements)
        {
            if (elements != null)
            {
                foreach (var element in elements)
                    if (element != null)
                        list.Add(element);
            }
        }
    }
}