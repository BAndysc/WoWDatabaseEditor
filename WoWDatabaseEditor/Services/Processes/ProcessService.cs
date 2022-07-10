using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Processes
{
    [AutoRegister]
    [SingleInstance]
    public class ProcessService : IProcessService
    {
        public class ProcessData : IProcess
        {
            private readonly Process process;

            public ProcessData(Process process)
            {
                this.process = process;
                process.EnableRaisingEvents = true;
                process.Exited += ProcessOnExited;
            }

            private void ProcessOnExited(object? sender, EventArgs e)
            {
                OnExit?.Invoke(process.ExitCode);
            }

            public bool IsRunning => !process.HasExited;
            
            public void Kill()
            {
                if (IsRunning)
                {
                    process.Kill();
                }
            }

            public event Action<int>? OnExit;
        }
        
        public IProcess RunAndForget(string path, string arguments, string? workingDirectory, bool noWindow,
            params (string, string)[] envVars)
        {
            var startInfo = new ProcessStartInfo(path, arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = noWindow;
            
            foreach (var envVar in envVars)
                startInfo.Environment.Add(envVar.Item1, envVar.Item2);
    
            if (workingDirectory != null)
                startInfo.WorkingDirectory = workingDirectory;
            
            Process process = new Process();
            process.StartInfo = startInfo;
            if (!process.Start())
                throw new Exception("Cannot start " + path);
            return new ProcessData(process);
        }
        
        public async Task<int> Run(CancellationToken token, string path, string arguments, string? workingDirectory, Action<string>? onOutput,
            Action<string>? onError, Action<TextWriter>? onInput,
            params (string, string)[] envVars)
        {
            var startInfo = new ProcessStartInfo(path, arguments);
            startInfo.UseShellExecute = false;
            if (onOutput != null)
                startInfo.RedirectStandardOutput = true;
            if (onError != null)
                startInfo.RedirectStandardError = true;
            if (onInput != null)
                startInfo.RedirectStandardInput = true;
            startInfo.CreateNoWindow = true;
            
            foreach (var envVar in envVars)
                startInfo.Environment.Add(envVar.Item1, envVar.Item2);
    
            if (workingDirectory != null)
                startInfo.WorkingDirectory = workingDirectory;
            
            Process process = new Process();
            process.StartInfo = startInfo;
            process.ErrorDataReceived += (sender, data) =>
            {
                if (data.Data != null)
                {
                    onError?.Invoke(data.Data);
                }
            };
            process.OutputDataReceived += (sender, data) =>
            {
                if (data.Data != null)
                {
                    onOutput?.Invoke(data.Data);
                }
            };
            if (!process.Start())
                throw new Exception("Cannot start " + path);
            
            if (onOutput != null)
                process.BeginOutputReadLine();
            
            if (onError != null)
                process.BeginErrorReadLine();

            //onInput?.Invoke(process.StandardInput);

            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (TaskCanceledException)
            {
                process.Kill();
            }

            return process.HasExited ? process.ExitCode : -1;
        }
    }
}