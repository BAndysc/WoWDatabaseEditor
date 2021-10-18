using System;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.WoWHeadConnector
{
    [AutoRegister]
    [SingleInstance]
    internal class WwwClient : IWwwClient
    {
        public async Task<string> FetchContent(Uri uri)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(6);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:93.0) Gecko/20100101 Firefox/93.0");
            return await client.GetStringAsync(uri);
        }
    }
}