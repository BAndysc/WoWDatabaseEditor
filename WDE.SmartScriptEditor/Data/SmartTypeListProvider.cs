using System;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Editor.Views;

namespace WDE.SmartScriptEditor.Data
{
    [AutoRegister]
    public class SmartTypeListProvider : ISmartTypeListProvider
    {
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;

        public SmartTypeListProvider(ISmartDataManager smartDataManager, IConditionDataManager conditionDataManager)
        {
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;
        }

        public int? Get(SmartType type, Func<SmartGenericJsonData, bool> predicate)
        {
            SmartSelectView view = new();
            SmartSelectViewModel model = new(GetFileNameFor(type), type, predicate, smartDataManager, conditionDataManager);
            view.DataContext = model;

            bool? res = view.ShowDialog();

            if (res.HasValue && res.Value)
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