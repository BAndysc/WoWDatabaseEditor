using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[UniqueProvider]
public interface IGeneralSmartScriptSettingsProvider
{
    SmartScriptViewType ViewType { get; set; }
    AddingElementBehaviour AddingBehaviour { get; set; }
    float DefaultScale { get; set; }
    void Apply();
}