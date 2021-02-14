using System;
using System.Collections.Generic;
using WDE.Common.Menu;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Conditions.Data;
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

        public ConditionModuleMenuItem(Func<IHistoryManager> historyCreator, IConditionDataProvider conditionDataProvider, IWindowManager windowManager, ITaskRunner taskRunner,
            IParameterFactory parameterFactory, IMessageBoxService messageBoxService)
        {
            var editors = new List<IMenuItem>();
            
            editors.Add(new ConditionsEditorMenuItem<ConditionsDefinitionEditorViewModel>("Conditions", new object[]
            {
                historyCreator, conditionDataProvider, windowManager, taskRunner, parameterFactory
            }));
            editors.Add(new ConditionsEditorMenuItem<ConditionSourcesListEditorViewModel>("Condition Sources", new object[]
            {
                historyCreator, conditionDataProvider, parameterFactory, windowManager, taskRunner
            }));
            editors.Add(new ConditionsEditorMenuItem<ConditionGroupsEditorViewModel>("Condition Groups", new object[]
            {
                historyCreator, conditionDataProvider, windowManager, messageBoxService, taskRunner
            }));

            var conditionsCategory = new List<IMenuItem> {new ConditionsMenuCategory("Conditions", editors)};
            SubItems = new List<IMenuItem>() { new ConditionsMenuCategory("Smart Data", conditionsCategory) };
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