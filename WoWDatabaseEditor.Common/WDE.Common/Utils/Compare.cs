using System;
using System.Collections.Generic;

namespace WDE.Common.Utils
{
    public static class Compare
    {
        public static IEqualityComparer<TSource> By<TSource, TIdentity>(Func<TSource?, TIdentity> identitySelector) where TIdentity : notnull
        {
            return new DelegateComparer<TSource, TIdentity>(identitySelector);
        }

        private class DelegateComparer<T, TIdentity> : IEqualityComparer<T> where TIdentity : notnull
        {
            private readonly Func<T?, TIdentity> identitySelector;

            public DelegateComparer(Func<T?, TIdentity> identitySelector)
            {
                this.identitySelector = identitySelector;
            }

            public bool Equals(T? x, T? y)
            {
                return Equals(identitySelector(x), identitySelector(y));
            }

            public int GetHashCode(T obj)
            {
                return identitySelector(obj).GetHashCode();
            }
        }
        
        public static IEqualityComparer<TSource> CreateEqualityComparer<TSource>(Func<TSource?, TSource?, bool> equals, Func<TSource, int> hashcode)
        {
            return new DelegateEqualityComparer<TSource>(equals, hashcode);
        }

        private class DelegateEqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T?, T?, bool> @equals;
            private readonly Func<T, int> hashcode;

            public DelegateEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> hashcode)
            {
                this.@equals = @equals;
                this.hashcode = hashcode;
            }

            public bool Equals(T? x, T? y)
            {
                return @equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return hashcode(obj);
            }
        }
        public static IComparer<TSource> CreateComparer<TSource>(Func<TSource?, TSource?, int> comparer)
        {
            return new DelegateComparer<TSource>(comparer);
        }

        private class DelegateComparer<T> : IComparer<T>
        {
            private readonly Func<T?, T?, int> comparer;

            public DelegateComparer(Func<T?, T?, int> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare(T? x, T? y)
            {
                return comparer(x, y);
            }
        }
    }
}