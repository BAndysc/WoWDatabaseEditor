using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.Updater.Client;
using WDE.Updater.Data;
using WDE.Updater.Models;

namespace WDE.Updater.Services
{
    public interface IUpdateService
    {
        bool CanCheckForUpdates();
        Task<string?> CheckForUpdates();
        Task DownloadLatestVersion(ITaskProgress taskProgress);
        Task CloseForUpdate();
    }
    
    [AutoRegister]
    [SingleInstance]
    public class UpdateService : IUpdateService
    {
        private readonly IUpdateServerDataProvider data;
        private readonly IUpdateClientFactory clientFactory;
        private readonly IApplicationVersion applicationVersion;
        private readonly IApplication application;
        private readonly IFileSystem fileSystem;
        private readonly IStandaloneUpdater standaloneUpdater;
        private IUpdateClient? updateClient;

        private IUpdateClient UpdateClient
        {
            get
            {
                if (!CanCheckForUpdates())
                    throw new Exception();
                
                if (updateClient == null)
                    updateClient = clientFactory.Create(data.UpdateServerUrl, data.Marketplace, data.UpdateKey, data.Platform);

                return updateClient;
            }
        }

        public UpdateService(IUpdateServerDataProvider data, 
            IUpdateClientFactory clientFactory,
            IApplicationVersion applicationVersion,
            IApplication application,
            IFileSystem fileSystem,
            IStandaloneUpdater standaloneUpdater)
        {
            this.data = data;
            this.clientFactory = clientFactory;
            this.applicationVersion = applicationVersion;
            this.application = application;
            this.fileSystem = fileSystem;
            this.standaloneUpdater = standaloneUpdater;
            standaloneUpdater.RenameIfNeeded();
        }

        public bool CanCheckForUpdates() => data.HasUpdateServerData && applicationVersion.VersionKnown;

        private async Task<CheckVersionResponse?> InternalCheckForUpdates()
        {
            var response = await UpdateClient.CheckForUpdates(applicationVersion.Branch, applicationVersion.BuildVersion);

            if (response.LatestVersion > applicationVersion.BuildVersion)
                return response;

            return null;
        }
        
        public async Task<string?> CheckForUpdates()
        {
            var response = await InternalCheckForUpdates();
            return response?.DownloadUrl;
        }

        public async Task DownloadLatestVersion(ITaskProgress taskProgress)
        {
            var response = await InternalCheckForUpdates();
            
            if (response != null)
            {
                var progress = new Progress<(long downloaded, long? totalBytes)>((v) =>
                {
                    var isDownloaded = (v.totalBytes.HasValue && v.totalBytes.Value == v.downloaded) ||
                                       v.downloaded == -1;
                    var isStatusKnown = v.totalBytes.HasValue;
                    var currentProgress = v.totalBytes.HasValue ? (int) v.downloaded : (v.downloaded < 0 ? 1 : 0);
                    var maxProgress = v.totalBytes ?? 1;
                    
                    if (taskProgress.State == TaskState.InProgress)
                    {
                        taskProgress.Report(currentProgress, (int)maxProgress, isDownloaded ? 
                            "finished" : 
                            (isStatusKnown ? $"{v.downloaded / 1_000_000f:0.00}/{maxProgress / 1_000_000f:0.00}MB" : $"{v.downloaded / 1_000_000f:0.00}MB"));                        
                    }
                });
                await UpdateClient.DownloadUpdate(response, "update.zip", progress);

                if (response.ChangeLog?.Length > 0)
                    fileSystem.WriteAllText("~/changelog.json", JsonConvert.SerializeObject(response.ChangeLog));
            }
            
            taskProgress.ReportFinished();
        }

        public async Task CloseForUpdate()
        {
            if (!await application.CanClose())
                return;

            standaloneUpdater.Launch();

            application.ForceClose();
        }
    }
}
