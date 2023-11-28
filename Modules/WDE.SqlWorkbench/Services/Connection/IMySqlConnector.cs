using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.LanguageServer;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IMySqlConnector
{
    Task<IMySqlSession> ConnectAsync(string connectionString);
    Task<IMySqlSession> ConnectAsync(DatabaseCredentials credentials);
}