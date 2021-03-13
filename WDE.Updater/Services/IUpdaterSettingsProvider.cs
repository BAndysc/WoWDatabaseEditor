using WDE.Updater.Data;

namespace WDE.Updater.Services
{
    public interface IUpdaterSettingsProvider
    {
        UpdaterSettings Settings { get; set; }
    }
}