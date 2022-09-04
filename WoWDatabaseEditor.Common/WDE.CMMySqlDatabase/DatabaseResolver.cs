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
    private readonly IMangosDatabaseProvider mangosWorld;

    public DatabaseResolver(ICurrentCoreVersion core,
        Lazy<DatabaseProviderWoTLK> cmWrath,
        Lazy<DatabaseProviderTBC> cmTbc,
        Lazy<DatabaseProviderClassic> cmClassic)
    {
        switch (core.Current.Tag)
        {
            case "CMaNGOS-WoTLK":
            {
                var db= cmWrath.Value;
                auth = db;
                world = db;
                mangosWorld = db;
                break;
            }
            case "CMaNGOS-TBC":
            {
                var db= cmTbc.Value;
                auth = db;
                world = db;
                mangosWorld = db;
                break;
            }
            case "CMaNGOS-Classic":
            {
                var db= cmClassic.Value;
                auth = db;
                world = db;
                mangosWorld = db;
                break;
            }
            default:
            {
                throw new ArgumentException("Unexpected core tag! " + core.Current.Tag);
            }
        }
    }

    public IAsyncDatabaseProvider ResolveWorld() => world;
    
    public IMangosDatabaseProvider ResolveMangosWorld() => mangosWorld;

    public IAuthDatabaseProvider ResolveAuth() => auth;
}