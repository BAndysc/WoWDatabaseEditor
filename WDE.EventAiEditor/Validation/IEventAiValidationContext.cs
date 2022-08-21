using WDE.Common.Database;

namespace WDE.EventAiEditor.Validation
{
    public interface IEventAiValidationContext
    {
        public bool HasAction { get; }
        public int EventParametersCount { get; }
        public int ActionParametersCount { get; }
        public long GetEventFlags();
        public long GetEventParameter(int index);
        public long GetActionParameter(int index);
    }
}