using System.Diagnostics.CodeAnalysis;

namespace WDE.Updater.Models
{
    [ExcludeFromCodeCoverage]
    public class CheckVersionRequest
    {
        public long CurrentVersion { get; }
        public string Marketplace { get; }
        public string Branch { get; }
        public Platforms Platform { get; }
        public string? Key { get; }

        public CheckVersionRequest(long currentVersion, string marketplace, string branch, Platforms platform, string? key)
        {
            CurrentVersion = currentVersion;
            Marketplace = marketplace;
            Branch = branch;
            Platform = platform;
            Key = key;
        }
    }
}