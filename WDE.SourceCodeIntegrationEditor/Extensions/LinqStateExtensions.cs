using System;
using System.Collections.Generic;

namespace WDE.SourceCodeIntegrationEditor.Extensions
{
    public static class LinqStateExtensions
    {
        public static IEnumerable<T> Between<T>(this IEnumerable<T> that, Func<T, bool> start, Func<T, bool> end, bool inverse = false)
        {
            bool choose = false;
            foreach (var f in that)
            {
                if (!choose)
                    choose = start(f);
                var pickThat = choose;
                if (pickThat)
                    choose = !end(f);

                if (inverse)
                    pickThat = !pickThat;
                
                if (pickThat)
                    yield return f;
            }
        }
    }
}