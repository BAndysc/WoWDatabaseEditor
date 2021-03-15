using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.Updater.Services;

namespace WDE.Updater.ViewModels
{
    [AutoRegister]
    public class UpdaterConfigurationViewModel : ObservableBase, IConfigurable
    {
        private readonly IUpdaterSettingsProvider settings;

        public UpdaterConfigurationViewModel(IUpdaterSettingsProvider settings,
            IApplicationVersion applicationVersion,
            IChangelogProvider changelogProvider,
            UpdateViewModel updateViewModel)
        {
            this.settings = settings;
            Watch(settings, s => s.Settings, nameof(LastCheckForUpdates));
            Watch(this, t => t.DisableAutoUpdates, nameof(IsModified));

            ShowChangelog = new DelegateCommand(changelogProvider.TryShowChangelog, changelogProvider.HasChangelog);
            Save = new DelegateCommand(() =>
            {
                var sett = settings.Settings;
                sett.DisableAutoUpdates = DisableAutoUpdates;
                settings.Settings = sett;
                RaisePropertyChanged(nameof(IsModified));
            });
            DisableAutoUpdates = settings.Settings.DisableAutoUpdates;
            CheckForUpdatesCommand = updateViewModel.CheckForUpdatesCommand;
            CurrentVersion = applicationVersion.VersionKnown
                ? $"build {applicationVersion.BuildVersion}"
                : "-- not known --";
        }

        public DateTime LastCheckForUpdates => settings.Settings.LastCheckedForUpdates;
        public ICommand CheckForUpdatesCommand { get; }
        public ICommand ShowChangelog { get; }
        public string CurrentVersion { get; }
        
        private bool disableAutoUpdates;
        public bool DisableAutoUpdates
        {
            get => disableAutoUpdates;
            set => SetProperty(ref disableAutoUpdates, value);
        }
        
        public ICommand Save { get; }
        public string Name => "Editor updates";
        public string ShortDescription =>
            "WoW Database Editor can automatically check for updates. No personal data is sent during checking. You can change the behaviour or check for updates manually here.";        public bool IsModified => disableAutoUpdates != settings.Settings.DisableAutoUpdates;
        public bool IsRestartRequired => false;
    }
}