using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    [AutoRegister]
    public class UpdateClientFactory : IUpdateClientFactory
    {
        private readonly IApplicationVersion applicationVersion;

        public UpdateClientFactory(IApplicationVersion applicationVersion)
        {
            this.applicationVersion = applicationVersion;
        }
        
        public IUpdateClient Create(Uri updateServerUrl, string marketplace, string? key, Platforms platform)
        {
            var httpClient = new HttpClient();
            var buildVersion = applicationVersion.VersionKnown ? applicationVersion.BuildVersion : -1;
            var branch = applicationVersion.VersionKnown ? applicationVersion.Branch! : "(unknown)";
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"WoWDatabaseEditor/{buildVersion} (branch: {branch}, marketplace: {marketplace}, platform: {platform.ToString()})");
            return new UpdateClient(updateServerUrl, marketplace, key, platform, httpClient);
        }
    }
}