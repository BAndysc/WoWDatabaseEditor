using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Menu;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Providers
{
    [UniqueProvider]
    [AutoRegister]
    [SingleInstance]
    public class EditorMainMenuItemsProvider
    {
        private readonly IEnumerable<IMainMenuItem> menuItemProviders;
        
        public EditorMainMenuItemsProvider(IEnumerable<IMainMenuItem> menuItemProviders)
        {
            this.menuItemProviders = menuItemProviders;
        }

        public List<IMainMenuItem> GetItems()
        {
            var itemsDict = new Dictionary<string, IMainMenuItem>();
            foreach (var menuItem in menuItemProviders)
            {
                if (itemsDict.ContainsKey(menuItem.ItemName) &&
                    itemsDict[menuItem.ItemName] is MainMenuSubItemsAggregator aggregator)
                    aggregator.AddSubItems(menuItem.SubItems);
                else
                    itemsDict.Add(menuItem.ItemName,
                        new MainMenuSubItemsAggregator(menuItem.ItemName, menuItem.SortPriority, menuItem.SubItems.ToList()));
            }
        
            var list = itemsDict.Values.ToList();
            list.Sort(new MainMenuItemComparer());
            return list;
        }
    }
    
    internal class MainMenuSubItemsAggregator: IMainMenuItem
    {
        public string ItemName { get; }
        private readonly List<IMenuItem> subItems;
        public MainMenuItemSortPriority SortPriority { get; }
        public List<IMenuItem> SubItems => subItems;
    
        internal MainMenuSubItemsAggregator(string itemName, MainMenuItemSortPriority sortPriority, List<IMenuItem> subItems)
        {
            this.subItems = subItems;
            SortPriority = sortPriority;
            ItemName = itemName; 
        }

        public void AddSubItems(IEnumerable<IMenuItem> items)
        {
            foreach (var item in items)
            {
                var itemIndex = subItems.FindIndex(x => x.ItemName == item.ItemName);
                if (itemIndex == -1)
                    subItems.Add(item);
                else
                {
                    var existingItem = subItems[itemIndex];
                    if (existingItem is IMenuSeparator && item is IMenuSeparator)
                    {
                        subItems.Add(item);
                        continue;
                    }
                    
                    if (existingItem is not IMenuCategoryItem && item is not IMenuCategoryItem)
                        throw new DuplicatedMenuItemException(
                    $"Found duplicated menu item of non category type in MainMenuItem ({ItemName})! Duplicated name: {item.ItemName}");

                    if (existingItem is MenuItemsAggregator aggregator)
                    {
                        if (item is IMenuCategoryItem categoryItem)
                            aggregator.AddSubItems(categoryItem.CategoryItems);
                        else
                            aggregator.AddSubItems(new List<IMenuItem> { item });
                    }
                    else
                    {
                        subItems.RemoveAt(itemIndex);
                        var mia = new MenuItemsAggregator(existingItem.ItemName, new List<IMenuItem>());
                        if (existingItem is IMenuCategoryItem categoryItem)
                            mia.AddSubItems(categoryItem.CategoryItems);
                        else
                            mia.AddSubItems(new List<IMenuItem> { existingItem });
                        
                        if (item is IMenuCategoryItem newCategoryItem)
                            mia.AddSubItems(newCategoryItem.CategoryItems);
                        else
                            mia.AddSubItems(new List<IMenuItem> { item });
                        subItems.Insert(itemIndex, mia);
                    }
                }
            }
        }
    }

    internal class MenuItemsAggregator : IMenuCategoryItem
    {
        public string ItemName { get; }
        private readonly List<IMenuItem> subItems;
        public List<IMenuItem> CategoryItems => subItems;
        
        internal MenuItemsAggregator(string itemName, List<IMenuItem> categoryItems)
        {
            ItemName = itemName;
            subItems = categoryItems;
        }

        internal void AddSubItems(List<IMenuItem> items)
        {
            foreach (var item in items)
            {
                var itemIndex = subItems.FindIndex(x => x.ItemName == item.ItemName);
                if (itemIndex == -1)
                    subItems.Add(item);
                else
                {
                    var existingItem = subItems[itemIndex];
                    if (existingItem is IMenuSeparator && item is IMenuSeparator)
                    {
                        subItems.Add(item);
                        continue;
                    }
                    
                    if (existingItem is not IMenuCategoryItem && item is not IMenuCategoryItem)
                        throw new DuplicatedMenuItemException(
                            $"Found duplicated menu item of non category type in MainMenuItem ({ItemName})! Duplicated name: {item.ItemName}");

                    if (existingItem is MenuItemsAggregator aggregator)
                    {
                        if (item is IMenuCategoryItem categoryItem)
                            aggregator.AddSubItems(categoryItem.CategoryItems);
                        else
                            aggregator.AddSubItems(new List<IMenuItem> { item });
                    }
                    else
                    {
                        subItems.RemoveAt(itemIndex);
                        var mia = new MenuItemsAggregator(existingItem.ItemName, new List<IMenuItem>());
                        if (existingItem is IMenuCategoryItem categoryItem)
                            mia.AddSubItems(categoryItem.CategoryItems);
                        else
                            mia.AddSubItems(new List<IMenuItem> { existingItem });
                        
                        if (item is IMenuCategoryItem newCategoryItem)
                            mia.AddSubItems(newCategoryItem.CategoryItems);
                        else
                            mia.AddSubItems(new List<IMenuItem> { item });
                        subItems.Insert(itemIndex, mia);
                    }
                }
            }
        }
    }
    
    internal class DuplicatedMenuItemException : Exception
    {
        internal DuplicatedMenuItemException(string msg) : base(msg)
        {
        }
    }
}