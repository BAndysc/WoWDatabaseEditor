using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Processes;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.SqlDump;
using WDE.SqlWorkbench.Settings;

namespace WDE.SqlWorkbench.Services.SqlImport;

[AutoRegister]
[SingleInstance]
internal class MySqlImportService : IMySqlImportService
{
    private readonly IProgramFinder programFinder;
    private readonly IMainThread mainThread;
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IProcessService processService;

    private static byte[] NewLine = "\n"u8.ToArray();
    
    public MySqlImportService(IProgramFinder programFinder,
        IMainThread mainThread,
        ISqlWorkbenchPreferences preferences,
        IProcessService processService)
    {
        this.programFinder = programFinder;
        this.mainThread = mainThread;
        this.preferences = preferences;
        this.processService = processService;
    }
    
    public async Task ImportToDatabaseAsync(DatabaseCredentials credentials,
        MySqlToolsVersion version,
        FileInfo[] inputFiles,
        bool eachFileInNewSession,
        string? saveTempFile,
        Action<int> fileImported,
        Action<long> bytesReadCallback, Action<string, FileInfo?> errorCallback, CancellationToken token)
    {
        if (inputFiles.Length == 0)
            return;

        List<string> arguments = new();
        arguments.Add(credentials.SchemaName);

        if (credentials.Host.StartsWith("/"))
        {
            arguments.Add("--socket");
            arguments.Add(credentials.Host);
        }
        else
        {
            arguments.Add("--host");
            arguments.Add(credentials.Host);
            arguments.Add("--port");
            arguments.Add(credentials.Port.ToString());
            arguments.Add("--user");
            arguments.Add(credentials.User);
            if (!string.IsNullOrEmpty(credentials.Passwd))
                arguments.Add($"--password={credentials.Passwd}");
            else
                arguments.Add("--skip-password");
        }

        bool[] isClosed = {false};

        var mysqlPath = LocateImporter(version);

        byte[] buffer = new byte[4096];

        var tmpFile = saveTempFile == null ? null : File.Open(saveTempFile, FileMode.Create, FileAccess.Write);

#pragma warning disable CS8321 // Local function is declared but never used
        async Task<int> ReadUntilByte(FileStream stream, byte b, CancellationToken token)
#pragma warning restore CS8321 // Local function is declared but never used
        {
            int totalBytesRead = 0;
            while (true)
            {
                if (totalBytesRead + 4096 > buffer.Length)
                    Array.Resize(ref buffer, Math.Max(buffer.Length * 2, buffer.Length + 4096));

                var outMem = buffer.AsMemory(totalBytesRead, 4096);
                var bytesRead = await stream.ReadAsync(outMem, token);
                if (bytesRead == 0)
                    break;

                for (int i = 0; i < bytesRead; ++i)
                {
                    if (buffer[totalBytesRead + i] == b)
                    {
                        totalBytesRead += i + 1;
                        stream.Seek(-(bytesRead - i - 1), SeekOrigin.Current);
                        return totalBytesRead;
                    }
                }

                totalBytesRead += bytesRead;
                token.ThrowIfCancellationRequested();
            }

            return totalBytesRead;
        }

        var queriesSeparator = "\nDELIMITER ;\n"u8.ToArray();

        async Task WriteTask(FileInfo[] files, bool reportState, Stream mySqlInputStream)
        {
            for (var index = 0; index < files.Length; index++)
            {
                var file = files[index];
                await using var fileStream = file.OpenRead();
                while (true)
                {
                    var bytesRead =
                        await fileStream.ReadAsync(buffer,
                            token); //  await ReadUntilByte(fileStream, 10 /* \n */, token);
                    token.ThrowIfCancellationRequested();
                    if (bytesRead == 0)
                        break;

                    if (isClosed[0])
                        throw new Exception("mysql process closed unexpectedly");
                    await mySqlInputStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    if (tmpFile != null)
                        await tmpFile.WriteAsync(buffer.AsMemory(0, bytesRead), token);

                    mainThread.Dispatch(() => bytesReadCallback(bytesRead));

                    token.ThrowIfCancellationRequested();
                }

                await mySqlInputStream.WriteAsync(queriesSeparator, token);
                if (tmpFile != null)
                    await tmpFile.WriteAsync(queriesSeparator, token);

                if (reportState)
                {
                    fileImported(index);
                }
            }

            mySqlInputStream.Close();
        }

        async Task DoJob(FileInfo[] files, bool reportState)
        {
            var mysqlTask = processService.Run(token, mysqlPath, arguments.ToList(), null, x =>
            {
            }, x => mainThread.Dispatch(() => errorCallback(x, files.Length > 1 ? null : files[0])),true, out var writer);

            var writeTask = WriteTask(files, reportState, writer.BaseStream);
            var finishedTask = await Task.WhenAny(writeTask, mysqlTask);

            if (finishedTask == mysqlTask)
            {
                // mysql finished first, so a problem occured
                isClosed[0] = true;
            }
            else if (finishedTask == writeTask)
            {
                // all good, so now let's just wait for mysql to finish
                await mysqlTask;
                isClosed[0] = true;
            }

            if (tmpFile != null)
                tmpFile.Close();

            var exitCode = await mysqlTask;
            if (exitCode != 0 && !token.IsCancellationRequested)
                throw new Exception("mysql failed with exit code " + exitCode + ". See the log for more details.");
        }

        if (eachFileInNewSession)
        {
            for (var index = 0; index < inputFiles.Length; index++)
            {
                var file = inputFiles[index];
                await DoJob(new[] { file }, false);
                fileImported(index);
            }
        }
        else
        {
            await DoJob(inputFiles, true);
        }
    }


    private string LocateImporter(MySqlToolsVersion version)
    {
        if (version == MySqlToolsVersion.MySql)
        {
            if (!string.IsNullOrEmpty(preferences.CustomMySqlImportPath) &&
                File.Exists(preferences.CustomMySqlImportPath))
                return preferences.CustomMySqlImportPath;

            return programFinder.TryLocate("MySQL Server 8.0/bin/mysql.exe",
                       "MySQL Server 8.1/bin/mysql.exe",
                       "MySQL Server 8.2/bin/mysql.exe",
                       "MySQL Server 8.3/bin/mysql.exe",
                       "MySQL Server 8.4/bin/mysql.exe",
                       "MySQL Server 8.6/bin/mysql.exe",
                       "MySQL Server 8.7/bin/mysql.exe",
                       "MySQL Server 8.8/bin/mysql.exe",
                       "MySQL Server 8.9/bin/mysql.exe",
                       "MySQL Server 9.0/bin/mysql.exe",
                       "MySQL Server 9.1/bin/mysql.exe",
                       "MySQL Server 9.2/bin/mysql.exe",
                       "MySQL Server 9.3/bin/mysql.exe",
                       "MySQL Server 5.0/bin/mysql.exe",
                       "MySQL Server 5.1/bin/mysql.exe",
                       "MySQL Server 5.5/bin/mysql.exe",
                       "MySQL Server 5.6/bin/mysql.exe",
                       "MySQL Server 5.7/bin/mysql.exe",
                       "mysql") ??
                   throw new Exception(
                       "Couldn't find mysql on your PC. You can configure the path in the settings.");
        }
        else
        {
            if (!string.IsNullOrEmpty(preferences.CustomMariaDumpPath) &&
                File.Exists(preferences.CustomMariaDumpPath))
                return preferences.CustomMariaDumpPath;

            return programFinder.TryLocate(
                       "MariaDB 11.11/bin/mariadb.exe",
                       "MariaDB 11.10/bin/mariadb.exe",
                       "MariaDB 11.9/bin/mariadb.exe",
                       "MariaDB 11.8/bin/mariadb.exe",
                       "MariaDB 11.7/bin/mariadb.exe",
                       "MariaDB 11.6/bin/mariadb.exe",
                       "MariaDB 11.5/bin/mariadb.exe",
                       "MariaDB 11.4/bin/mariadb.exe",
                       "MariaDB 11.3/bin/mariadb.exe",
                       "MariaDB 11.2/bin/mariadb.exe",
                       "MariaDB 11.1/bin/mariadb.exe",
                       "MariaDB 11.0/bin/mariadb.exe",
                       "MariaDB 10.11/bin/mariadb.exe",
                       "MariaDB 10.10/bin/mariadb.exe",
                       "MariaDB 10.9/bin/mariadb.exe",
                       "MariaDB 10.8/bin/mariadb.exe",
                       "MariaDB 10.7/bin/mariadb.exe",
                       "MariaDB 10.6/bin/mariadb.exe",
                       "MariaDB 10.5/bin/mariadb.exe",
                       "MariaDB 10.4/bin/mariadb.exe",
                       "MariaDB 10.3/bin/mariadb.exe",
                       "MariaDB 10.2/bin/mariadb.exe",
                       "MariaDB 10.1/bin/mariadb.exe",
                       "MariaDB 10.0/bin/mariadb.exe",
                       "mariadb") ??
                   throw new Exception(
                       "Couldn't find maridb on your PC. You can configure the path in the settings.");
        }
    }
}