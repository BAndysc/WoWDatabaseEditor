using System;
using System.IO;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.MangosEventAiEditor.Acid;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.MangosEventAiEditor.Settings
{
    [AutoRegisterToParentScope]
    public partial class CMangosConfigurationViewModel : ObservableBase, IConfigurable
    {
        private readonly ICMangosSettingsProvider settingsProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;

        [Notify] private string dbRepositoryPath = "";
        [Notify] private bool updateAcidFileOnSave;
        [Notify] private bool isModified;
        [Notify] private string acidFileStatus = "";

        public IAsyncCommand PickFolderCommand { get; }

        public CMangosConfigurationViewModel(ICMangosSettingsProvider settingsProvider,
            ICurrentCoreVersion currentCoreVersion,
            Lazy<IWindowManager> windowManager,
            Lazy<IMessageBoxService> messageBoxService)
        {
            this.settingsProvider = settingsProvider;
            this.currentCoreVersion = currentCoreVersion;

            dbRepositoryPath = settingsProvider.DbRepositoryPath ?? "";
            updateAcidFileOnSave = settingsProvider.UpdateAcidFileOnSave;

            PickFolderCommand = new AsyncCommand(async () =>
            {
                var result = await windowManager.Value.ShowFolderPickerDialog(
                    string.IsNullOrWhiteSpace(DbRepositoryPath) ? Environment.CurrentDirectory : DbRepositoryPath);
                if (result == null)
                    return;

                var repoRoot = AcidFileLocator.NormalizeRepositoryPath(result);
                if (repoRoot == null)
                {
                    await messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Not a cmangos -db repository")
                        .SetMainInstruction("Couldn't find the ACID file in the selected folder")
                        .SetContent($"Pick either the repository root (e.g. wotlk-db) or its ACID subfolder. Expected to find one of: {string.Join(", ", AcidFileLocator.KnownAcidFileNames)}.")
                        .WithOkButton(true).Build());
                    return;
                }

                DbRepositoryPath = repoRoot;
            });

            Save = new DelegateCommand(() =>
            {
                // a manually typed ACID subfolder path is normalized to the repository root too
                var normalized = AcidFileLocator.NormalizeRepositoryPath(DbRepositoryPath);
                if (normalized != null && normalized != DbRepositoryPath)
                    DbRepositoryPath = normalized;
                settingsProvider.DbRepositoryPath = string.IsNullOrWhiteSpace(DbRepositoryPath) ? null : DbRepositoryPath;
                settingsProvider.UpdateAcidFileOnSave = UpdateAcidFileOnSave;
                settingsProvider.Apply();
                IsModified = false;
            });

            On(() => DbRepositoryPath, _ =>
            {
                IsModified = true;
                UpdateAcidFileStatus();
            });
            On(() => UpdateAcidFileOnSave, _ => IsModified = true);
            UpdateAcidFileStatus();
            IsModified = false;
        }

        private void UpdateAcidFileStatus()
        {
            if (string.IsNullOrWhiteSpace(DbRepositoryPath))
                AcidFileStatus = "No repository path set, the ACID file will not be updated on save.";
            else if (!Directory.Exists(DbRepositoryPath))
                AcidFileStatus = "The directory doesn't exist.";
            else
            {
                var file = AcidFileLocator.Locate(DbRepositoryPath, currentCoreVersion.Current.Tag);
                AcidFileStatus = file == null
                    ? "No ACID .sql file found in the ACID subdirectory of this folder."
                    : $"Found: {file}";
            }
        }

        public ICommand Save { get; }
        public string Name => "CMaNGOS settings";
        public ImageUri Icon { get; } = new ImageUri("Icons/icon_cmangos.png");
        public string? ShortDescription => "Path to the cmangos -db repository (e.g. wotlk-db). When set, saving an EventAI script to the database also updates the creature_ai_scripts rows in the ACID .sql file, ready to be committed.";
        public bool IsRestartRequired => false;
        public ConfigurableGroup Group => ConfigurableGroup.Basic;
    }
}
