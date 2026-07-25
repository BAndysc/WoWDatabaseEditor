using System.Collections.Generic;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[AutoRegisterToParentScope]
[SingleInstance]
public class SmartScriptAnalyticsSessionProps : IAnalyticsSessionPropsProvider
{
    private readonly IGeneralSmartScriptSettingsProvider settings;

    public SmartScriptAnalyticsSessionProps(IGeneralSmartScriptSettingsProvider settings)
    {
        this.settings = settings;
    }

    public IEnumerable<(string key, object value)> GetSessionProps()
    {
        yield return ("smart_adding_behaviour", settings.AddingBehaviour.ToString());
        yield return ("smart_view_type", settings.ViewType.ToString());
    }
}
