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
            IStandaloneUpdater standaloneUpdater)
        {
            this.data = data;
            this.clientFactory = clientFactory;
            this.applicationVersion = applicationVersion;
            this.application = application;
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

            var progress = new Progress<float>((v) => taskProgress.Report((int) (v * 1000), 1000, "downloading"));

            if (response != null)
            {
                await UpdateClient.DownloadUpdate(response, "update.zip", progress);

                if (response.ChangeLog?.Length > 0)
                {
                    await File.WriteAllTextAsync("~/changelog.json", JsonConvert.SerializeObject(response.ChangeLog));
                }
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