using DBCD.Providers;
using System.IO;

namespace WDE.DbcStore.Providers
{
    public class DBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build) => File.OpenRead(tableName);
    }
}
