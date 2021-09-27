using System;
using System.Collections.Generic;
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
    }
}