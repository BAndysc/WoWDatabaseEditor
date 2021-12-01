using System;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.TrinityMySqlDatabase.Database;

namespace WDE.TrinityMySqlDatabase;

[AutoRegister]
[SingleInstance]
public class DatabaseResolver
{
    private readonly IAuthDatabaseProvider auth;
    private readonly IAsyncDatabaseProvider world;

    public DatabaseResolver(ICurrentCoreVersion core,
        Lazy<TrinityWrathMySqlDatabaseProvider> tcWrath,
        Lazy<TrinityCataMySqlDatabaseProvider> tcCata,
        Lazy<TrinityMasterMySqlDatabaseProvider> tcMaster,
        Lazy<AzerothhMySqlDatabaseProvider> azeroth)
    {
        switch (core.Current.Tag)
        {
            case "Azeroth":
            {
                var db= azeroth.Value;
                auth = db;
                world = db;
                break;
            }
            case "TrinityWrath":
            {
                var db= tcWrath.Value;
                auth = db;
                world = db;
                break;
            }
            case "TrinityCata":
            {
                var db= tcCata.Value;
                auth = db;
                world = db;
                break;
            }
            case "TrinityMaster":
            {
                var db= tcMaster.Value;
                auth = db;
                world = db;
                break;
            }
            default:
                throw new Exception("Unknown core version");
        }
    }

    public IAsyncDatabaseProvider ResolveWorld() => world;

    public IAuthDatabaseProvider ResolveAuth() => auth;
}