using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlDump;

internal interface IMySqlDumpService
{
    Task DumpDatabaseAsync(DatabaseCredentials credentials,
        MySqlDumpOptions options, 
        MySqlToolsVersion version,
        string[] allTables,
        string[] tables, 
        string outputFile,
        Action<long> bytesWrittenCallback,
        Action<string> errorCallback,
        CancellationToken token);
}

internal enum MySqlToolsVersion
{
    MySql,
    MariaDb
}