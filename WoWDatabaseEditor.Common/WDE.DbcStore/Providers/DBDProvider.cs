using DBCD.Providers;
using System;
using System.IO;
using System.Net.Http;

namespace WDE.DbcStore.Providers
{
    public class DBDProvider : IDBDProvider
    {
        private static Uri BaseURI = new Uri("https://raw.githubusercontent.com/wowdev/WoWDBDefs/master/definitions/");
        private static string CachePath = "dbdCache/";
        private HttpClient client = new HttpClient();

        public DBDProvider()
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);

            client.BaseAddress = BaseURI;
        }

        public Stream StreamForTableName(string tableName, string build = null)
        {
            string dbdName = Path.GetFileName(tableName).Replace(".db2", ".dbd");

            if (!File.Exists($"{CachePath}/{dbdName}") || (DateTime.Now - File.GetLastWriteTime($"{CachePath}/{dbdName}")).TotalHours > 24)
            {
                try
                {
                    var bytes = client.GetByteArrayAsync(dbdName).Result;
                    File.WriteAllBytes($"{CachePath}/{dbdName}", bytes);

                    return new MemoryStream(bytes);
                }
                catch (HttpRequestException /*requestException*/)
                {
                    if (File.Exists($"{CachePath}/{dbdName}"))
                        return new MemoryStream(File.ReadAllBytes($"{CachePath}/{dbdName}"));
                    else
                        return null;
                }
            }
            else
                return new MemoryStream(File.ReadAllBytes($"{CachePath}/{dbdName}"));
        }
    }
}
