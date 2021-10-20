using WDE.Common.Database;
using WDE.SmartScriptEditor.Validation;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartValidationContext : ISmartValidationContext
    {
        private readonly SmartScriptBase script;
        private readonly SmartEvent @event;
        private readonly SmartAction? action;

        public SmartValidationContext(SmartScriptBase script, SmartEvent @event, SmartAction? action)
        {
            this.script = script;
            this.@event = @event;
            this.action = action;
        }

        public SmartScriptType ScriptType => script.SourceType;
        public bool HasAction => action != null;
        public int EventParametersCount => @event.ParametersCount;
        public int ActionParametersCount => action?.ParametersCount ?? 0;
        public int ActionSourceParametersCount => action?.Source.ParametersCount ?? 0;
        public int ActionTargetParametersCount => action?.Target.ParametersCount ?? 0;

        public long GetEventParameter(int index) => @event.GetParameter(index).Value;
        public long GetActionParameter(int index) => action?.GetParameter(index).Value ?? 0;
        public long GetActionSourceParameter(int index) => action?.Source.GetParameter(index).Value ?? 0;
        public long GetActionTargetParameter(int index) => action?.Target.GetParameter(index).Value ?? 0;
        public long GetTargetType() => action?.Target.Id ?? 0;
    }
}