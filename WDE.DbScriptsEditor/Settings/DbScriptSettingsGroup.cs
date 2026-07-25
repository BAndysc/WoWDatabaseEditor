using System.Collections.Generic;
using WDE.Common.Settings;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Settings
{
    // The "DB scripts editor" group on the General settings page.
    [AutoRegister]
    public class DbScriptSettingsGroup : IGeneralSettingsGroup
    {
        private readonly IDbScriptEditorSettings settings;
        private readonly ListOptionGenericSetting addFlow;

        public string Name => "DB scripts editor";
        public IReadOnlyList<IGenericSetting> Settings { get; }

        public DbScriptSettingsGroup(IDbScriptEditorSettings settings)
        {
            this.settings = settings;

            addFlow = new ListOptionGenericSetting("Add action behaviour",
                new object[] { DbScriptAddFlow.Wizard, DbScriptAddFlow.ActionFirst },
                settings.AddFlow,
                "Wizard asks for the source, then an action compatible with it, then the target, and finally opens the parameters dialog. ActionFirst asks only for the action and opens the parameters dialog directly — the source and target can be changed there.");

            Settings = new List<IGenericSetting> { addFlow };
        }

        public void Save()
        {
            settings.AddFlow = (DbScriptAddFlow)addFlow.SelectedOption;
            settings.Apply();
        }
    }
}
