using System;
using WDE.Common.Managers;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;

namespace WDE.SmartScriptEditor.Data
{
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

        public int? Get(SmartType type, Func<SmartGenericJsonData, bool> predicate)
        {
            SmartSelectViewModel model = new(GetFileNameFor(type), type, predicate, smartDataManager, conditionDataManager);
            if (windowManager.ShowDialog(model))
                return model.SelectedItem.Id;

            return null;
        }

        private string GetFileNameFor(SmartType type)
        {
            switch (type)
            {
                case SmartType.SmartEvent:
                    return "events.txt";
                case SmartType.SmartAction:
                    return "actions.txt";
                case SmartType.SmartTarget:
                    return "targets.txt";
                case SmartType.SmartSource:
                    return "targets.txt";
                case SmartType.SmartCondition:
                    return "conditions.txt";
            }

            return null;
        }
    }
}