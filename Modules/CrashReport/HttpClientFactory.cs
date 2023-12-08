using System;
using System.Net.Http;
using WDE.Common.Factories;
using WDE.Common.Services;

namespace CrashReport;

public class HttpClientFactory : IHttpClientFactory
{
    private readonly ApplicationVersion version;

    public HttpClientFactory(ApplicationVersion version)
    {
        this.version = version;
    }
    
    public HttpClient Factory()
    {
        var client = new HttpClient();
        var os = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
        client.DefaultRequestHeaders.Add("User-Agent", $"WoWDatabaseEditor/{version.BuildVersion} CrashReporter (branch: {version.Branch}, marketplace: {version.Marketplace}, platform: {version.Platform}, OS: {os})");
        return client;
    }
}