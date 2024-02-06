using System.ComponentModel;
using WDE.Updater.Data;
using WDE.Updater.Services;

namespace CrashReport;

public class SettingsProvider : IUpdaterSettingsProvider
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public UpdaterSettings Settings { get; set; } = new UpdaterSettings()
    {
        DisableAutoUpdates = false,
        EnableSilentUpdates = true,
        EnableReadyToInstallPopup = true,
        LastCheckedForUpdates = default,
        LastShowedChangelog = default
    };
}