using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.WorldMap.Services
{
    [SingleInstance]
    [AutoRegister]
    public class MapDataDownloadService : IMapDataDownloadService
    {
        private readonly Uri mapServerUrl;
        private readonly HttpClient client;
        private readonly bool hasMapServer; 

        public MapDataDownloadService(IApplicationReleaseConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            var server = configuration.GetString("MAPDATA_SERVER");
            hasMapServer = server != null;
            this.mapServerUrl = new Uri(server ?? "http://localhost");
            this.client = httpClientFactory.Factory();
        }

        public async Task DownloadMaps(FileInfo destination, IProgress<(long, long?)>? progress = null)
        {
            if (!hasMapServer)
                throw new Exception("Map server unknown. Can't download maps.");
            
            using var response = await client.GetAsync(Path.Join(mapServerUrl.AbsoluteUri, "Static/335_maps_v1.zip"), HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new Exception("File not found on the server");
            
            var contentLength = response.Content.Headers.ContentLength;

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var file = destination.OpenWrite();
            
            var relativeProgress = new Progress<long>(totalBytes => progress?.Report((totalBytes, contentLength)));
            await stream.CopyToAsync(file, 81920, relativeProgress);
            progress?.Report((contentLength ?? -1, contentLength));
        }
    }

    [UniqueProvider]
    public interface IMapDataDownloadService
    {
        Task DownloadMaps(FileInfo destination, IProgress<(long, long?)>? progress = null);
    }
}