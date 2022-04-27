using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WDE.Common.Utils
{
    public static class LinqExtensions
    {
        public static void Do<T>(this T? t, Action<T> action)
        {
            if (t != null)
                action(t);
        }
        
        public static async Task<IEnumerable<TResult>> SelectAsync<TSource,TResult>(
            this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method)
        {
            return await Task.WhenAll(source.Select(method));
        }
        
        public static Dictionary<TKey, TElement> SafeToDictionary<TSource, TKey, TElement>(
            this IEnumerable<TSource>? source, 
            Func<TSource, TKey> keySelector, 
            Func<TSource, TElement> elementSelector, 
            IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            var dictionary = new Dictionary<TKey, TElement>(comparer);

            if (source == null)
            {
                return dictionary;
            }

            foreach (TSource element in source)
            {
                dictionary[keySelector(element)] = elementSelector(element);
            }

            return dictionary; 
        }
        
        public static void RemoveAll<T>(this ObservableCollection<T> collection)
        {
            for (int i = collection.Count - 1; i >= 0; --i)
                collection.RemoveAt(i);
        }

        public static void Each<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var x in collection)
                action(x);
        }

        public static void Each<T>(this IEnumerable<T> collection, Action<T, int> action)
        {
            int index = 0;
            foreach (var x in collection)
                action(x, index++);
        }
    }
}