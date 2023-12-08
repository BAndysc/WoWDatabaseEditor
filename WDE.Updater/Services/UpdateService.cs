using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.Updater.Client;
using WDE.Updater.Data;
using WDE.Updater.Models;

namespace WDE.Updater.Services
{
    public interface IUpdateService
    {
        bool CanCheckForUpdates();
        Task<string?> CheckForUpdates(bool allowRedownload);
        Task<bool> DownloadLatestVersion(ITaskProgress taskProgress);
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
        private readonly IUpdaterSettingsProvider settings;
        private readonly IAutoUpdatePlatformService platformService;
        private readonly IUpdateVerifier updateVerifier;
        private IUpdateClient? updateClient;
        private CheckVersionResponse? cachedResponse;

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
            IStandaloneUpdater standaloneUpdater,
            IUpdaterSettingsProvider settings,
            IAutoUpdatePlatformService platformService,
            IUpdateVerifier updateVerifier)
        {
            this.data = data;
            this.clientFactory = clientFactory;
            this.applicationVersion = applicationVersion;
            this.application = application;
            this.fileSystem = fileSystem;
            this.standaloneUpdater = standaloneUpdater;
            this.settings = settings;
            this.platformService = platformService;
            this.updateVerifier = updateVerifier;
            standaloneUpdater.RenameIfNeeded();
        }

        public bool CanCheckForUpdates() => data.HasUpdateServerData && applicationVersion.VersionKnown;

        private async Task<CheckVersionResponse?> InternalCheckForUpdates(bool allowRedownload)
        {
            var response = await UpdateClient.CheckForUpdates(applicationVersion.Branch, applicationVersion.BuildVersion);

            if (response.LatestVersion > applicationVersion.BuildVersion ||
                allowRedownload)
                return response;

            return null;
        }
        
        public async Task<string?> CheckForUpdates(bool allowRedownload)
        {
            cachedResponse = await InternalCheckForUpdates(allowRedownload);
            var s = settings.Settings;
            s.LastCheckedForUpdates = DateTime.Now;
            settings.Settings = s;
            return cachedResponse?.DownloadUrl;
        }

        public async Task<bool> DownloadLatestVersion(ITaskProgress taskProgress)
        {
            cachedResponse ??= await InternalCheckForUpdates(false);
            
            if (cachedResponse != null)
            {
                var physPath = fileSystem.ResolvePhysicalPath(platformService.UpdateZipFilePath);
                bool success = false;
                int trial = 0;
                do
                {
                    trial++;
                    success = true;
                    await UpdateClient.DownloadUpdate(cachedResponse, physPath.FullName, taskProgress.ToProgress());
                    if (!string.IsNullOrEmpty(cachedResponse.DownloadMd5))
                    {
                        success = await updateVerifier.IsUpdateValid((FileInfo)physPath, cachedResponse.DownloadMd5);
                    }
                } while (!success && trial < 3);

                if (!success)
                {
                    physPath.Delete();
                    taskProgress.ReportFinished();
                    return false;
                }
                
                if (cachedResponse.ChangeLog?.Length > 0)
                    fileSystem.WriteAllText("~/changelog.json", JsonConvert.SerializeObject(cachedResponse.ChangeLog));
                
                taskProgress.ReportFinished();
                return true;
            }
            
            taskProgress.ReportFinished();
            return false;
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
