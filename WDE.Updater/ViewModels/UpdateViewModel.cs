using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
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

        public UpdateViewModel(IUpdateService updateService, ITaskRunner taskRunner, IStatusBar statusBar)
        {
            this.updateService = updateService;
            this.taskRunner = taskRunner;
            this.statusBar = statusBar;
            if (updateService.CanCheckForUpdates())
            { 
                taskRunner.ScheduleTask("Check for updates", UpdatesCheck);
            }
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
                    new AsyncAutoCommand(async () => await updateService.CloseForUpdate())));
            }
            catch (Exception e)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Error while checking for the updates: " + e.Message));
            }
        }
    }
}