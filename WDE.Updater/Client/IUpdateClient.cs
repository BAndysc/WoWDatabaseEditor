using System;
using System.Threading.Tasks;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    public interface IUpdateClient
    {
        Task<CheckVersionResponse> CheckForUpdates(string branch, long version);
        Task DownloadUpdate(CheckVersionResponse response, string destination, IProgress<float>? progress = null);
    }
}