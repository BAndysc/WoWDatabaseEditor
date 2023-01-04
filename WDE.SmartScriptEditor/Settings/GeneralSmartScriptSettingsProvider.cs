using System;
using System.ComponentModel;
using Newtonsoft.Json;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Settings;

[AutoRegisterToParentScope]
[SingleInstance]
public class GeneralSmartScriptSettingsProvider : IGeneralSmartScriptSettingsProvider
{
    public struct Data : ISettings
    {
        [DefaultValue(SmartScriptViewType.Compact)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public SmartScriptViewType ViewType;

        public AddingElementBehaviour AddingBehaviour;

        public ActionEditViewOrder ActionEditViewOrder;

        public bool InsertActionOnEventInsert;
        
        [DefaultValue(1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public float DefaultScale;
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

    public float DefaultScale
    {
        get => Math.Clamp(currentData.DefaultScale, 0.5f, 2f);
        set => currentData.DefaultScale = Math.Clamp(value, 0.5f, 2f);
    }

    public ActionEditViewOrder ActionEditViewOrder
    {
        get => currentData.ActionEditViewOrder;
        set => currentData.ActionEditViewOrder = value;
    }
    
    public bool InsertActionOnEventInsert
    {
        get => currentData.InsertActionOnEventInsert;
        set => currentData.InsertActionOnEventInsert = value;
    }
    
    public void Apply()
    {
        Save();
    }
}