using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WDE.PacketViewer.Utils;

public static class BinarySearchUtils
{
    internal static int BinarySearchOrGreater<T, TSearch>(this IList<T> array, TSearch value, Func<T, TSearch, int> comparer)
    {
        Debug.Assert(array != null, "Check the arguments in the caller!");

        int lo = 0;
        int hi = array.Count - 1;
        while (lo <= hi)
        {
            int i = lo + ((hi - lo) >> 1);
            int order = comparer(array[i], value);

            if (order == 0) return i;
            if (order < 0)
            {
                lo = i + 1;
            }
            else
            {
                hi = i - 1;
            }
        }

        return ~lo;
    }
}