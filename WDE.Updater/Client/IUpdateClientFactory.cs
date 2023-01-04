using System;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    public interface IUpdateClientFactory
    {
        IUpdateClient Create(Uri updateServerUrl, string marketplace, string? key, UpdatePlatforms platform);
    }
}