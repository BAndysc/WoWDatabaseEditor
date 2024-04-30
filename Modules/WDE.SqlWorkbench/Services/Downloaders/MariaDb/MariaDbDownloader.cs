using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MariaDb;

[AutoRegister]
[SingleInstance]
internal class MariaDbDownloader : IMariaDbDownloader
{
    private HttpClient client;

    private static string URL = "https://downloads.mariadb.org/rest-api";

    public MariaDbDownloader()
    {
        client = new HttpClient();
    }
    
    public async Task<IReadOnlyList<MariaDbRelease>> GetReleasesAsync(CancellationToken token = default)
    {
        var releases = await client.GetFromJsonAsync<Dictionary<string, List<MariaDbRelease>>>($"{URL}/mariadb/", cancellationToken: token);
        return releases?.SelectMany(x => x.Value).ToArray() ?? Array.Empty<MariaDbRelease>();
    }
    
    public async Task<string?> GetDownloadLinkAsync(string version, CancellationToken token = default)
    {
        var productInfo = await client.GetFromJsonAsync<MariaProductInfo>($"{URL}/mariadb/{version}", cancellationToken: token);
        var x = productInfo.Releases
            .SelectMany(x => x.Value.Files
                .Where(FileMatchesOS)
                .Where(x => x.PackageType == "ZIP file" &&
                            !x.FileDownloadUrl.Contains("debug", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();
        return x.FileDownloadUrl;
    }

    public async Task DownloadAsync(string version, string outputPath, ITaskProgress progress, CancellationToken token = default)
    {
        var downloadLink = await GetDownloadLinkAsync(version, token);
        if (downloadLink == null)
            throw new Exception("Cannot find download link for MariaDB for your OS");
        
        var response = await client.GetAsync(downloadLink, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode)
            throw new Exception("Cannot download MariaDB");
        
        var total = response.Content.Headers.ContentLength ?? 0;
        var read = 0L;
        var buffer = new byte[8192];
        var content = response.Content;
        await using var stream = await content.ReadAsStreamAsync(token);
        await using var fileStream = System.IO.File.Create(outputPath);
        
        while (true)
        {
            var length = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            if (length <= 0)
                break;
            read += length;
            await fileStream.WriteAsync(buffer, 0, length, token);
            progress.Report((int)read, (int)total, null);
            
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
        }
    }

    public async Task<(string dump, string mariadb)> DownloadMariaDbAsync(string version, string outputDirectory, ITaskProgress progress, CancellationToken token = default)
    {
        var tempDir = Path.GetTempFileName() + ".dir";
        Directory.CreateDirectory(tempDir);
        var outMaria = Path.Combine(tempDir, "mariadb.zip");

        try
        {
            await DownloadAsync(version, outMaria, progress, token);

            await using var stream = new FileStream(Path.Combine(tempDir, "mariadb.zip"), FileMode.Open);
            var zip = new ZipArchive(stream);
            var entryDump = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("mariadb-dump.exe", StringComparison.OrdinalIgnoreCase));
            var entryMariaDb = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("mariadb.exe", StringComparison.OrdinalIgnoreCase));

            if (entryDump == null)
                throw new Exception("Cannot find mariadb-dump.exe in downloaded archive");

            if (entryMariaDb == null)
                throw new Exception("Cannot find mariadb.exe in downloaded archive");

            var outFileDump = Path.Combine(outputDirectory, "mariadb-dump.exe");
            var outFileMariaDb = Path.Combine(outputDirectory, "mariadb.exe");
            entryDump.ExtractToFile(outFileDump, true);
            entryMariaDb.ExtractToFile(outFileMariaDb, true);

            if (!File.Exists(outFileDump))
                throw new Exception("Cannot find mariadb-dump.exe in downloaded archive");

            if (!File.Exists(outFileMariaDb))
                throw new Exception("Cannot find mariadb.exe in downloaded archive");

            return (Path.GetFullPath(outFileDump), Path.GetFullPath(outFileMariaDb));
        }
        finally
        {
            if (File.Exists(outMaria))
                File.Delete(outMaria);
        }
    }

    private bool FileMatchesOS(MariaReleaseFile file)
    {
        var currentOs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : null;
        var currentArch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" :
            RuntimeInformation.OSArchitecture == Architecture.X64 ? "x86_64" : null;
        var fallbackArch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "x86_64" : null;

        return file.Os == currentOs && (file.Cpu == currentArch || file.Cpu == fallbackArch);
    }
}