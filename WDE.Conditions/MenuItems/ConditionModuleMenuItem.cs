using System;
using System.Collections.Generic;
using WDE.Common.Menu;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.Conditions.ViewModels;
using WDE.Module.Attributes;

namespace WDE.Conditions.MenuItems
{
    [AutoRegister]
    public class ConditionModuleMenuItem: IMainMenuItem
    {
        public string ItemName { get; } = "Editors";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityNormal;

        public ConditionModuleMenuItem(Func<IHistoryManager> historyCreator)
        {
            var editors = new List<IMenuItem>();
            
            // TODO: Fill this out
            editors.Add(new ConditionsEditorMenuItem<ConditionsDefinitionEditorViewModel>("Conditions", new [] { historyCreator }));
            
            SubItems = new List<IMenuItem>() { new ConditionsMenuCategory("Smart Data", editors) };
        }
    }

    internal class ConditionsMenuCategory : IMenuCategoryItem
    {
        public string ItemName { get; }
        public List<IMenuItem> CategoryItems { get; }

        internal ConditionsMenuCategory(string itemName, List<IMenuItem> categoryItems)
        {
            ItemName = itemName;
            CategoryItems = categoryItems;
        }
    }
}