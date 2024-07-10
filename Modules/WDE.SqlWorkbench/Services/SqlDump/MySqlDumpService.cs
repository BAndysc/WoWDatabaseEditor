using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Processes;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Settings;

namespace WDE.SqlWorkbench.Services.SqlDump;

[AutoRegister]
[SingleInstance]
internal class MySqlDumpService : IMySqlDumpService
{
    private readonly IProgramFinder programFinder;
    private readonly IMainThread mainThread;
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IProcessService processService;

    private static byte[] NewLine = "\n"u8.ToArray();
    
    public MySqlDumpService(IProgramFinder programFinder,
        IMainThread mainThread,
        ISqlWorkbenchPreferences preferences,
        IProcessService processService)
    {
        this.programFinder = programFinder;
        this.mainThread = mainThread;
        this.preferences = preferences;
        this.processService = processService;
    }

    private void AddOptions(MySqlDumpOptions options, List<string> output)
    {
        var fields = typeof(MySqlDumpOptions).GetFields();
        foreach (var field in fields)
        {
            var value = field.GetValue(options);
            var argumentName = field.GetCustomAttribute<ArgumentAttribute>()?.Name ?? null;
            
            if (argumentName == null)
                continue;
            
            if (value is bool b)
            {
                if (!b && field.GetCustomAttribute<SkipWhenFalseAttribute>() != null)
                    continue;
                
                output.Add($"--{argumentName}=" + (b ? "1" : "0"));
            }
            else if (value is string s)
            {
                output.Add($"--{argumentName}=\"{s}\"");
                output.Add(s);
            }
            else
                throw new Exception();
        }
    }
    
    public async Task DumpDatabaseAsync(DatabaseCredentials credentials, 
        MySqlDumpOptions options, 
        MySqlToolsVersion version,
        string[] allTables,
        string[] tables,
        string output, 
        Action<long> bytesWrittenCallback,
        Action<string> errorCallback,
        CancellationToken token)
    {
        List<string> arguments = new List<string>();

        arguments.Add(credentials.SchemaName);

        AddOptions(options, arguments);

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

        foreach (var ignoredTable in allTables.Except(tables))
        {
            arguments.Add("--ignore-table");
            arguments.Add($"{credentials.SchemaName}.{ignoredTable}");
        }

        await using var f = File.Open(output, FileMode.Create, FileAccess.Write);
        bool[] isClosed = {false};

        var mysqldumpPath = LocateDumper(version);
        
        var exitCode = await processService.Run(token, mysqldumpPath, arguments.ToList(), null, x =>
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(x);
            if (isClosed[0])
                return;
            
            f.Write(bytes);
            f.Write(NewLine);
            mainThread.Dispatch(() => bytesWrittenCallback(bytes.Length + NewLine.Length));
        }, x => mainThread.Dispatch(() => errorCallback(x)), false, out _);
        isClosed[0] = true;
        if (exitCode != 0 && !token.IsCancellationRequested)
            throw new Exception("mysqldump failed with exit code " + exitCode + ". See the log for more details.");
    }

    private string LocateDumper(MySqlToolsVersion version)
    {
        if (version == MySqlToolsVersion.MySql)
        {
            if (!string.IsNullOrEmpty(preferences.CustomMySqlDumpPath) &&
                File.Exists(preferences.CustomMySqlDumpPath))
                return preferences.CustomMySqlDumpPath;

            return programFinder.TryLocate("MySQL Server 8.0/bin/mysqldump.exe",
                "MySQL Server 8.1/bin/mysqldump.exe",
                "MySQL Server 8.2/bin/mysqldump.exe",
                "MySQL Server 8.3/bin/mysqldump.exe",
                "MySQL Server 8.4/bin/mysqldump.exe",
                "MySQL Server 8.6/bin/mysqldump.exe",
                "MySQL Server 8.7/bin/mysqldump.exe",
                "MySQL Server 8.8/bin/mysqldump.exe",
                "MySQL Server 8.9/bin/mysqldump.exe",
                "MySQL Server 9.0/bin/mysqldump.exe",
                "MySQL Server 9.1/bin/mysqldump.exe",
                "MySQL Server 9.2/bin/mysqldump.exe",
                "MySQL Server 9.3/bin/mysqldump.exe",
                "MySQL Server 5.0/bin/mysqldump.exe",
                "MySQL Server 5.1/bin/mysqldump.exe",
                "MySQL Server 5.5/bin/mysqldump.exe",
                "MySQL Server 5.6/bin/mysqldump.exe",
                "MySQL Server 5.7/bin/mysqldump.exe",
                "mysqldump") ?? throw new Exception("Couldn't find mysqldump on your PC. You can configure the path in the settings.");
        }
        else
        {
            if (!string.IsNullOrEmpty(preferences.CustomMariaDumpPath) &&
                File.Exists(preferences.CustomMariaDumpPath))
                return preferences.CustomMariaDumpPath;

            return programFinder.TryLocate(
                "MariaDB 11.11/bin/mariadb-dump.exe",
                "MariaDB 11.10/bin/mariadb-dump.exe",
                "MariaDB 11.9/bin/mariadb-dump.exe",
                "MariaDB 11.8/bin/mariadb-dump.exe",
                "MariaDB 11.7/bin/mariadb-dump.exe",
                "MariaDB 11.6/bin/mariadb-dump.exe",
                "MariaDB 11.5/bin/mariadb-dump.exe",
                "MariaDB 11.4/bin/mariadb-dump.exe",
                "MariaDB 11.3/bin/mariadb-dump.exe",
                "MariaDB 11.2/bin/mariadb-dump.exe",
                "MariaDB 11.1/bin/mariadb-dump.exe",
                "MariaDB 11.0/bin/mariadb-dump.exe",
                "MariaDB 10.11/bin/mariadb-dump.exe",
                "MariaDB 10.10/bin/mariadb-dump.exe",
                "MariaDB 10.9/bin/mariadb-dump.exe",
                "MariaDB 10.8/bin/mariadb-dump.exe",
                "MariaDB 10.7/bin/mariadb-dump.exe",
                "MariaDB 10.6/bin/mariadb-dump.exe",
                "MariaDB 10.5/bin/mariadb-dump.exe",
                "MariaDB 10.4/bin/mariadb-dump.exe",
                "MariaDB 10.3/bin/mariadb-dump.exe",
                "MariaDB 10.2/bin/mariadb-dump.exe",
                "MariaDB 10.1/bin/mariadb-dump.exe",
                "MariaDB 10.0/bin/mariadb-dump.exe",
                "mariadb-dump") ?? throw new Exception("Couldn't find maridb-dump on your PC. You can configure the path in the settings.");
        }
    }
}