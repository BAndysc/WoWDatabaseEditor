using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WDE.SqlWorkbench.Services.Downloaders.MySql;

[AutoRegister]
[SingleInstance]
internal class MySqlDbDownloader : IMySqlDownloader
{
    private HttpClient client;

    private static string RELEASES_URL = "https://dev.mysql.com/downloads/mysql/";
    // 3 - windows, because only this is supported now
    private static string LISTING_URL = "https://dev.mysql.com/downloads/mysql/?tpl=platform&os=3&osva=&version=";
    private static string BASE_URL = "https://dev.mysql.com/";

    public MySqlDbDownloader()
    {
        client = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip,
        });
        var randomChromeVersion = new Random().Next(90, 120);
        var randomAppleWebKitVersion = new Random().Next(450, 550);
        var randomAppleWebKitSubVersion = new Random().Next(10, 90);
        // MySql won't allow to download without user agent -.-'
        client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/{randomAppleWebKitVersion}.{randomAppleWebKitSubVersion} (KHTML, like Gecko) Chrome/{randomChromeVersion}.0.0.0 Safari/{randomAppleWebKitVersion}.{randomAppleWebKitSubVersion}");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en");
        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        client.DefaultRequestHeaders.Add("Sec-Ch-Ua", $"\"Google Chrome\";v=\"{randomChromeVersion}\", \"Chromium\";v=\"{randomChromeVersion}\", \"Not?A_Brand\";v=\"24\"");
        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"macOS\"");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }
    
    public async Task<IReadOnlyList<MySqlRelease>> GetReleasesAsync(CancellationToken token = default)
    {
        var releases = await client.GetStringAsync(RELEASES_URL, token);
        var indexOfSelect = releases.IndexOf("""<select name="version" """);
        var endOfSelect = releases.IndexOf("</select>", indexOfSelect);
        
        if (indexOfSelect == -1 || endOfSelect == -1)
            throw new Exception("Cannot find releases list on MySQL website");
        
        var select = releases.Substring(indexOfSelect, endOfSelect - indexOfSelect);
        var regex = new Regex("<option\\s+value=\"(.*?)\"[^>]*>\\s*(.*?)\\s*<\\/option>");
        var matches = regex.Matches(select);
        var result = new List<MySqlRelease>();
        foreach (Match match in matches)
        {
            var id = match.Groups[1].Value;
            var name = match.Groups[2].Value;
            result.Add(new MySqlRelease()
            {
                Id = id,
                Name = name
            });
        }

        return result;
    }
    
    public async Task<string?> GetDownloadLinkAsync(string id, CancellationToken token = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Only Windows is supported now. But you can download and install MySQL manually.");
        var html = await client.GetStringAsync($"{LISTING_URL}{id}", cancellationToken: token);
        var regex = new Regex(
            "<tr[^>]*>\\s*<td class=\"col1\"><b>\\s*(.*?)\\s*<\\/b><\\/td>\\s*<td class=\"col3\">\\s*((?:.|\\n)*?)\\s*<\\/td>\\s*<td class=\"col4\">\\s*((?:.|\\n)*?)\\s*<\\/td>\\s*<td class=\"col5\">\\s*((?:.|\\n)*?)\\s*<\\/td>\\s*");

        var hrefRegex = new Regex("href=\"(.*?)\"");
        
        // praise MariaDb for exposing a REST API, Oracle is....
        
        var matches = regex.Matches(html);
        
        string? fileLink = null;

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var version = match.Groups[2].Value;
            var size = match.Groups[3].Value;
            var downloadLinkRaw = match.Groups[4].Value;
            
            var hrefMatch = hrefRegex.Match(downloadLinkRaw);
            
            if (!hrefMatch.Success)
                continue;
            
            var downloadLink = hrefMatch.Groups[1].Value;
            
            if (name.Contains("debug", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!name.Contains("zip", StringComparison.OrdinalIgnoreCase))
                continue;
            
            if (!name.Contains("64-bit", StringComparison.OrdinalIgnoreCase))
                continue;

            fileLink = Path.Join(BASE_URL, downloadLink);
            break;
        }

        if (fileLink != null)
        {
            var linkHtml = await client.GetStringAsync(fileLink, token);
            var linkRegex = new Regex("<a href=\"(.*?)\">No thanks, just start my download\\.<\\/a>");
            var linkMatch = linkRegex.Match(linkHtml);
            if (linkMatch.Success)
                return Path.Join(BASE_URL, linkMatch.Groups[1].Value);
        }
        
        return null;
    }

    public async Task DownloadAsync(string version, string outputPath, ITaskProgress progress, CancellationToken token = default)
    {
        var downloadLink = await GetDownloadLinkAsync(version, token);
        if (downloadLink == null)
            throw new Exception("Cannot find download link for MySQL for your OS");
        
        var response = await client.GetAsync(downloadLink, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode)
            throw new Exception("Cannot download MySQL");
        
        var total = response.Content.Headers.ContentLength ?? 0;
        var read = 0L;
        var buffer = new byte[8192];
        var content = response.Content;
        await using var stream = await content.ReadAsStreamAsync(token);
        await using var fileStream = File.Create(outputPath);
        
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

    public async Task<(string dump, string mysql)> DownloadMySqlAsync(string version, string outputDirectory, ITaskProgress progress, CancellationToken token = default)
    {
        var tempDir = Path.GetTempFileName() + ".dir";
        Directory.CreateDirectory(tempDir);
        var outMySqlZip = Path.Combine(tempDir, "mysql.zip");

        try
        {
            await DownloadAsync(version, outMySqlZip, progress, token);

            await using var stream = new FileStream(Path.Combine(tempDir, "mysql.zip"), FileMode.Open);
            var zip = new ZipArchive(stream);
            var mysqldump = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("mysqldump.exe", StringComparison.OrdinalIgnoreCase));
            var mysql = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("mysql.exe", StringComparison.OrdinalIgnoreCase));
            var libcrypto = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("libcrypto-3-x64.dll", StringComparison.OrdinalIgnoreCase));
            var libssl = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("libssl-3-x64.dll", StringComparison.OrdinalIgnoreCase));
            
            if (mysqldump == null)
                throw new Exception("Cannot find mysqldump.exe in downloaded archive");

            if (mysql == null)
                throw new Exception("Cannot find mysql.exe in downloaded archive");

            if (libcrypto == null)
                throw new Exception("Cannot find libcrypto-3-x64.dll in downloaded archive");
            
            if (libssl == null)
                throw new Exception("Cannot find libssl-3-x64.dll in downloaded archive");
            
            var outMySqlDump = Path.Combine(outputDirectory, "mysqldump.exe");
            var outMySql = Path.Combine(outputDirectory, "mysql.exe");
            var outLibCrypto = Path.Combine(outputDirectory, "libcrypto-3-x64.dll");
            var outLibSsl = Path.Combine(outputDirectory, "libssl-3-x64.dll");
            
            mysqldump.ExtractToFile(outMySqlDump, true);
            mysql.ExtractToFile(outMySql, true);
            libcrypto.ExtractToFile(outLibCrypto, true);
            libssl.ExtractToFile(outLibSsl, true);
            
            if (!File.Exists(outMySqlDump))
                throw new Exception("Cannot find mysqldump.exe in downloaded archive");

            if (!File.Exists(outMySql))
                throw new Exception("Cannot find mysql.exe in downloaded archive");

            return (Path.GetFullPath(outMySqlDump), Path.GetFullPath(outMySql));
        }
        finally
        {
            if (File.Exists(outMySqlZip))
                File.Delete(outMySqlZip);
        }
    }
}