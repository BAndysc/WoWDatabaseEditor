using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Utils;
using WDE.Updater.Models;

namespace WDE.Updater.Client
{
    public class UpdateClient : IUpdateClient
    {
        private readonly Uri updateServerUrl;
        private readonly string marketplace;
        private readonly string? key;
        private readonly UpdatePlatforms platform;
        private readonly HttpClient client;

        public UpdateClient(Uri updateServerUrl, string marketplace, string? key, UpdatePlatforms platform, HttpClient client)
        {
            this.updateServerUrl = updateServerUrl;
            this.marketplace = marketplace;
            this.key = key;
            this.platform = platform;
            this.client = client;
            if (IsUnsupportedOS())
                this.platform = UpdatePlatforms.Windows7;
        }

        /**
         * returns true on windows 7 and older
         */
        private bool IsUnsupportedOS()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && os.Version.Major <= 6 && os.Version.Minor <= 1;
        }
        
        public async Task<CheckVersionResponse> CheckForUpdates(string branch, long version)
        {
            var request = new CheckVersionRequest(version, marketplace, branch, platform, key);
            var response = await client.PostAsync(
                updateServerUrl + "CheckVersion", 
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CheckVersionResponse>(responseBody);
                if (result != null)
                    return result;
            }

            throw new Exception("Update server returned " + response.StatusCode);
        }

        public async Task DownloadUpdate(CheckVersionResponse versionResponse, string destination, IProgress<(long, long?)>? progress = null)
        {
            using var response = await client.GetAsync(Path.Join(updateServerUrl.AbsoluteUri, versionResponse.DownloadUrl!.TrimStart('/')), HttpCompletionOption.ResponseHeadersRead);
            var contentLength = response.Content.Headers.ContentLength;
            
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var file = File.OpenWrite(destination);
            
            var relativeProgress = new Progress<long>(totalBytes => progress?.Report((totalBytes, contentLength)));
            await stream.CopyToAsync(file, 81920, relativeProgress);
            progress?.Report((contentLength ?? -1, contentLength));
            await stream.CopyToAsync(file);
        }
    }
}