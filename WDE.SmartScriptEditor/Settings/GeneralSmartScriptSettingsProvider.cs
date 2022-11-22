using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[AutoRegisterToParentScope]
[SingleInstance]
public class GeneralSmartScriptSettingsProvider : IGeneralSmartScriptSettingsProvider
{
    public struct Data : ISettings
    {
        public SmartScriptViewType ViewType;
        public AddingElementBehaviour AddingBehaviour;
    }

    private Data currentData;
    
    private readonly IUserSettings userSettings;

    public GeneralSmartScriptSettingsProvider(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        currentData = userSettings.Get<Data>(new Data(){});
    }

    private void Save()
    {
        userSettings.Update(currentData);
    }
    
    public SmartScriptViewType ViewType
    {
        get => currentData.ViewType;
        set => currentData.ViewType = value;
    }

    public AddingElementBehaviour AddingBehaviour
    {
        get => currentData.AddingBehaviour;
        set => currentData.AddingBehaviour = value;
    }

    public void Apply()
    {
        Save();
    }
}