using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.LanguageServer;

namespace WDE.SqlWorkbench.Services.Connection;

[SingleInstance]
[AutoRegister]
internal class MySqlConnector : IMySqlConnector
{
    private readonly IQuerySafetyService querySafetyService;

    public MySqlConnector(IQuerySafetyService querySafetyService)
    {
        this.querySafetyService = querySafetyService;
    }
    
    public async Task<IRawMySqlConnection> ConnectAsync(string connectionString, QueryExecutionSafety safeMode)
    {
        MySqlConnection conn = new(connectionString);
        await conn.OpenAsync();
        return new RawMySqlConnection(conn, querySafetyService, safeMode);
    }

    public Task<IRawMySqlConnection> ConnectAsync(DatabaseCredentials credentials, QueryExecutionSafety safeMode)
    {
        return ConnectAsync($"Server={credentials.Host};Port={credentials.Port};Database={credentials.SchemaName};Uid={credentials.User};Pwd={credentials.Passwd};AllowUserVariables=True;AllowZeroDateTime=True;ApplicationName=WoWDatabaseEditor;Pooling=False;ConnectionTimeout=30;DefaultCommandTimeout=0;GuidFormat=None;TreatTinyAsBoolean=False;UseAffectedRows=True", safeMode);
    }
}