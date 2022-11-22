using System.Collections.Generic;
using WDE.Common.Settings;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[AutoRegisterToParentScope]
public class SettingsProvider : IGeneralSettingsGroup
{
    private readonly IGeneralSmartScriptSettingsProvider smartSettingsProvider;
    public string Name => "General Smart scripts";
    public IReadOnlyList<IGenericSetting> Settings { get; }
    
    private ListOptionGenericSetting viewType;
    private ListOptionGenericSetting addingBehaviour;

    public void Save()
    {
        smartSettingsProvider.AddingBehaviour = (AddingElementBehaviour)addingBehaviour.SelectedOption;
        smartSettingsProvider.ViewType = (SmartScriptViewType)viewType.SelectedOption;
        smartSettingsProvider.Apply();
    }

    public SettingsProvider(IGeneralSmartScriptSettingsProvider smartSettingsProvider)
    {
        this.smartSettingsProvider = smartSettingsProvider;

        viewType = new ListOptionGenericSetting("Script view type",
            new object[]
            {
                SmartScriptViewType.Normal, SmartScriptViewType.Compact
            },
            smartSettingsProvider.ViewType, null);

        addingBehaviour = new ListOptionGenericSetting("Add behaviour",
            new object[]
            {
                AddingElementBehaviour.Wizard, AddingElementBehaviour.DirectlyOpenDialog, AddingElementBehaviour.JustAdd
            },
            smartSettingsProvider.AddingBehaviour, null);
        
        Settings = new List<IGenericSetting>()
        {
            viewType,
            addingBehaviour
        };
    }
}