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
    }
}