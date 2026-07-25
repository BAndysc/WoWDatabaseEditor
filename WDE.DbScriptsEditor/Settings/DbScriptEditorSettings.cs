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
        void Apply();
    }

    [AutoRegister]
    [SingleInstance]
    public class DbScriptEditorSettings : IDbScriptEditorSettings
    {
        private struct Data : ISettings
        {
            public DbScriptAddFlow AddFlow;
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

        public void Apply() => userSettings.Update(currentData);
    }
}
