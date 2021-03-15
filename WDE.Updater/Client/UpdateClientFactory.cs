using System;
using WDE.Module.Attributes;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    [AutoRegister]
    public class UpdateClientFactory : IUpdateClientFactory
    {
        public IUpdateClient Create(Uri updateServerUrl, string marketplace, string? key, Platforms platform)
        {
            return new UpdateClient(updateServerUrl, marketplace, key, platform);
        }
    }
}