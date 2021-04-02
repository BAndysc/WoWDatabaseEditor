using System;
using WDE.Common.Managers;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Data
{
    [SingleInstance]
    [AutoRegister]
    public class SmartTypeListProvider : ISmartTypeListProvider
    {
        private readonly IWindowManager windowManager;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;

        public SmartTypeListProvider(IWindowManager windowManager, ISmartDataManager smartDataManager, IConditionDataManager conditionDataManager)
        {
            this.windowManager = windowManager;
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;
        }

        public async System.Threading.Tasks.Task<int?> Get(SmartType type, Func<SmartGenericJsonData, bool> predicate)
        {
            var title = GetTitleForType(type);
            SmartSelectViewModel model = new(title, type, predicate, smartDataManager, conditionDataManager);
            
            if (await windowManager.ShowDialog(model))
                return model.SelectedItem.Id;

            return null;
        }

        private string GetTitleForType(SmartType type)
        {
            switch (type)
            {
                case SmartType.SmartEvent:
                    return "Pick event";
                case SmartType.SmartAction:
                    return "Pick action";
                case SmartType.SmartTarget:
                    return "Pick action target";
                case SmartType.SmartCondition:
                    return "Pick condition";
                case SmartType.SmartConditionSource:
                    return "Pick condition source";
                case SmartType.SmartSource:
                    return "Pick action source";
                default:
                    return "Pick";
            }
        }
    }
}