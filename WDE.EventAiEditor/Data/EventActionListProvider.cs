using System;
using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.EventAiEditor.Editor.ViewModels;
using WDE.EventAiEditor.Services;

namespace WDE.EventAiEditor.Data
{
    [SingleInstance]
    [AutoRegister]
    public class EventActionListProvider : IEventActionListProvider
    {
        private readonly IWindowManager windowManager;
        private readonly IEventAiDataManager eventAiDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly IFavouriteEventAiService favouriteEventAiService;

        public EventActionListProvider(IWindowManager windowManager,
            IEventAiDataManager eventAiDataManager, 
            IConditionDataManager conditionDataManager,
            IFavouriteEventAiService favouriteEventAiService)
        {
            this.windowManager = windowManager;
            this.eventAiDataManager = eventAiDataManager;
            this.conditionDataManager = conditionDataManager;
            this.favouriteEventAiService = favouriteEventAiService;
        }

        public async System.Threading.Tasks.Task<(uint, bool)?> Get(EventOrAction type, Func<EventActionGenericJsonData, bool> predicate, List<(uint, string)>? customItems)
        {
            var title = GetTitleForType(type);
            EventAiSelectViewModel model = new(title, type, predicate, customItems, eventAiDataManager, conditionDataManager, favouriteEventAiService);

            if (await windowManager.ShowDialog(model) && model.SelectedItem != null)
            {
                if (model.SelectedItem.CustomId.HasValue)
                    return (model.SelectedItem.CustomId.Value, true);
                return (model.SelectedItem.Id, false);
            }

            return null;
        }

        private string GetTitleForType(EventOrAction type)
        {
            switch (type)
            {
                case EventOrAction.Event:
                    return "Pick event";
                case EventOrAction.Action:
                    return "Pick action";
                default:
                    return "Pick";
            }
        }
    }
}