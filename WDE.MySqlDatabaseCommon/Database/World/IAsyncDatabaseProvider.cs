using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public interface IAsyncDatabaseProvider : IDatabaseProvider
    {
        void ConnectOrThrow();
    }
}