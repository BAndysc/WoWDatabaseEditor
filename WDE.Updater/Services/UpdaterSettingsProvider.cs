using System.ComponentModel;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.Updater.Data;

namespace WDE.Updater.Services
{
    [SingleInstance]
    [AutoRegister]
    public class UpdaterSettingsProvider : IUpdaterSettingsProvider
    {
        private readonly IUserSettings userSettings;
        private UpdaterSettings settings;

        public UpdaterSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            settings = userSettings.Get<UpdaterSettings>(UpdaterSettings.Defaults);
        }

        public UpdaterSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                userSettings.Update(value); 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Settings)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}