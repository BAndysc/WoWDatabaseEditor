using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[UniqueProvider]
public interface IGeneralSmartScriptSettingsProvider
{
    SmartScriptViewType ViewType { get; set; }
    AddingElementBehaviour AddingBehaviour { get; set; }
    ActionEditViewOrder ActionEditViewOrder { get; set; }
    bool InsertActionOnEventInsert { get; set; }
    bool AutomaticallyApplyNonRepeatableFlag { get; set; }
    float DefaultScale { get; set; }
    void Apply();
}