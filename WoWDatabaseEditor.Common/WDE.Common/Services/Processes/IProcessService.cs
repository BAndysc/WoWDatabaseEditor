using System;
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
    }

    [UniqueProvider]
    public interface IProgramFinder
    {
        string? TryLocate(params string[] names);
    }

    [UniqueProvider]
    public interface IProcessService
    {
        IProcess RunAndForget(string path, string arguments, string? workingDirectory, bool noWindow,
            params (string, string)[] envVars);
        
        Task<int> Run(CancellationToken token, string path, string arguments, string? workingDirectory,
            Action<string>? onOutput,
            Action<string>? onError,
            Action<TextWriter>? onInput = null,
            params (string, string)[] envVars);
    }
}