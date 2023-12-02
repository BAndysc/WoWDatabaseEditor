using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal interface IDatabaseDumpService
{
    Task ShowDumpDatabaseWindowAsync(DatabaseCredentials credentials);
    Task ShowDumpTableWindowAsync(DatabaseCredentials credentials, string? table);
}