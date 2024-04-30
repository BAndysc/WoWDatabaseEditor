using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.SqlDump;

namespace WDE.SqlWorkbench.Services.SqlImport;

internal interface IMySqlImportService
{
    Task ImportToDatabaseAsync(DatabaseCredentials credentials,
        MySqlToolsVersion version,
        FileInfo[] inputFiles,
        bool eachFileInNewSession,
        string? saveTempFile,
        Action<int> fileImported,
        Action<long> bytesReadCallback,
        Action<string, FileInfo?> errorCallback,
        CancellationToken token);
}