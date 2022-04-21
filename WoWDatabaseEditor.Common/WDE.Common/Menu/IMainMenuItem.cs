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
    
    [NonUniqueProvider]
    public interface IToolMenuItem : IMenuCommandItem
    {
    }

    public enum MainMenuItemSortPriority
    {
        PriorityVeryHigh,
        PriorityHigh,
        PriorityNormal,
        PriorityLow,
    }
}