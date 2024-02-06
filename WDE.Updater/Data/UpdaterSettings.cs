using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Services;

namespace WDE.Updater.Data
{
    [ExcludeFromCodeCoverage]
    public struct UpdaterSettings : ISettings
    {
        public static UpdaterSettings Defaults => new UpdaterSettings()
        {
            DisableAutoUpdates = false,
            EnableSilentUpdates = false,
            EnableReadyToInstallPopup = true,
            LastCheckedForUpdates = default,
            LastShowedChangelog = default
        };

        public bool DisableAutoUpdates { get; set; }
        public bool EnableSilentUpdates { get; set; }
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool EnableReadyToInstallPopup { get; set; }
        public long LastShowedChangelog { get; set; }
        public DateTime LastCheckedForUpdates { get; set; }
    }
}