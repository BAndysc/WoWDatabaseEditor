using System;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.CMMySqlDatabase.Database;

namespace WDE.CMMySqlDatabase;

[AutoRegister]
[SingleInstance]
public class DatabaseResolver
{
    private readonly IAuthDatabaseProvider auth;
    private readonly IAsyncDatabaseProvider world;

    public DatabaseResolver(ICurrentCoreVersion core,
        Lazy<DatabaseProviderWoTLK> cmWrath)
    {
        switch (core.Current.Tag)
        {
            case "CMaNGOS-WoTLK":
            {
                var db= cmWrath.Value;
                auth = db;
                world = db;
                break;
            }
            default:
            {
                var db= cmWrath.Value;
                auth = db;
                world = db;
                return;
            }
        }
    }

    public IAsyncDatabaseProvider ResolveWorld() => world;

    public IAuthDatabaseProvider ResolveAuth() => auth;
}