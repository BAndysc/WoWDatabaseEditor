using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Menu
{
    [NonUniqueProvider]
    public interface IMainMenuItem
    {
        string ItemName { get; }
        List<IMenuItem> SubItems { get; }
        MainMenuItemSortPriority SortPriority { get; }
    }

    public enum MainMenuItemSortPriority
    {
        PriorityVeryHigh,
        PriorityHigh,
        PriorityNormal,
        PriorityLow,
    }
}