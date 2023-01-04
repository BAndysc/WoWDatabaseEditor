using System;
using WDE.Updater.Models;

namespace WDE.Updater.Data
{
    public interface IUpdateServerDataProvider
    {
        Uri UpdateServerUrl { get; }
        string Marketplace { get; }
        UpdatePlatforms Platform { get; }
        string? UpdateKey { get; }
        bool HasUpdateServerData { get; }
    }
}