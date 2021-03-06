using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Tasks;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
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

        public DbEditorsModuleMenuItem(IDbEditorTableDataProvider tableDataProvider, ICreatureEntryProviderService creatureEntryProviderService, IItemFromListProvider itemFromListProvider, 
            IParameterFactory parameterFactory, Func<IHistoryManager> historyCreator, ITaskRunner taskRunner, IWindowManager windowManager)
        {
            ItemName = "Editors";
            SortPriority = MainMenuItemSortPriority.PriorityNormal;
            Func<Task<uint?>> creatureEntryProvider = creatureEntryProviderService.GetEntryFromService;
            Func<uint, Task<IDbTableData>> creatureDataProvider = tableDataProvider.LoadCreatureTamplateDataEntry;
            var editors = new List<IMenuItem>
            {
                new DbEditorMenuItem<CreatureTemplateDbEditorViewModel>("Creature Template",
                    new object[] { tableDataProvider, itemFromListProvider, parameterFactory, creatureEntryProvider, creatureDataProvider, historyCreator, taskRunner, windowManager })
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