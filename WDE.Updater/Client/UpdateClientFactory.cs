using System;
using WDE.Common.Factories;
using WDE.Module.Attributes;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    [AutoRegister]
    public class UpdateClientFactory : IUpdateClientFactory
    {
        private readonly IHttpClientFactory httpClientFactory;

        public UpdateClientFactory(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }
        
        public IUpdateClient Create(Uri updateServerUrl, string marketplace, string? key, UpdatePlatforms platform)
        {
            var httpClient = httpClientFactory.Factory();
            return new UpdateClient(updateServerUrl, marketplace, key, platform, httpClient);
        }
    }
}