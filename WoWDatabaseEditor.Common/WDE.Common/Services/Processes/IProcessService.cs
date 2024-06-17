using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.Processes
{
    public interface IProcess
    {
        bool IsRunning { get; }
        void Kill();
        event Action<int>? OnExit;
    }

    [UniqueProvider]
    public interface IProgramFinder
    {
        string? TryLocate(params string[] names);
        string? TryLocateIncludingCurrentDir(params string[] names);
    }

    [UniqueProvider]
    public interface IProcessService
    {
        IProcess RunAndForget(string path, IReadOnlyList<string> arguments, string? workingDirectory, bool noWindow,
            params (string, string)[] envVars);
        
        Task<int> Run(CancellationToken token, string path, IReadOnlyList<string> arguments, string? workingDirectory,
            Action<string>? onOutput,
            Action<string>? onError,
            bool redirectInput,
            out StreamWriter inputWriter,
            params (string, string)[] envVars);
    }
}