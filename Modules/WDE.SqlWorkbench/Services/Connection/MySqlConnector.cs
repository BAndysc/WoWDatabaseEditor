using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.LanguageServer;

namespace WDE.SqlWorkbench.Services.Connection;

[SingleInstance]
[AutoRegister]
internal class MySqlConnector : IMySqlConnector
{
    public async Task<IRawMySqlConnection> ConnectAsync(string connectionString)
    {
        MySqlConnection conn = new(connectionString);
        await conn.OpenAsync();
        return new RawMySqlConnection(conn);
    }

    public Task<IRawMySqlConnection> ConnectAsync(DatabaseCredentials credentials)
    {
        return ConnectAsync($"Server={credentials.Host};Port={credentials.Port};Database={credentials.SchemaName};Uid={credentials.User};Pwd={credentials.Passwd};AllowUserVariables=True;AllowZeroDateTime=True;ApplicationName=WoWDatabaseEditor;Pooling=False;ConnectionTimeout=30;DefaultCommandTimeout=0");
    }
}