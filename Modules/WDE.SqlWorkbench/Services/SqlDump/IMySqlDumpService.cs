using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal interface IMySqlDumpService
{
    Task DumpDatabaseAsync(DatabaseCredentials credentials, MySqlDumpOptions options, string[] tables, string outputFile, CancellationToken token);
}