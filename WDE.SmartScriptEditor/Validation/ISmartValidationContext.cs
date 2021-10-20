using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Validation
{
    public interface ISmartValidationContext
    {
        public SmartScriptType ScriptType { get; }
        public bool HasAction { get; }
        public int EventParametersCount { get; }
        public int ActionParametersCount { get; }
        public int ActionSourceParametersCount { get; }
        public int ActionTargetParametersCount { get; }
        
        public long GetEventParameter(int index);
        public long GetActionParameter(int index);
        public long GetActionSourceParameter(int index);
        public long GetActionTargetParameter(int index);
        public long GetTargetType();
    }
}