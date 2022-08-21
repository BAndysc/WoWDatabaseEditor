using WDE.Common.Database;
using WDE.EventAiEditor.Validation;

namespace WDE.EventAiEditor.Models
{
    public class EventAiValidationContext : IEventAiValidationContext
    {
        private readonly EventAiBase script;
        private readonly EventAiEvent eventAiEvent;
        private readonly EventAiAction? action;

        public EventAiValidationContext(EventAiBase script, EventAiEvent eventAiEvent, EventAiAction? action)
        {
            this.script = script;
            this.eventAiEvent = eventAiEvent;
            this.action = action;
        }

        public bool HasAction => action != null;
        public int EventParametersCount => eventAiEvent.ParametersCount;
        public int ActionParametersCount => action?.ParametersCount ?? 0;
        public long GetEventFlags() => eventAiEvent.Flags.Value;
        public long GetEventParameter(int index) => eventAiEvent.GetParameter(index).Value;
        public long GetActionParameter(int index) => action?.GetParameter(index).Value ?? 0;
    }
}