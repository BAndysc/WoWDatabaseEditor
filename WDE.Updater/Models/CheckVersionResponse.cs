using System.Diagnostics.CodeAnalysis;

namespace WDE.Updater.Models
{
    [ExcludeFromCodeCoverage]
    public class CheckVersionResponse
    {
        public long LatestVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public ChangeLogEntry[]? ChangeLog { get; set; }
    }
}