using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Tasks;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;
 
internal interface IMariaDbDownloader
{
    Task<IReadOnlyList<MariaDbRelease>> GetReleasesAsync(CancellationToken token = default);
    Task<string?> GetDownloadLinkAsync(string version, CancellationToken token = default);
    Task DownloadAsync(string version, string outputPath, ITaskProgress progress, CancellationToken token = default);
    Task<(string dump, string mariadb)> DownloadMariaDbAsync(string version, string outputDirectory, ITaskProgress progress, CancellationToken token = default);
}

internal struct MariaDbRelease
{
    [JsonPropertyName("release_id")]
    public string Id { get; set; } 
    
    [JsonPropertyName("release_name")]
    public string Name { get; set; }

    [JsonPropertyName("release_status")]
    public string Status { get; set; }

    [JsonPropertyName("release_support_type")]
    public string SupportType { get; set; }

    [JsonPropertyName("release_eol_date")]
    public string EolDate { get; set; }
}

internal struct MariaProductInfo
{
    [JsonPropertyName("releases")]
    public Dictionary<string, MariaReleaseInfo> Releases { get; set; }
}

internal struct MariaReleaseInfo
{
    [JsonPropertyName("release_id")]
    public string Id { get; set; } 
    
    [JsonPropertyName("release_name")]
    public string Name { get; set; }

    [JsonPropertyName("date_of_release")]
    public string DateOfRelease { get; set; }
    
    [JsonPropertyName("release_notes_url")]
    public string ReleaseNotesUrl { get; set; }
    
    [JsonPropertyName("change_log")]
    public string ChangeLog { get; set; }
    
    [JsonPropertyName("files")]
    public List<MariaReleaseFile> Files { get; set; }
}

internal struct MariaReleaseFile
{
    [JsonPropertyName("file_id")]
    public long Id { get; set; } 
    
    [JsonPropertyName("file_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("package_type")]
    public string PackageType { get; set; }
    
    [JsonPropertyName("os")]
    public string Os { get; set; }
    
    [JsonPropertyName("cpu")]
    public string Cpu { get; set; }
    
    [JsonPropertyName("signature")]
    public string Signature { get; set; }
    
    [JsonPropertyName("checksum_url")]
    public string ChecksumUrl { get; set; }
    
    [JsonPropertyName("signature_url")]
    public string SignatureUrl { get; set; }
    
    [JsonPropertyName("file_download_url")]
    public string FileDownloadUrl { get; set; }
}