using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal interface IMySqlConnector
{
    Task<IRawMySqlConnection> ConnectAsync(string connectionString);
    Task<IRawMySqlConnection> ConnectAsync(DatabaseCredentials credentials);
}