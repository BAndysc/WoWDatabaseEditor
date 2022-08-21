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
    private readonly DatabaseProviderWoTLK world;

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
                throw new ArgumentException("Unexpected core tag! " + core.Current.Tag);
            }
        }
    }

    public IAsyncDatabaseProvider ResolveWorld() => world;
    
    public IMangosDatabaseProvider ResolveMangosWorld() => world;

    public IAuthDatabaseProvider ResolveAuth() => auth;
}