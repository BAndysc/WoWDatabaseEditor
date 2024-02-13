using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.Updater.Services;

namespace WDE.Updater.ViewModels
{
    [AutoRegister]
    [SingleInstance]
    public partial class UpdateViewModel : BindableBase, IUpdateViewModel
    {
        private readonly IUpdateService updateService;
        private readonly ITaskRunner taskRunner;
        private readonly IStatusBar statusBar;
        private readonly IUpdaterSettingsProvider settingsProvider;
        private readonly IAutoUpdatePlatformService platformService;
        private readonly IFileSystem fileSystem;
        private readonly IMessageBoxService messageBoxService;

        public ICommand CheckForUpdatesCommand { get; }

        [Notify] private bool hasPendingUpdate;

        [Notify] private ICommand installPendingCommandUpdate;

        public UpdateViewModel(IUpdateService updateService, 
            ITaskRunner taskRunner, 
            IStatusBar statusBar,
            IUpdaterSettingsProvider settingsProvider,
            IAutoUpdatePlatformService platformService,
            IFileSystem fileSystem,
            IStandaloneUpdater standaloneUpdater,
            IApplication application,
            IMainThread mainThread,
            IMessageBoxService messageBoxService)
        {
            this.updateService = updateService;
            this.taskRunner = taskRunner;
            this.statusBar = statusBar;
            this.settingsProvider = settingsProvider;
            this.platformService = platformService;
            this.fileSystem = fileSystem;
            this.messageBoxService = messageBoxService;
            installPendingCommandUpdate = AlwaysDisabledCommand.Command;

            CheckForUpdatesCommand = new DelegateCommand(() =>
            {
                if (updateService.CanCheckForUpdates())
                    taskRunner.ScheduleTask(new UpdateTask(this, true));
            }, updateService.CanCheckForUpdates);

            if (!settingsProvider.Settings.DisableAutoUpdates &&
                updateService.CanCheckForUpdates())
            {
                CheckForUpdatesCommand.Execute(null);
                mainThread.StartTimer(() =>
                {
                    taskRunner.ScheduleTask(new UpdateTask(this, false));
                    return true;
                }, TimeSpan.FromHours(1));
            }
        }
        
        private class UpdateTask : IAsyncTask
        {
            private readonly UpdateViewModel parent;
            private readonly bool initialCheck;
            public string Name => "Check for updates";
            
            public bool WaitForOtherTasks => false;

            public UpdateTask(UpdateViewModel parent, bool initialCheck)
            {
                this.parent = parent;
                this.initialCheck = initialCheck;
            }
            
            public Task Run(ITaskProgress progress) => parent.UpdatesCheck(initialCheck);
        }

        private async Task UpdatesCheck(bool initialCheck)
        {
            try
            {
                string? newUpdateUrl = await updateService.CheckForUpdates(false);
                if (newUpdateUrl != null)
                {
                    if (settingsProvider.Settings.EnableSilentUpdates)
                    {
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info,"Downloading update..."));
                        ScheduleDownloadUpdate(settingsProvider.Settings.EnableReadyToInstallPopup);
                    }
                    else
                    {
                        Task<bool> AskToDownload()
                        {
                            return messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                .SetTitle("New update")
                                .SetMainInstruction("A new update is ready to be downloaded")
                                .SetContent("Do you want to download the update now?")
                                .WithYesButton(true)
                                .WithNoButton(false)
                                .SetIcon(MessageBoxIcon.Information)
                                .Build());
                        }

                        if (initialCheck && await AskToDownload())
                        {
                            ScheduleDownloadUpdate(settingsProvider.Settings.EnableReadyToInstallPopup);
                        }
                        else
                        {
                            HasPendingUpdate = true;
                            InstallPendingCommandUpdate = new AsyncAutoCommand(async () =>
                            {
                                if (await AskToDownload())
                                    await DownloadUpdate(true);
                            });
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Info, 
                                "New updates are ready to download. Click to download.",
                                new DelegateCommand(() => DownloadUpdate(true))));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }

        private void ScheduleDownloadUpdate(bool askToRestartOrOnlyNotification)
        {
            DownloadUpdate(askToRestartOrOnlyNotification).ListenErrors();
        }

        private Task DownloadUpdate(bool askToRestartOrOnlyNotification)
        {
            statusBar.PublishNotification(
                new PlainNotification(NotificationType.Info, "Downloading..."));
            return taskRunner.ScheduleTask(new DownloadUpdateTask(this, askToRestartOrOnlyNotification));
        }
        
        private class DownloadUpdateTask : IAsyncTask
        {
            private readonly UpdateViewModel parent;
            private readonly bool askToRestartOrOnlyNotification;
            public string Name => "Downloading update";
            
            public bool WaitForOtherTasks => false;

            public DownloadUpdateTask(UpdateViewModel parent, bool askToRestartOrOnlyNotification)
            {
                this.parent = parent;
                this.askToRestartOrOnlyNotification = askToRestartOrOnlyNotification;
            }
            
            public Task Run(ITaskProgress progress) => parent.DownloadUpdateTaskImpl(progress, askToRestartOrOnlyNotification);
        }
        
        private async Task DownloadUpdateTaskImpl(ITaskProgress taskProgress, bool askToRestartOrOnlyNotification)
        {
            try
            {
                if (!await updateService.DownloadLatestVersion(taskProgress))
                {
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Warning,
                        "Failed to download the update, update file got corrupted. Please try later."));
                    return;
                }

                var restartCommand = new AsyncAutoCommand(async () =>
                {
                    if (platformService.PlatformSupportsSelfInstall)
                        await updateService.CloseForUpdate();
                    else
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Your platform doesn't support self updates")
                            .SetContent(
                                "Sadly, self updater is not available on your operating system yet.\n\nA new version of WoW Database Editor has been downloaded, but you have to manually copy new version to the Applications folder")
                            .Build());
                        var physPath = fileSystem.ResolvePhysicalPath(platformService.UpdateZipFilePath);

                        using Process open = new Process
                        {
                            StartInfo =
                            {
                                FileName = "open",
                                Arguments = "-R " + physPath.FullName,
                                UseShellExecute = true
                            }
                        };
                        open.Start();
                    }
                });

                IAsyncCommand askToRestartCommand = new AsyncAutoCommand(async () =>
                {
                    if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Update ready to install")
                            .SetMainInstruction("Do you want to restart the editor now to install the update?")
                            .SetContent(
                                "The update has been downloaded and is ready to be installed. If you select 'YES' the editor will restart to install the update. If you select 'NO' the editor will update upon the next editor start.")
                            .WithYesButton(true)
                            .WithNoButton(false)
                            .Build()))
                    {
                        await restartCommand.ExecuteAsync();
                    }
                });

                InstallPendingCommandUpdate = new AsyncAutoCommand(async () =>
                {
                    // check for EnableReadyToInstallPopup?
                    // idk, better always ask here, because the user has pressed the button and he might not know what it does
                    await askToRestartCommand.ExecuteAsync();
                });
                HasPendingUpdate = true;
                if (askToRestartOrOnlyNotification)
                {
                    await askToRestartCommand.ExecuteAsync();
                }
                else
                {
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Info,
                        "Update ready to install. It will be installed on the next editor restart or when you click here",
                        restartCommand));
                }
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }
    }
}