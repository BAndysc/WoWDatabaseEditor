using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [SingleInstance]
    [AutoRegister]
    public class CurrentCoreSettings : ICurrentCoreSettings
    {
        private readonly IUserSettings userSettings;
        
        public string? CurrentCore { get; }
        
        public CurrentCoreSettings(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            CurrentCore = userSettings.Get<Data>().Version;
        }
        
        public void UpdateCore(ICoreVersion core)
        {
            userSettings.Update(new Data() {Version = core.Tag});
        }

        private struct Data : ISettings
        {
            public string Version { get; set; }
        }
    }
}