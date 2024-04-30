using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;
 
internal interface IMySqlDownloader
{
    Task<IReadOnlyList<MySqlRelease>> GetReleasesAsync(CancellationToken token = default);
    Task<string?> GetDownloadLinkAsync(string version, CancellationToken token = default);
    Task DownloadAsync(string version, string outputPath, ITaskProgress progress, CancellationToken token = default);
    Task<(string dump, string mysql)> DownloadMySqlAsync(string version, string outputDirectory, ITaskProgress progress, CancellationToken token = default);
}

internal struct MySqlRelease
{
    public string Name { get; set; }

    public string Id { get; set; }
}