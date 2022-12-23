using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Services;

namespace WDE.TrinitySmartScriptEditor.Services;

[AutoRegister]
[SingleInstance]
public class TrinitySmartHighlighter : SmartHighlighter
{
    public TrinitySmartHighlighter(ISmartDataManager smartDataManager, IConditionDataManager conditionDataManager) : base(smartDataManager, conditionDataManager)
    {
    }
}