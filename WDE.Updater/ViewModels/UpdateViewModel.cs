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
            IStandaloneUpdater standaloneUpdater,
            IApplication application,
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
                    taskRunner.ScheduleTask(new UpdateTask(this));
            }, updateService.CanCheckForUpdates);
            
            if (!settingsProvider.Settings.DisableAutoUpdates)
                CheckForUpdatesCommand.Execute(null);
        }
        
        private class UpdateTask : IAsyncTask
        {
            private readonly UpdateViewModel parent;
            public string Name => "Check for updates";
            
            public bool WaitForOtherTasks => false;

            public UpdateTask(UpdateViewModel parent)
            {
                this.parent = parent;
            }
            
            public Task Run(ITaskProgress progress) => parent.UpdatesCheck();
        }

        private async Task UpdatesCheck()
        {
            try
            {
                string? newUpdateUrl = await updateService.CheckForUpdates(false);
                if (newUpdateUrl != null)
                {
                    if (settingsProvider.Settings.EnableSilentUpdates)
                    {
                        DownloadUpdate();
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Info,"Downloading update..."));
                    }
                    else
                    {
                        if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("New update")
                            .SetMainInstruction("A new update is ready to be downloaded")
                            .SetContent("Do you want to download the update now?")
                            .WithYesButton(true)
                            .WithNoButton(false)
                            .SetIcon(MessageBoxIcon.Information)
                            .Build()))
                        {
                            DownloadUpdate();
                        }
                        else
                        {
                            statusBar.PublishNotification(new PlainNotification(NotificationType.Info, 
                                "New updates are ready to download. Click to download.",
                                new DelegateCommand(DownloadUpdate)));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }

        private void DownloadUpdate()
        {
            statusBar.PublishNotification(
                new PlainNotification(NotificationType.Info, "Downloading..."));
            taskRunner.ScheduleTask(new DownloadUpdateTask(this));
        }
        
        private class DownloadUpdateTask : IAsyncTask
        {
            private readonly UpdateViewModel parent;
            public string Name => "Downloading update";
            
            public bool WaitForOtherTasks => false;

            public DownloadUpdateTask(UpdateViewModel parent)
            {
                this.parent = parent;
            }
            
            public Task Run(ITaskProgress progress) => parent.DownloadUpdateTaskImpl(progress);
        }
        
        private async Task DownloadUpdateTaskImpl(ITaskProgress taskProgress)
        {
            try
            {
                if (!await updateService.DownloadLatestVersion(taskProgress))
                {
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Warning,
                        "Failed to download the update, update file got corrupted. Please try later."));
                    return;
                }
                statusBar.PublishNotification(new PlainNotification(NotificationType.Info, 
                    "Update ready to install. It will be installed on the next editor restart or when you click here",
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