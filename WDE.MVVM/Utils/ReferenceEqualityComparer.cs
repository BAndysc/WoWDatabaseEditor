using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WDE.MVVM.Utils
{
    /// <summary>
    /// An equality comparer that compares objects for reference equality.
    /// </summary>
    /// <typeparam name="T">The type of objects to compare.</typeparam>
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        #region Predefined

        /// <summary>
        /// Gets the default instance of the
        /// <see cref="ReferenceEqualityComparer{T}"/> class.
        /// </summary>
        /// <value>A &lt;see cref="ReferenceEqualityComparer&lt;T&gt;"/&gt; instance.</value>
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        #endregion

        /// <inheritdoc />
        public bool Equals(T? left, T? right)
        {
            return ReferenceEquals(left, right);
        }

        /// <inheritdoc />
        public int GetHashCode(T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }
}