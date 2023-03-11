using System;
using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Services;

namespace WDE.SmartScriptEditor.Data
{
    [SingleInstance]
    [AutoRegister]
    public class SmartTypeListProvider : ISmartTypeListProvider
    {
        private readonly IWindowManager windowManager;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly IFavouriteSmartsService favouriteSmartsService;

        public SmartTypeListProvider(IWindowManager windowManager,
            ISmartDataManager smartDataManager, 
            IConditionDataManager conditionDataManager,
            IFavouriteSmartsService favouriteSmartsService)
        {
            this.windowManager = windowManager;
            this.smartDataManager = smartDataManager;
            this.conditionDataManager = conditionDataManager;
            this.favouriteSmartsService = favouriteSmartsService;
        }

        public async System.Threading.Tasks.Task<(int, bool)?> Get(SmartType type, Func<SmartGenericJsonData, bool> predicate, List<(int, string)>? customItems)
        {
            var title = GetTitleForType(type);
            using SmartSelectViewModel model = new(title, type, predicate, customItems, smartDataManager, conditionDataManager, favouriteSmartsService);

            if (await windowManager.ShowDialog(model) && model.SelectedItem != null)
            {
                if (model.SelectedItem.CustomId.HasValue)
                    return (model.SelectedItem.CustomId.Value, true);
                return (model.SelectedItem.Id, false);
            }

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