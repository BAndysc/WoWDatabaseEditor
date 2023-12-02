using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal interface IMySqlDumpService
{
    Task DumpDatabaseAsync(DatabaseCredentials credentials,
        MySqlDumpOptions options, 
        string[] allTables,
        string[] tables, 
        string outputFile,
        Action<long> bytesWrittenCallback,
        CancellationToken token);
}