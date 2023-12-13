using System.Collections.Generic;

namespace WDE.Common.Services.CommandLine
{
    public interface ICommandLineArgs : IEnumerable<string>
    {
        bool IsArgumentSet(string argument);
        string? GetValue(string argument);
        void Init(string[] args);
    }
}