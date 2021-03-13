using WDE.Common.Services;

namespace WDE.Updater.Data
{
    public struct UpdaterSettings : ISettings
    {
        public bool DisableAutoUpdates { get; set; }
        public long LastShowedChangelog { get; set; }
    }
}