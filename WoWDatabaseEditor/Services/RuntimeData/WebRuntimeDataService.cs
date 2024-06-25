using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.RuntimeData;

[AutoRegister(Platforms.Browser | Platforms.iOS)]
[SingleInstance]
public class WebRuntimeDataService : IRuntimeDataService
{
    private string host;
    private HttpClient httpClient;

    public WebRuntimeDataService(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.Factory();
        host = GlobalApplication.Arguments.GetValue("href") ?? "http://localhost:5000";
        LOG.LogInformation("Using root host " + host);
    }
    
    public async Task<string> ReadAllText(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(Path.Combine(host, path));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            throw new DataMissingException(path, e);
        }
    }

    public async Task<bool> Exists(string path)
    {
        var response = await httpClient.GetAsync(Path.Combine(host, path));
        return response.IsSuccessStatusCode;
    }

    public IDirectoryWatcher WatchDirectory(string path, bool recursive)
    {
        return NullDirectoryWatcher.Instance;
    }

    public async Task<byte[]> ReadAllBytes(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(Path.Combine(host, path));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception e)
        {
            throw new DataMissingException(path, e);
        }
    }

    public async Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern)
    {
        var response = await httpClient.GetAsync(Path.Combine(host, directory, "files.txt"));
        response.EnsureSuccessStatusCode();
        var allFiles = (await response.Content.ReadAsStringAsync()).Split("\n");

        List<string> results = new List<string>();
        foreach (var file in allFiles)
        {
            if (SearchPatternMatch(Path.GetFileName(file), searchPattern))
                results.Add(file);
        }

        return results;
    }

    private bool SearchPatternMatch(string file, string searchPattern)
    {
        var anyPrefix = searchPattern.StartsWith('*');
        var anySuffix = searchPattern.EndsWith('*');
        if (anyPrefix && anySuffix)
            return file.Contains(searchPattern.Substring(1, searchPattern.Length - 2));
        if (anyPrefix)
            return file.EndsWith(searchPattern.Substring(1));
        if (anySuffix)
            return file.StartsWith(searchPattern.Substring(0, searchPattern.Length - 1));
        return file == searchPattern;
    }

    private class NullDirectoryWatcher : IDirectoryWatcher
    {
        public static NullDirectoryWatcher Instance = new NullDirectoryWatcher();

        public void Dispose()
        {
        }

        public event Action<WatcherChangeTypes, string>? OnChanged;
    }
}