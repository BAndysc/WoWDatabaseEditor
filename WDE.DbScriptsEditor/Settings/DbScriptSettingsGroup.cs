using System.Collections.Generic;
using Prism.Commands;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Settings;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Editor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Settings
{
    // The "DB scripts editor" group on the General settings page.
    [AutoRegister]
    public class DbScriptSettingsGroup : IGeneralSettingsGroup
    {
        private readonly IDbScriptEditorSettings settings;
        private readonly ListOptionGenericSetting addFlow;
        private readonly BoolGenericSetting loneTargetAsSource;

        public string Name => "DB scripts editor";
        public IReadOnlyList<IGenericSetting> Settings { get; }

        public DbScriptSettingsGroup(IDbScriptEditorSettings settings,
            IContainerProvider containerProvider,
            IWindowManager windowManager)
        {
            this.settings = settings;

            addFlow = new ListOptionGenericSetting("Add action behaviour",
                new object[] { DbScriptAddFlow.Wizard, DbScriptAddFlow.ActionFirst },
                settings.AddFlow,
                "Wizard asks for the source, then an action compatible with it, then the target, and finally opens the parameters dialog. ActionFirst asks only for the action and opens the parameters dialog directly — the source and target can be changed there.");

            loneTargetAsSource = new BoolGenericSetting("Show lone target as source",
                settings.PresentLoneTargetAsSource,
                "Commands that use only a target (e.g. Despawn gameobject, Set gossip menu) present that single actor as the \"source\" in the editor, which reads more naturally. The database row still stores it as the target. Untick to always show the raw source/target split.");

            var allSettings = new List<IGenericSetting> { addFlow, loneTargetAsSource };
#if DEBUG
            allSettings.Add(new ButtonGenericSetting("Roundtrip test (debug)", "Run",
                new DelegateCommand(() =>
                    windowManager.ShowWindow(containerProvider.Resolve<DbScriptRoundtripTestViewModel>(), out _)),
                "Loads every dbscript through the editor model and compares the regenerated SQL with the original rows (comments excluded)."));
#endif
            Settings = allSettings;
        }

        public void Save()
        {
            settings.AddFlow = (DbScriptAddFlow)addFlow.SelectedOption;
            settings.PresentLoneTargetAsSource = loneTargetAsSource.Value;
            settings.Apply();
        }
    }
}
