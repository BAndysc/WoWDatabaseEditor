using DBCD.Providers;
using System.IO;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Providers
{
    [AutoRegister]
    public class DBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build) => File.OpenRead(tableName);
    }
}
