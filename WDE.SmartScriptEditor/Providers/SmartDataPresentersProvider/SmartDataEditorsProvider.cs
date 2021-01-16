using System.Collections.Generic;
using WDE.Common.Providers;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.Editor.ViewModels;
using System.Linq;
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
    class SmartDataEditorsProvider : IDataDefinitionsProvider
    {
        private readonly IEnumerable<IDataDefinitionEditor> editors;
        public SmartDataEditorsProvider(ISmartDataProvider smartDataProvider, IParameterFactory parameterFactory, ISmartDataManager smartDataManager,
            ITaskRunner taskRunner, IMessageBoxService messageBoxService, IWindowManager windowManager)
        {
            editors = new List<IDataDefinitionEditor> {
                new SmartDataPresenter<SmartDataDefinesListViewModel>("Smart Data - Events", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataPresenter<SmartDataDefinesListViewModel>("Smart Data - Actions", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataPresenter<SmartDataDefinesListViewModel>("Smart Data - Targets", new object[] { smartDataProvider, smartDataManager, parameterFactory,
                    taskRunner, messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_TARGETS }),
                new SmartDataPresenter<SmartDataGroupsEditorViewModel>("Smart Data - Event Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_EVENTS }),
                new SmartDataPresenter<SmartDataGroupsEditorViewModel>("Smart Data - Action Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_ACTIONS }),
                new SmartDataPresenter<SmartDataGroupsEditorViewModel>("Smart Data - Target Groups", new object[] { smartDataProvider, taskRunner, 
                    messageBoxService, windowManager, SmartDataSourceMode.SD_SOURCE_TARGETS }),
            }.AsEnumerable();
        }
        public string GetDataCategoryName() => "Smart Data";

        public IEnumerable<IDataDefinitionEditor> GetDataDefinitionEditors() => editors;
    }
}
