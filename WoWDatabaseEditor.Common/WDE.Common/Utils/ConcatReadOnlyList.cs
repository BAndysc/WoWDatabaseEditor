using System.Collections;
using System.Collections.Generic;

namespace WDE.Common.Utils;

public static class ConcatReadOnlyListExtensions
{
    public static IReadOnlyList<T> Concat<T>(this IReadOnlyList<T> a, IReadOnlyList<T> b)
    {
        return new ConcatReadOnlyList<T>(a, b);
    }
    
    private class ConcatReadOnlyList<T> : IReadOnlyList<T>
    {
        private IReadOnlyList<T> a, b;

        public ConcatReadOnlyList(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            this.a = a;
            this.b = b;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var element in a)
            {
                yield return element;
            }
            foreach (var element in b)
            {
                yield return element;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => a.Count + b.Count;

        public T this[int index]
        {
            get
            {
                if (index < a.Count)
                    return a[index];
                return b[index - a.Count];   
            }
        }
    }
}