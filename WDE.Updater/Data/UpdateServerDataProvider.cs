using System;
using WDE.Common.Factories.Http;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.Updater.Models;

namespace WDE.Updater.Data
{
    [AutoRegister]
    [SingleInstance]
    public class UpdateServerDataProvider : IUpdateServerDataProvider, IUserAgentPart
    {
        public UpdateServerDataProvider(IApplicationReleaseConfiguration configuration)
        {
            var updateServer = configuration.GetString("UPDATE_SERVER");
            var updateKey = configuration.GetString("UPDATE_KEY");
            var marketplace = configuration.GetString("MARKETPLACE");
            var platformString = configuration.GetString("PLATFORM");
            UpdatePlatforms? platform = null;
            
            if (platformString != null && Enum.TryParse(platformString, true, out UpdatePlatforms p))
                platform = p;

            HasUpdateServerData = updateServer != null && marketplace != null && platform != null;
            UpdateServerUrl = SafeCreateUrl(updateServer, new Uri("http://localhost"));
            Marketplace = marketplace ?? "";
            UpdateKey = updateKey;
            Platform = platform ?? UpdatePlatforms.Windows;
        }

        private Uri SafeCreateUrl(string? uri, Uri fallback)
        {
            if (uri == null)
                return fallback;
            
            try
            {
                return new Uri(uri);
            }
            catch (Exception)
            {
                return fallback;
            }
        }
        
        public Uri UpdateServerUrl { get; }
        public string Marketplace { get; }
        public UpdatePlatforms Platform { get; }
        public string? UpdateKey { get; }
        public bool HasUpdateServerData { get; }
        public string Part => $"marketplace: {Marketplace}, platform: {Platform.ToString()}";
    }
}