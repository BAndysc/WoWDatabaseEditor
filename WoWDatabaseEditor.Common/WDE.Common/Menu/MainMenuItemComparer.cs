using System;
using System.Collections.Generic;

namespace WDE.Common.Menu
{
    public class MainMenuItemComparer: IComparer<IMainMenuItem>
    {
        public int Compare(IMainMenuItem? x, IMainMenuItem? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            var prioSort = (x.SortPriority == y.SortPriority) ? 0 : (x.SortPriority < y.SortPriority ? -1 : 1);
            if (prioSort == 0) return string.Compare(x.ItemName, y.ItemName, StringComparison.Ordinal);
            return prioSort;
        }
    }
}