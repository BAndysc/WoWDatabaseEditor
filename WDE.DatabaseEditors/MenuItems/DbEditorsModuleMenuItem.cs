using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Tasks;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.MenuItems
{
    [AutoRegister]
    public class DbEditorsModuleMenuItem : IMainMenuItem
    {
        public string ItemName { get; }
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; }

        public DbEditorsModuleMenuItem(IDbEditorTableDataProvider tableDataProvider, ITaskRunner taskRunner, IWindowManager windowManager)
        {
            ItemName = "Editors";
            SortPriority = MainMenuItemSortPriority.PriorityNormal;
            var editors = new List<IMenuItem>
            {
                new DbEditorMenuItem<CreatureTemplateDbEditorViewModel>("Creature Template",
                    new object[] { tableDataProvider, taskRunner, windowManager })
            };

            SubItems = new List<IMenuItem>() {new DbEditorsCategoryMenuItem("Database", editors)};
        }
    }

    internal class DbEditorsCategoryMenuItem : IMenuCategoryItem
    {
        public string ItemName { get; }
        public List<IMenuItem> CategoryItems { get; }

        public DbEditorsCategoryMenuItem(string itemName, List<IMenuItem> categoryItems)
        {
            ItemName = itemName;
            CategoryItems = categoryItems;
        }
    }
}