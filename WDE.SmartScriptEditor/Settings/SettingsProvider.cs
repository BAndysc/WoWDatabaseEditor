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
    private ListOptionGenericSetting actionEditViewOrder;
    private BoolGenericSetting insertActionOnEventInsert;
    private BoolGenericSetting automaticallyApplyNonRepeatableFlag;
    private FloatSliderGenericSetting defaultScale;
    
    public void Save()
    {
        smartSettingsProvider.AddingBehaviour = (AddingElementBehaviour)addingBehaviour.SelectedOption;
        smartSettingsProvider.ViewType = (SmartScriptViewType)viewType.SelectedOption;
        smartSettingsProvider.ActionEditViewOrder = (ActionEditViewOrder)actionEditViewOrder.SelectedOption;
        smartSettingsProvider.DefaultScale = defaultScale.Value;
        smartSettingsProvider.InsertActionOnEventInsert = insertActionOnEventInsert.Value;
        smartSettingsProvider.AutomaticallyApplyNonRepeatableFlag = automaticallyApplyNonRepeatableFlag.Value;
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
            smartSettingsProvider.ViewType, "Compact mode takes less space, but to add an action you have to use the context menu instead of having a button.");

        addingBehaviour = new ListOptionGenericSetting("Add behaviour",
            new object[]
            {
                AddingElementBehaviour.Wizard, AddingElementBehaviour.DirectlyOpenDialog, AddingElementBehaviour.JustAdd
            },
            smartSettingsProvider.AddingBehaviour, "Controls the way how new actions are added. The wizard shows three dialogs with all possible actions, sources and types, the 'directly open dialog' adds a default action and instantly opens the action edit dialog. 'Just add' only adds an action.");

        actionEditViewOrder = new ListOptionGenericSetting("Action edit view order",
            new object[]
            {
                ActionEditViewOrder.SourceActionTarget, ActionEditViewOrder.ActionSourceTarget
            },
            smartSettingsProvider.ActionEditViewOrder, "Decide how the order in the action edit dialog and the order of selecting elements in wizard add behaviour mode.");

        insertActionOnEventInsert = new BoolGenericSetting("Insert a new action on event insert",
            smartSettingsProvider.InsertActionOnEventInsert, "When enabled, a new action will be inserted when you insert a new event.");
        
        automaticallyApplyNonRepeatableFlag = new BoolGenericSetting("Automatically apply non-repeatable flag",
            smartSettingsProvider.AutomaticallyApplyNonRepeatableFlag, "When enabled, non-repeatable flag will be automatically applied and removed from events depending on their timers.");
        
        defaultScale = new FloatSliderGenericSetting("Default scaling",
            smartSettingsProvider.DefaultScale,
            0.5f,
            1f);
        
        Settings = new List<IGenericSetting>()
        {
            viewType,
            addingBehaviour,
            actionEditViewOrder,
            insertActionOnEventInsert,
            automaticallyApplyNonRepeatableFlag,
            defaultScale
        };
    }
}