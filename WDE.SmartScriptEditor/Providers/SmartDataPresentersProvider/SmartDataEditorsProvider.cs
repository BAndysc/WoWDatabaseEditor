﻿using System.Collections.Generic;
using WDE.Common.History;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.Common.Menu;
using WDE.SmartScriptEditor.Data;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Services.MessageBox;

namespace WDE.SmartScriptEditor.Providers
{
    public enum SmartDataSourceMode
    {
        SD_SOURCE_EVENTS,
        SD_SOURCE_ACTIONS,
        SD_SOURCE_TARGETS,
    }
    [AutoRegister]
    class SmartDataEditorsProvider : IMainMenuItem
    {
        public string ItemName { get; } = "Editors";
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityNormal;

        public SmartDataEditorsProvider(ISmartRawDataProvider smartDataProvider, IParameterFactory parameterFactory, ISmartDataManager smartDataManager,
            ITaskRunner taskRunner, IMessageBoxService messageBoxService, IWindowManager windowManager, IHistoryManager history)
        {
            var editors = new List<IMenuDocumentItem> {
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Events", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Actions", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataCategoryMenuItemProvider<SmartDataDefinesListViewModel>("Targets", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_TARGETS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Event Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Action Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataCategoryMenuItemProvider<SmartDataGroupsEditorViewModel>("Target Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, history, SmartDataSourceMode.SD_SOURCE_TARGETS }),
            };
            
            IMenuCategoryItem obj = new SmartDataCategoryItem("Smart Data", editors.ToArray());
            SubItems = new List<IMenuItem>() {obj};
        }

        public List<IMenuItem> SubItems { get; }
    }

    internal class SmartDataCategoryItem : IMenuCategoryItem
    {
        public string ItemName { get; }
        public IMenuItem[] CategoryItems { get; }

        public SmartDataCategoryItem(string itemName, IMenuItem[] categoryItemDocuments)
        {
            ItemName = itemName;
            CategoryItems = categoryItemDocuments;
        }
    } 
}
