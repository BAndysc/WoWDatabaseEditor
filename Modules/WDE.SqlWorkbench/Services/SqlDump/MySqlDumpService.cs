using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.SqlDump;

[AutoRegister]
[SingleInstance]
internal class MySqlDumpService : IMySqlDumpService
{
    private readonly IProgramFinder programFinder;
    private readonly IProcessService processService;

    private static byte[] NewLine = "\n"u8.ToArray();
    
    public MySqlDumpService(IProgramFinder programFinder,
        IProcessService processService)
    {
        this.programFinder = programFinder;
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
    
    public async Task DumpDatabaseAsync(DatabaseCredentials credentials, MySqlDumpOptions options, string[] tables, string output, CancellationToken token)
    {
        List<string> arguments = new List<string>();
        AddOptions(options, arguments);
        
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
        
        arguments.Add(credentials.SchemaName);
        arguments.AddRange(tables);

        await using var f = File.Open(output, FileMode.Create, FileAccess.Write);
        
        await processService.Run(token, "mysqldump", string.Join(" ", arguments), null, x =>
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(x);
            f.Write(bytes);
            f.Write(NewLine);
        }, x =>
        {
            Console.WriteLine(x);
        });
    }
}