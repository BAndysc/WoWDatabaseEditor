using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
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
    public class UpdateViewModel : BindableBase
    {
        private readonly IUpdateService updateService;
        private readonly ITaskRunner taskRunner;
        private readonly IStatusBar statusBar;
        private readonly IUpdaterSettingsProvider settingsProvider;
        private readonly IAutoUpdatePlatformService platformService;
        private readonly IFileSystem fileSystem;
        private readonly IMessageBoxService messageBoxService;

        public ICommand CheckForUpdatesCommand { get; }
        
        public UpdateViewModel(IUpdateService updateService, 
            ITaskRunner taskRunner, 
            IStatusBar statusBar,
            IUpdaterSettingsProvider settingsProvider,
            IAutoUpdatePlatformService platformService,
            IFileSystem fileSystem,
            IMessageBoxService messageBoxService)
        {
            this.updateService = updateService;
            this.taskRunner = taskRunner;
            this.statusBar = statusBar;
            this.settingsProvider = settingsProvider;
            this.platformService = platformService;
            this.fileSystem = fileSystem;
            this.messageBoxService = messageBoxService;

            CheckForUpdatesCommand = new DelegateCommand(() =>
            {
                if (updateService.CanCheckForUpdates())
                    taskRunner.ScheduleTask("Check for updates", UpdatesCheck);
            }, updateService.CanCheckForUpdates);
            
            if (!settingsProvider.Settings.DisableAutoUpdates)
                CheckForUpdatesCommand.Execute(null);
        }
        
        private async Task UpdatesCheck()
        {
            try
            {
                string? newUpdateUrl = await updateService.CheckForUpdates();
                if (newUpdateUrl != null)
                {
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Info, 
                        "New updates are ready to download. Click to download.",
                        new DelegateCommand(() =>
                        {
                            statusBar.PublishNotification(
                                new PlainNotification(NotificationType.Info, "Downloading..."));
                            DownloadUpdate();
                        })));
                }
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }

        private void DownloadUpdate()
        {
            taskRunner.ScheduleTask("Downloading update", DownloadUpdateTask);
        }

        private async Task DownloadUpdateTask(ITaskProgress taskProgress)
        {
            try
            {
                await updateService.DownloadLatestVersion(taskProgress);
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, 
                    "Update ready to install. Click here to install",
                    new AsyncAutoCommand(async () =>
                    {
                        if (platformService.PlatformSupportsSelfInstall)
                            await updateService.CloseForUpdate();
                        else
                        {
                            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                .SetTitle("Your platform doesn't support self updates")
                                .SetContent("Sadly, self updater is not available on your operating system yet.\n\nA new version of WoW Database Editor has been downloaded, but you have to manually copy new version to the Applications folder")
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
                    })));
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }
    }
}