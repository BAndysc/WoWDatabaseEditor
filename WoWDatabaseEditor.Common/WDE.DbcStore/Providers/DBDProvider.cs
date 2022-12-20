using DBCD.Providers;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Providers
{
    [AutoRegister]
    public class DBDProvider : IDBDProvider
    {
        private static Uri BaseURI = new Uri("https://raw.githubusercontent.com/wowdev/WoWDBDefs/master/definitions/");
        private static string CachePath = "~/dbdCache/";
        private HttpClient client;
        readonly IFileSystem fileSystem;

        public DBDProvider(HttpClient client, IFileSystem fileSystem)
        {
            this.client = client;
            this.fileSystem = fileSystem;

            this.client.BaseAddress = BaseURI;
        }

        public Stream? StreamForTableName(string tableName, string? build = null)
        {
            string dbdName = Path.ChangeExtension(Path.GetFileName(tableName), ".dbd");

            if (!fileSystem.Exists($"{CachePath}/{dbdName}") || (DateTime.Now - fileSystem.GetLastWriteTime($"{CachePath}/{dbdName}")).TotalHours > 24)
            {
                try
                {
                    var webRequest = new HttpRequestMessage(HttpMethod.Get, "https://raw.githubusercontent.com/wowdev/WoWDBDefs/master/definitions/" + dbdName);                   
                    var response = client.Send(webRequest);

                    using var reader = new StreamReader(response.Content.ReadAsStream());
                    var bytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());

                    var f = fileSystem.OpenWrite($"{CachePath}/{dbdName}");
                    f.Write(bytes);
                    f.Close();

                    return new MemoryStream(bytes);
                }
                catch (HttpRequestException /*requestException*/)
                {
                    if (fileSystem.Exists($"{CachePath}/{dbdName}"))
                        return new MemoryStream(fileSystem.ReadAllBytes($"{CachePath}/{dbdName}"));
                    else
                        return null;
                }
            }
            else
                return new MemoryStream(fileSystem.ReadAllBytes($"{CachePath}/{dbdName}"));
        }
    }
}
