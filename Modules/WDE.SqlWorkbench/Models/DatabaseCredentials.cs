using System;
using Newtonsoft.Json;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.SqlWorkbench.Models;

internal readonly struct DatabaseCredentials : IEquatable<DatabaseCredentials>
{
    public readonly string User;
    public readonly string Passwd;
    public readonly string Host;
    public readonly int Port;
    public readonly string SchemaName;

    [JsonConstructor]
    public DatabaseCredentials(string user, string passwd, string host, int port, string schemaName)
    {
        User = user;
        Passwd = passwd;
        Host = host;
        Port = port;
        SchemaName = schemaName;
    }

    public DatabaseCredentials(IDbAccess dbAccess)
    {
        User = dbAccess.User ?? "";
        Passwd = dbAccess.Password ?? "";
        Host = dbAccess.Host ?? "";
        Port = dbAccess.Port ?? 0;
        SchemaName = dbAccess.Database ?? "";
    }

    public DatabaseCredentials WithSchemaName(string schemaName)
    {
        return new DatabaseCredentials(User, Passwd, Host, Port, schemaName);
    }

    public bool Equals(DatabaseCredentials other)
    {
        return User == other.User && Passwd == other.Passwd && Host == other.Host && Port == other.Port && SchemaName == other.SchemaName;
    }

    public override bool Equals(object? obj)
    {
        return obj is DatabaseCredentials other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(User, Passwd, Host, Port, SchemaName);
    }

    public static bool operator ==(DatabaseCredentials left, DatabaseCredentials right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DatabaseCredentials left, DatabaseCredentials right)
    {
        return !left.Equals(right);
    }
    
    public static DatabaseCredentials FromDbAccess(IDbAccess dbAccess)
    {
        return new DatabaseCredentials(dbAccess.User ?? "", dbAccess.Password ?? "", dbAccess.Host ?? "", dbAccess.Port ?? 0, dbAccess.Database ?? "");
    }
}