using System;
using System.Diagnostics.CodeAnalysis;
using WDE.Common.Services;

namespace WDE.Updater.Data
{
    [ExcludeFromCodeCoverage]
    public struct UpdaterSettings : ISettings
    {
        public bool DisableAutoUpdates { get; set; }
        public long LastShowedChangelog { get; set; }
        public DateTime LastCheckedForUpdates { get; set; }
    }
}