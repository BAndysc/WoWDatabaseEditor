using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Settings
{
    // How "Add action" behaves in the dbscript editor.
    public enum DbScriptAddFlow
    {
        // pick source → compatible action → target → parameters dialog (SmartScript wizard style)
        Wizard,
        // pick just the action → parameters dialog (source/target changed there)
        ActionFirst,
    }

    [UniqueProvider]
    public interface IDbScriptEditorSettings
    {
        DbScriptAddFlow AddFlow { get; set; }
        // Presentation-only: commands that use only a target (no source) show that single actor as
        // the "source" in the editor (the DB row still uses the target slot). Default on; opt-out.
        bool PresentLoneTargetAsSource { get; set; }
        void Apply();
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptEditorSettings : IDbScriptEditorSettings
    {
        private struct Data : ISettings
        {
            public DbScriptAddFlow AddFlow;
            // stored inverted so the serialized default (false) means the feature is ON
            public bool DisableLoneTargetAsSource;
        }

        private readonly IUserSettings userSettings;
        private Data currentData;

        public DbScriptEditorSettings(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            currentData = userSettings.Get<Data>();
        }

        public DbScriptAddFlow AddFlow
        {
            get => currentData.AddFlow;
            set => currentData.AddFlow = value;
        }

        public bool PresentLoneTargetAsSource
        {
            get => !currentData.DisableLoneTargetAsSource;
            set => currentData.DisableLoneTargetAsSource = !value;
        }

        public void Apply() => userSettings.Update(currentData);
    }
}
