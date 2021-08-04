using System;
using System.Collections.Generic;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Parameters;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors;
using WDE.TrinitySmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Providers.SmartDataPresentersProvider
{
    class SmartDataEditorsProvider : IMainMenuItem
    {
        public string ItemName { get; } = "Editors";
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityNormal;

        public SmartDataEditorsProvider(ISmartRawDataProvider smartDataProvider, IParameterFactory parameterFactory, ISmartDataManager smartDataManager,
            ITaskRunner taskRunner, IMessageBoxService messageBoxService, IWindowManager windowManager, Func<IHistoryManager> historyCreator)
        {
            var editors = new List<IMenuItem> {
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Events", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Actions", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Targets", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_TARGETS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Event Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Action Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Target Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, historyCreator, SmartDataSourceMode.SD_SOURCE_TARGETS }),
            };

            var saiCategory = new List<IMenuItem> {new SmartDataCategoryItem("Smart Scripts", editors)};
            IMenuCategoryItem obj = new SmartDataCategoryItem("Smart Data", saiCategory);
            SubItems = new List<IMenuItem>() {obj};
        }

        public List<IMenuItem> SubItems { get; }
    }

    internal class SmartDataCategoryItem : IMenuCategoryItem
    {
        public string ItemName { get; }
        public List<IMenuItem> CategoryItems { get; }

        public SmartDataCategoryItem(string itemName, List<IMenuItem> categoryItemDocuments)
        {
            ItemName = itemName;
            CategoryItems = categoryItemDocuments;
        }
    }
}
