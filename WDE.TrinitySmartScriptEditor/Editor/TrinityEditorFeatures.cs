using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor;

namespace WDE.TrinitySmartScriptEditor.Editor
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityEditorFeatures : IEditorFeatures
    {
        public string Name => "TC";
        public bool SupportsSource => false;
        public bool SupportsEventCooldown => false;
        public bool SupportsTargetCondition => false;
        public bool SupportsEventTimerId => false;
        public bool SourceHasPosition => false;
        public ParametersCount EventParametersCount { get; } = new ParametersCount(4, 0, 0);
        public ParametersCount ActionParametersCount { get; } = new ParametersCount(6, 0, 0);
        public ParametersCount TargetParametersCount { get; } = new ParametersCount(3, 4, 0);
    }
}