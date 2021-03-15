using System.Diagnostics.CodeAnalysis;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.Updater.Data;

namespace WDE.Updater.Services
{
    [SingleInstance]
    [AutoRegister]
    [ExcludeFromCodeCoverage]
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
            set => userSettings.Update(value);
        }
    }
}