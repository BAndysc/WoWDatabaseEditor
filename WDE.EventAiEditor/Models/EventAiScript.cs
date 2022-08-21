using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.EventAiEditor.Data;

namespace WDE.EventAiEditor.Models
{
    public class EventAiScript : EventAiBase
    {
        public EventAiScript(IEventAiSolutionItem item,
            IEventAiFactory eventAiFactory,
            IEventAiDataManager eventAiDataManager,
            IMessageBoxService messageBoxService) : base (eventAiFactory, eventAiDataManager, messageBoxService)
        {
            EntryOrGuid = item.EntryOrGuid;
        }

        public readonly int EntryOrGuid;
    }
}