using System;
using System.Collections.Generic;

namespace WDE.DatabaseEditors.Utils
{
    /// <summary>
    /// Utility class for creating <see cref="IEqualityComparer{T}"/> instances 
    /// from Lambda expressions.
    /// </summary>
    public static class EqualityComparerFactory
    {
        /// <summary>Creates the specified <see cref="IEqualityComparer{T}" />.</summary>
        /// <typeparam name="T">The type to compare.</typeparam>
        /// <param name="getHashCode">The get hash code delegate.</param>
        /// <param name="equals">The equals delegate.</param>
        /// <returns>An instance of <see cref="IEqualityComparer{T}" />.</returns>
        public static IEqualityComparer<T> Create<T>(
            Func<T, int> getHashCode,
            Func<T?, T?, bool> equals)
        {
            if (getHashCode == null)
            {
                throw new ArgumentNullException(nameof(getHashCode));
            }

            if (equals == null)
            {
                throw new ArgumentNullException(nameof(equals));
            }

            return new Comparer<T>(getHashCode, equals);
        }

        private class Comparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, int> _getHashCode;
            private readonly Func<T?, T?, bool> _equals;

            public Comparer(Func<T, int> getHashCode, Func<T?, T?, bool> equals)
            {
                _getHashCode = getHashCode;
                _equals = equals;
            }

            public bool Equals(T? x, T? y) => _equals(x, y);

            public int GetHashCode(T obj) => _getHashCode(obj);
        }
    }
}