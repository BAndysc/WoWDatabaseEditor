using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Providers;

namespace WDE.HttpDatabase.Mock;

[AutoRegister]
[SingleInstance]
public class MockHotfixDatabaseSettingsProvider : IHotfixDatabaseSettingsProvider
{
    public IDbAccess Settings { get; set; } = new DbAccess() { Database = "hotfix" };
}

[AutoRegister]
[SingleInstance]
public class MockWorldDatabaseSettingsProvider : IWorldDatabaseSettingsProvider
{
    public IDbAccess Settings { get; set; } = new DbAccess() { Database = "world" };
}

[AutoRegister]
[SingleInstance]
public class MockAuthDatabaseSettingsProvider : IAuthDatabaseSettingsProvider
{
    public IDbAccess Settings { get; set; } = new DbAccess() { Database = "auth" };
}

public class DbAccess : IDbAccess
{
    public string? Host { get; set; }
    public string? Password { get; set; }
    public int? Port { get; set; }
    public string? User { get; set; }
    public string? Database { get; set; }
    public bool IsEmpty => Database == null;
}