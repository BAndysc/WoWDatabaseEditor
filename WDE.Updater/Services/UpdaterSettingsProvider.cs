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

        public UpdaterSettingsProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
        }

        public UpdaterSettings Settings
        {
            get => userSettings.Get<UpdaterSettings>();
            set
            {
                userSettings.Update(value); 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Settings)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}